using KCPExampleProtocol;
using XLGame;
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// .net core服务端session
/// </summary>
namespace KCPExampleServer
{
    class ServerSession : KCPSession<NetMsg>
    {
        protected override void OnConnected()
        {
            KCPTools.ColorLog(LogColor.Green, "client online,sid:{0}", m_sissionId);
        }

        protected override void OnDisconnected()
        {
            KCPTools.Warning("client offline,sid:{0}", m_sissionId);
        }

        protected override void OnReceiveMsg(NetMsg msg)
        {
            KCPTools.ColorLog(LogColor.Magenta, "sid:{0},receive By Client,cmd:{1},info:{2}", m_sissionId, msg.cmd.ToString(), msg.info);

            if(msg.cmd==CMD.NetPing)
            {
                if(msg.netPing.isOver)
                {
                    CloseSession();
                }
                else
                {
                    //收到ping请求，则重置ping检查计数，并回复ping消息到客户端
                    checkCounter = 0;
                    NetMsg pingMsg = new NetMsg
                    {
                        cmd = CMD.NetPing,
                        netPing = new NetPing
                        {
                            isOver = false
                        }
                    };
                    SendMsg(pingMsg);
                }
            }
        }

        private int checkCounter;
        DateTime checkTime = DateTime.UtcNow.AddSeconds(5);

        protected override void OnUpdate(DateTime now)
        {
            if(now>checkTime)
            {
                checkTime = now.AddSeconds(5);
                checkCounter++;
                if(checkCounter>3)
                {
                    NetMsg pingMsg = new NetMsg
                    {
                        cmd = CMD.NetPing,
                        netPing = new NetPing
                        {
                            isOver = true
                        }
                    };
                    OnReceiveMsg(pingMsg);
                }
            }
        }
    }
}
