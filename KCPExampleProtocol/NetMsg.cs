using XLGame;
using System;

namespace KCPExampleProtocol
{
    [Serializable]
    public class NetMsg:KCPMsg
    {
        public CMD cmd;
        public NetPing netPing;
        public string info;
    }

    [Serializable]
    public class NetPing
    {
        //是否结束连接
        public bool isOver;
    }

    [Serializable]
    public enum CMD
    {
        None,
        NetPing,
    }
}
