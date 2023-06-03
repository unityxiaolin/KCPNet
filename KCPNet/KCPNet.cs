using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 基于kcp封装，实现可靠udp
/// </summary>
namespace XLGame
{
    [Serializable]
    public abstract class KCPMsg { }
    public class KCPNet<T, K> where T : KCPSession<K>, new() where K : KCPMsg, new()
    {
        UdpClient m_udp;
        IPEndPoint m_ipEndPoint;
        private CancellationTokenSource m_cancellationTokenSource;//取消令牌源
        private CancellationToken m_cancellationToken;//取消令牌
        public KCPNet()
        {
            m_cancellationTokenSource = new CancellationTokenSource();
            m_cancellationToken = m_cancellationTokenSource.Token;
        }

        private void InitIOControl()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                m_udp.Client.IOControl((IOControlCode)(-1744830452), new byte[] { 0, 0, 0, 0 }, null);
            }
        }

        #region 服务器

        private Dictionary<uint, T> m_sessionDic = null;
        public void StartAsServer(string ip, int port)
        {
            m_sessionDic = new Dictionary<uint, T>();
            m_ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            m_udp = new UdpClient(m_ipEndPoint);
            InitIOControl();
            KCPTools.ColorLog(LogColor.Green, "Server Start ...");
            Task.Run(ServerReceive, m_cancellationToken);
        }

        async void ServerReceive()
        {
            UdpReceiveResult result;
            while (true)
            {
                try
                {
                    if (m_cancellationToken.IsCancellationRequested)
                    {
                        KCPTools.ColorLog(LogColor.Cyan, "客户端接收数据的任务已经取消");
                        break;
                    }

                    result = await m_udp.ReceiveAsync();
                    uint sessionId = BitConverter.ToUInt32(result.Buffer, 0);
                    if (sessionId == 0)
                    {
                        //客户端首次建立连接，分配一个唯一sid，前四个字节为0，后四个字节为sid
                        sessionId = GenerateUniqueSessionID();
                        byte[] sidBytes = BitConverter.GetBytes(sessionId);
                        byte[] convBytes = new byte[8];
                        Array.Copy(sidBytes, 0, convBytes, 4, 4);
                        SendUDPMsg(convBytes, result.RemoteEndPoint);
                    }
                    else
                    {
                        if (!m_sessionDic.TryGetValue(sessionId, out T session))
                        {
                            session = new T();
                            session.InitSession(sessionId, SendUDPMsg, result.RemoteEndPoint);
                            session.OnSessionClose = OnServerSessionClose;
                            lock (m_sessionDic)
                            {
                                m_sessionDic.Add(sessionId, session);
                            }
                        }
                        else
                        {
                            session = m_sessionDic[sessionId];
                        }
                        session.ReceiveBuffer(result.Buffer);
                    }
                }
                catch (Exception e)
                {
                    KCPTools.Warning("服务器udp接收数据异常:{0}", e.ToString());
                }
            }
        }

        void OnServerSessionClose(uint sid)
        {
            if (m_sessionDic.ContainsKey(sid))
            {
                m_sessionDic.Remove(sid);
                KCPTools.Warning("session:{0} remove in sessionDic.", sid);
            }
            else
            {
                KCPTools.Error("session:{0} cannot find in sessionDic.");
            }
        }

        public void CloseServer()
        {
            foreach (var session in m_sessionDic)
            {
                session.Value.CloseSession();
            }
            m_sessionDic = null;
            if (m_udp != null)
            {
                m_udp.Close();
                m_udp = null;
                m_cancellationTokenSource.Cancel();
            }
        }

        #endregion


        #region 客户端
        public T clientSession;
        public void StartAsClient(string ip, int port)
        {
            m_udp = new UdpClient(0);
            InitIOControl();
            m_ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            KCPTools.ColorLog(LogColor.Green, "Client Start ...");
            Task.Run(ClientReceive, m_cancellationToken);
        }

        /// <summary>
        /// 发送四个字节为0的数据给服务器，则此客户端为全新的客户端，服务器会返回一个sid给该客户端
        /// </summary>
        public Task<bool> ConnectServer(int interval, int maxIntervalSum = 5000)
        {
            SendUDPMsg(new byte[4], m_ipEndPoint);
            int checkTimes = 0;
            Task<bool> task = Task.Run(async () =>
              {
                  while (true)
                  {
                      await Task.Delay(interval);
                      checkTimes += interval;
                      if (clientSession != null && clientSession.IsConnected())
                      {
                          return true;
                      }
                      else
                      {
                          if (checkTimes > maxIntervalSum)
                          {
                              return false;
                          }
                      }
                  }
              });
            return task;
        }

        async void ClientReceive()
        {
            UdpReceiveResult result;
            while (true)
            {
                try
                {
                    if (m_cancellationToken.IsCancellationRequested)
                    {
                        KCPTools.ColorLog(LogColor.Cyan, "客户端接收数据的任务已经取消");
                        break;
                    }

                    result = await m_udp.ReceiveAsync();
                    if (Equals(m_ipEndPoint, result.RemoteEndPoint))
                    {
                        uint sessionId = BitConverter.ToUInt32(result.Buffer, 0);
                        if (sessionId == 0)
                        {
                            //sid 数据
                            if (clientSession != null && clientSession.IsConnected())
                            {
                                //已经建立了连接，初始化完成了，收到了多余的sid，直接丢弃
                                KCPTools.Warning("客户端初始化完成了，收到了多余的sid，直接丢弃");
                            }
                            else
                            {
                                //未初始化，收到了服务器分配的sid，初始化一个客户端session
                                sessionId = BitConverter.ToUInt32(result.Buffer, 4);
                                KCPTools.ColorLog(LogColor.Green, "udp请求分配的sid:{0}", sessionId);

                                //会话处理
                                clientSession = new T();
                                clientSession.InitSession(sessionId, SendUDPMsg, m_ipEndPoint);
                                clientSession.OnSessionClose = OnClientSessionClose;
                            }
                        }
                        else
                        {
                            //处理业务逻辑
                            if (clientSession != null && clientSession.IsConnected())
                            {
                                clientSession.ReceiveBuffer(result.Buffer);
                            }
                            else
                            {
                                //没初始化且sid!=0，数据消息提前到了，直接丢弃消息，直到初始化完成，kcp重传再开始处理
                                KCPTools.Warning("客户端正在初始化中...");
                            }
                        }
                    }
                    else
                    {
                        KCPTools.Warning("客户端udp接收了非法目标数据.");
                    }
                }
                catch (Exception e)
                {
                    KCPTools.Warning("客户端udp接收数据异常:{0}", e.ToString());
                }
            }
        }


        void OnClientSessionClose(uint sid)
        {
            m_cancellationTokenSource.Cancel();
            if (m_udp != null)
            {
                m_udp.Close();
                m_udp = null;
            }
            KCPTools.Warning("客户端断开连接，sid:{0}", sid);
        }

        public void CloseClient()
        {
            if (clientSession != null)
            {
                clientSession.CloseSession();
            }
        }

        #endregion

        void SendUDPMsg(byte[] bytes, IPEndPoint remotePoint)
        {
            if (m_udp != null)
            {
                m_udp.SendAsync(bytes, bytes.Length, remotePoint);
            }
        }

        public void BroadCastMsg(K msg)
        {
            byte[] bytes = KCPTools.Serialize(msg);
            foreach (var session in m_sessionDic)
            {
                session.Value.SendMsg(bytes);
            }
        }

        private uint sid = 0;
        public uint GenerateUniqueSessionID()
        {
            lock (m_sessionDic)
            {
                while (true)
                {
                    ++sid;
                    if (sid == uint.MaxValue)
                    {
                        sid = 1;
                    }
                    if (!m_sessionDic.ContainsKey(sid))
                    {
                        break;
                    }
                }
            }
            return sid;
        }
    }
}
