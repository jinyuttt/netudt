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
         * ��������
         * @return
         */
        private bool Check()
        {
            if (sumNum >= buffer.Length)
            {
                //���ɹ�
                if (sumLen == dataLen)
                {
                    //��ʼ�������
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
         * ��������
         * @param data
         * @return
         */
        public bool AddData(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            stream.Seek(12, SeekOrigin.Begin);//�Ƴ�12�ֽ�ͷ
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
         * ��ȡ����
         * @return
         */
        public byte[] GetData()
        {
            return result;
        }

    }
}
