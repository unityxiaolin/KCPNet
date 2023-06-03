using KCPExampleProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XLGame;

/// <summary>
/// .net Framework控制台客户端
/// </summary>
namespace KCPExampleClient
{
    class ClientStart
    {
        static KCPNet<ClientSession, NetMsg> client;
        static Task<bool> checkTask = null;
        static void Main(string[] args)
        {
            string ip = "127.0.0.1";
            client = new KCPNet<ClientSession, NetMsg>();
            client.StartAsClient(ip, 17666);
            checkTask = client.ConnectServer(200,5000);
            Task.Run(ConnectCheck);
            while (true)
            {
                string ipt = Console.ReadLine();
                if(ipt=="quit")
                {
                    client.CloseClient();
                    break;
                }
                else
                {
                    client.clientSession.SendMsg(new NetMsg { info = ipt });
                }
            }
            Console.ReadKey();
        }

        private static int counter = 0;
        static async void ConnectCheck()
        {
            while (true)
            {
                await Task.Delay(3000);
                if(checkTask!=null && checkTask.IsCompleted)
                {
                    if(checkTask.Result)
                    {
                        KCPTools.ColorLog(LogColor.Green, "连接服务器成功.");
                        checkTask = null;
                        await Task.Run(SendPingMsg);
                    }
                    else
                    {
                        ++counter;
                        if(counter>4)
                        {
                            KCPTools.Error("客户端连接服务器失败{0}次，请检查网络。", counter);
                            checkTask = null;
                            break;
                        }
                        else
                        {
                            KCPTools.Warning("客户端连接服务器失败{0}次，正尝试连接中。。。", counter);
                            checkTask= client.ConnectServer(200, 5000);
                        }
                    }
                }
            }
        }

        static async void SendPingMsg()
        {
            while (true)
            {
                await Task.Delay(5000);
                if(client!=null && client.clientSession!=null)
                {
                    client.clientSession.SendMsg((new NetMsg
                    {
                        cmd = CMD.NetPing,
                        netPing = new NetPing
                        {
                            isOver = false
                        }
                    }));
                    KCPTools.ColorLog(LogColor.Green, "客户端发送心跳包");
                }
                else
                {
                    KCPTools.ColorLog(LogColor.Green, "心跳包任务取消");
                    break;
                }
            }
        }
    }
}
