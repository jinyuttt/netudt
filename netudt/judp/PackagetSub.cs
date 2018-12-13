using System;
using System.IO;
using System.Threading;

namespace netudt.judp
{
    public class PackagetSub
    {
        private static long sessionid = 0;
        public static int dataSzie = 1472;
        private static int bufsize = 0;
        private static readonly int headLen = 20;

        /**
         * 分割数据
         *
         * 
         */
        public static byte[][] splitData(byte[] data)
        {
            if (bufsize == 0)
            {
                bufsize = dataSzie - headLen;
            }
            long session = Interlocked.Increment(ref sessionid);
            int dataLen = data.Length;
            int num = data.Length / bufsize + data.Length % bufsize > 0 ? 1 : 0;
            byte[][] sendData = new byte[num][];
            int index = 0;
          
            MemoryStream buf = new MemoryStream(dataSzie);
            for (int i = 0; i < num; i++)
            {
                buf.Write(BitConverter.GetBytes(session),0,8);
                buf.Write(BitConverter.GetBytes(num),0,4);
                buf.Write(BitConverter.GetBytes(i),0,4);
                buf.Write(BitConverter.GetBytes(dataLen),0,4);
                if (index + bufsize < data.Length)
                {
                    buf.Write(data, index, bufsize);
                    index += bufsize;
                }
                else
                {
                    buf.Write(data, index, data.Length - index);
                }
                //
               
                byte[] tmp = new byte[buf.Position];
                buf.Seek(0, SeekOrigin.Begin);
                buf.Read(tmp, 0, tmp.Length);
                sendData[i] = tmp;
                buf.Seek(0, SeekOrigin.Begin);
            }
            buf.Close();
            return sendData;
        }

        /**
         * 单独分割数据
         * 
         *
         * 
         */
        public byte[][] Split(byte[] data, int len)
        {
            int size = len - headLen;
            long session = Interlocked.Increment(ref sessionid);
            int dataLen = data.Length;
            int num = (data.Length / size) + (data.Length % size > 0 ? 1 : 0);
            byte[][] sendData = new byte[num][];
            int index = 0;
           
            MemoryStream buf = new MemoryStream(dataSzie);
            for (int i = 0; i < num; i++)
            {

              
                buf.Write(BitConverter.GetBytes(session), 0, 8);
                buf.Write(BitConverter.GetBytes(num), 0, 4);
                buf.Write(BitConverter.GetBytes(i), 0, 4);
                buf.Write(BitConverter.GetBytes(dataLen), 0, 4);
                if (index + size < data.Length)
                {
                    buf.Write(data, index, size);
                }
                else
                {
                    buf.Write(data, index, data.Length - index - 1);
                }
                //
                
                byte[] tmp = new byte[buf.Position];

                buf.Seek(0, SeekOrigin.Begin);
                buf.Read(tmp, 0, tmp.Length);
                sendData[i] = tmp;
                buf.Seek(0, SeekOrigin.Begin);
            }
            buf.Close();
            return sendData;
        }
    }
 
}
