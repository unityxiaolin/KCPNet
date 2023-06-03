using System;
using System.Net;
using System.Net.Sockets.Kcp;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 网络会话数据接收与发送
/// </summary>
namespace XLGame
{
    public enum SessionState
    {
        None,
        Connected,
        Disconnected
    }
    public abstract class KCPSession<T> where T : KCPMsg, new()
    {
        protected uint m_sissionId;
        Action<byte[], IPEndPoint> m_udpSender;
        private IPEndPoint m_ipEndPoint;
        protected SessionState m_sessionState = SessionState.None;
        public Action<uint> OnSessionClose;

        private KCPHandle m_kcpHandle;
        private Kcp m_kcp;

        private CancellationTokenSource m_cancellationTokenSource;//取消令牌源
        private CancellationToken m_cancellationToken;//取消令牌

        public void InitSession(uint sessionId, Action<byte[], IPEndPoint> udpSender, IPEndPoint remotePoint)
        {
            m_sissionId = sessionId;
            m_udpSender = udpSender;
            m_ipEndPoint = remotePoint;
            m_sessionState = SessionState.Connected;
            m_kcpHandle = new KCPHandle();
            m_kcp = new Kcp(sessionId, m_kcpHandle);
            //极速模式
            m_kcp.NoDelay(1, 10, 2, 1);
            //WndSize该调用将会设置协议的最大发送窗口和最大接收窗口大小，默认为32.
            //这个可以理解为 TCP的 SND_BUF 和 RCV_BUF，只不过单位不一样 SND/RCV_BUF 单位是字节，这个单位是包。
            m_kcp.WndSize(64, 64);
            //纯算法协议并不负责探测 MTU，默认 mtu是1400字节，可以使用ikcp_setmtu来设置该值。该值将会影响数据包归并及分片时候的最大传输单元。
            m_kcp.SetMtu(512);

            m_kcpHandle.OutAction = (buffer) =>
              {
                  byte[] bytes = buffer.ToArray();
                  udpSender(bytes, remotePoint);
              };
            m_kcpHandle.ReceiveAction = (buffer) =>
              {
                  buffer = KCPTools.DeCompress(buffer);
                  T msg = KCPTools.DeSerialize<T>(buffer);
                  if (msg != null)
                  {
                      OnReceiveMsg(msg);
                  }
              };
            OnConnected();
            m_cancellationTokenSource = new CancellationTokenSource();
            m_cancellationToken = m_cancellationTokenSource.Token;
            Task.Run(Update, m_cancellationToken);
        }

        public void ReceiveBuffer(byte[] buffer)
        {
            m_kcp.Input(buffer.AsSpan());
        }



        public void CloseSession()
        {
            m_cancellationTokenSource.Cancel();
            OnDisconnected();
            OnSessionClose?.Invoke(m_sissionId);
            OnSessionClose = null;

            m_sessionState = SessionState.Disconnected;
            m_ipEndPoint = null;
            m_udpSender = null;
            m_sissionId = 0;
            m_kcpHandle = null;
            m_kcp = null;
            m_cancellationTokenSource = null;
        }

        public void SendMsg(T msg)
        {
            if (IsConnected())
            {
                byte[] bytes = KCPTools.Serialize(msg);
                if (bytes != null)
                {
                    SendMsg(bytes);
                }
            }
            else
            {
                KCPTools.Warning("没有连接，不能发送消息");
            }
        }

        public void SendMsg(byte[] msgBytes)
        {
            if (IsConnected())
            {
                msgBytes = KCPTools.Compress(msgBytes);
                m_kcp.Send(msgBytes.AsSpan());
            }
            else
            {
                KCPTools.Warning("没有连接，不能发送消息");
            }
        }

        async void Update()
        {
            try
            {
                while (true)
                {
                    DateTime now = DateTime.UtcNow;
                    OnUpdate(now);
                    if (m_cancellationToken.IsCancellationRequested)
                    {
                        KCPTools.ColorLog(LogColor.Cyan, "{0}更新任务已经取消", "session");
                        break;
                    }
                    else
                    {
                        m_kcp.Update(now);
                        int len;
                        while ((len = m_kcp.PeekSize()) > 0)
                        {
                            var buffer = new byte[len];
                            if (m_kcp.Recv(buffer) >= 0)
                            {
                                m_kcpHandle.Receive(buffer);
                            }
                        }
                        await Task.Delay(10);
                    }
                }
            }
            catch (Exception e)
            {
                KCPTools.Warning("session 更新异常:{0}", e.ToString());
            }
        }

        protected abstract void OnConnected();
        protected abstract void OnDisconnected();
        protected abstract void OnReceiveMsg(T msg);
        protected abstract void OnUpdate(DateTime now);

        /// <summary>
        /// 是否是已连接的
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return m_sessionState == SessionState.Connected;
        }

        public override bool Equals(object obj)
        {
            if (obj is KCPSession<T>)
            {
                KCPSession<T> us = obj as KCPSession<T>;
                return m_sissionId == us.m_sissionId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return m_sissionId.GetHashCode();
        }

        public uint GetSessionID()
        {
            return m_sissionId;
        }
    }
}
