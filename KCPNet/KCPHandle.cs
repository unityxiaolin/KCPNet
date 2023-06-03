using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets.Kcp;
using System.Text;

/// <summary>
/// KCP数据处理器
/// </summary>
namespace XLGame
{
    public class KCPHandle : IKcpCallback
    {
        public Action<Memory<byte>> OutAction;
        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            using (buffer)
            {
                OutAction(buffer.Memory.Slice(0, avalidLength));
            }
        }

        public Action<byte[]> ReceiveAction;
        public void Receive(byte[] buffer)
        {
            ReceiveAction(buffer);
        }
    }
}
