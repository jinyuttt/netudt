using System;
using System.IO;

namespace netudt.judp
{
    public class DataStruct
    {
        public int dataLen = 0;
        public byte[][] buffer = null;
        public long id;
        private volatile int sumNum = 0;
        private volatile int sumLen = 0;
        private byte[] result = null;
        public DataStruct(int num)
        {
            buffer = new byte[num][];
        }

        /**
         * 整理数据
         * @return
         */
        private bool Check()
        {
            if (sumNum >= buffer.Length)
            {
                //检查成功
                if (sumLen == dataLen)
                {
                    //开始检查数据
                    result = new byte[dataLen];
                    MemoryStream cur = new MemoryStream(result);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i] == null)
                        {
                            return false;
                        }
                        else
                        {
                            cur.Write(buffer[i],0,buffer[i].Length);
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        public void clear()
        {
            buffer = null;
            result = null;
        }
        /**
         * 接收数据
         * @param data
         * @return
         */
        public bool AddData(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            stream.Seek(12, SeekOrigin.Begin);//移除12字节头
            byte[] indexBytes =new  byte[4];
            byte[] lenBytes = new byte[4];
            int index = 0;
            int dataLen = 0;
            stream.Read(indexBytes, 0, 4);
            stream.Read(lenBytes, 0, 4);
            index = BitConverter.ToInt32(indexBytes, 0);
            dataLen = BitConverter.ToInt32(lenBytes, 0);
            byte[] tmp = new byte[stream.Length-stream.Position];
            stream.Read(tmp, 0, tmp.Length);
            buffer[index] = tmp;
            sumNum++;
            sumLen += tmp.Length;
            return Check();
        }

        /**
         * 获取数据
         * @return
         */
        public byte[] GetData()
        {
            return result;
        }

    }
}
