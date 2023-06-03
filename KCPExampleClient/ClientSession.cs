using KCPExampleProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XLGame;

/// <summary>
/// .net 控制台客户端session
/// </summary>
namespace KCPExampleClient
{
    class ClientSession : KCPSession<NetMsg>
    {
        protected override void OnConnected()
        {
            
        }

        protected override void OnDisconnected()
        {
            
        }

        protected override void OnReceiveMsg(NetMsg msg)
        {
            KCPTools.ColorLog(LogColor.Magenta, "sid:{0}, receiveByServer:{1}", m_sissionId, msg.info);
        }

        protected override void OnUpdate(DateTime now)
        {
            
        }
    }
}
