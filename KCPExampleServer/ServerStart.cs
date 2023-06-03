using KCPExampleProtocol;
using XLGame;
using System;

/// <summary>
/// .net core控制台服务端
/// </summary>
namespace KCPExampleServer
{
    class ServerStart
    {
        static void Main(string[] args)
        {
            string ip = "127.0.0.1";
            KCPNet<ServerSession, NetMsg> server = new KCPNet<ServerSession, NetMsg>();
            server.StartAsServer(ip, 17666);

            while (true)
            {
                string ipt = Console.ReadLine();
                if(ipt=="quit")
                {
                    server.CloseServer();
                    break;
                }
                else
                {
                    server.BroadCastMsg(new NetMsg { info = ipt });
                }
            }
            Console.ReadKey();
        }
    }
}
