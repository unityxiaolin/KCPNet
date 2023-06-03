using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace XLGame
{
    public static class KCPTools
    {
        #region 序列化和反序列化工具

        /// <summary>
        /// 序列化
        /// </summary>
        public static byte[] Serialize<T>(T msg) where T : KCPMsg
        {
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, msg);
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms.ToArray();
                }
                catch (SerializationException e)
                {
                    Error("序列化失败：{0}", e.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        public static T DeSerialize<T>(byte[] bytes) where T : KCPMsg
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    T msg = (T)bf.Deserialize(ms);
                    return msg;
                }
                catch (SerializationException e)
                {
                    Error("反序列化失败：{0}    字节长度：{1}", e.Message, bytes.Length);
                    throw;
                }
            }
        }

        #endregion


        #region LOG

        public static Action<string> LogFunc;
        public static Action<LogColor, string> ColorLogFunc;
        public static Action<string> WarnFunc;
        public static Action<string> ErrorFunc;

        public static void Log(string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            if (LogFunc != null)
            {
                LogFunc(msg);
            }
            else
            {
                ConsoleLog(msg, LogColor.None);
            }
        }
        public static void ColorLog(LogColor color, string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            if (ColorLogFunc != null)
            {
                ColorLogFunc(color, msg);
            }
            else
            {
                ConsoleLog(msg, color);
            }
        }
        public static void Warning(string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            if (WarnFunc != null)
            {
                WarnFunc(msg);
            }
            else
            {
                ConsoleLog(msg, LogColor.Yellow);
            }
        }
        public static void Error(string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            if (ErrorFunc != null)
            {
                ErrorFunc(msg);
            }
            else
            {
                ConsoleLog(msg, LogColor.Red);
            }
        }
        private static void ConsoleLog(string msg, LogColor color)
        {
            int threadID = Thread.CurrentThread.ManagedThreadId;
            msg = string.Format("Thread:{0} {1}", threadID, msg);
            switch (color)
            {
                case LogColor.Red:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogColor.Green:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogColor.Blue:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogColor.Cyan:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogColor.Magenta:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogColor.Yellow:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogColor.None:
                default:
                    Console.WriteLine(msg);
                    break;
            }
        }

        #endregion


        #region 压缩和解压缩工具

        /// <summary>
        /// 压缩
        /// </summary>
        public static byte[] Compress(byte[] input)
        {
            using (MemoryStream outMs = new MemoryStream())
            {
                using (GZipStream gzs = new GZipStream(outMs, CompressionMode.Compress, true))
                {
                    gzs.Write(input, 0, input.Length);
                    gzs.Close();
                    return outMs.ToArray();
                }
            }
        }


        /// <summary>
        /// 解压缩
        /// </summary>
        public static byte[] DeCompress(byte[] input)
        {
            using (MemoryStream inputMs = new MemoryStream(input))
            {
                using (MemoryStream outputMs = new MemoryStream())
                {
                    using (GZipStream gzs = new GZipStream(inputMs, CompressionMode.Decompress))
                    {
                        byte[] bytes = new byte[1024];
                        int len = 0;
                        while ((len = gzs.Read(bytes, 0, bytes.Length)) > 0)
                        {
                            outputMs.Write(bytes, 0, len);
                        }
                        gzs.Close();
                        return outputMs.ToArray();
                    }
                }
            }
        }

        #endregion

        #region 日期时间相关

        static readonly DateTime utcStart = new DateTime(1970, 1, 1);
        public static ulong GetUTCStartMilliSeconds()
        {
            TimeSpan ts = DateTime.UtcNow - utcStart;
            return (ulong)ts.TotalMilliseconds;
        }

        #endregion

    }

    public enum LogColor
    {
        None,
        Red,
        Green,
        Blue,
        Cyan,
        Magenta,
        Yellow
    }
}