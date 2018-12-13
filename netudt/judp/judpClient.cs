
using netudt;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace netudt.judp
{
    public class judpClient {
        private UDTClient client = null;
        private const int bufSize = 65535;
        private long sumLen = 0;
        private PackagetCombin pack = new PackagetCombin();
        public int dataLen = 0;
        public judpClient(string lcoalIP, int port)
        {
           
            try {
                client = new UDTClient(port, lcoalIP);
            } catch (SocketException e) {
                Console.WriteLine(e);
            } catch (Exception e) {

                Console.WriteLine(e);
            }

        }
        public judpClient()
        {
            try {
                client = new UDTClient();
            } catch (SocketException e) {
                Console.WriteLine(e);
            } catch (Exception e) {
                Console.WriteLine(e);
            }

        }
        public judpClient(int port)
        {
            try {
                client = new UDTClient(port);
            } catch (SocketException e) {
                Console.WriteLine(e);
            } catch (Exception e) {
                Console.WriteLine(e);
            }

        }
        public bool Connect(string ip, int port)
        {
            bool isSucess = false;
            if (client != null)
            {
                try
                {
                    client.Connect(ip, port);
                    isSucess = true;
                }
                catch (Exception e)
                {

                    Console.WriteLine(e);
                }
            }

            return isSucess;
        }
        public int SendData(byte[] data)
        {
            if (data == null)
            {
                return 0;
            }
            int r = 0;
            if (client != null)
            {
                try
                {

                    client.sendBlocking(data);
                    r = data.Length;
                    sumLen += r;

                }
                catch (Exception e)
                {

                    Console.WriteLine(e);
                }
            }
            return r;
        }


        public int SendSplitData(byte[] data)
        {
            if (data == null)
            {
                return 0;
            }
            int r = 0;
            byte[][] sendData = null;
            if (dataLen == 0)
            {
                sendData = PackagetSub.splitData(data);
            }
            else
            {
                PackagetSub sub = new PackagetSub();
                sendData = sub.Split(data, dataLen);
            }
            for (int i = 0; i < sendData.Length; i++)
            {
                r += SendData(sendData[i]);
            }
            return r;

        }

        /// <summary>
        /// 
        /// </summary>
        public void PauseOutput()
        {
            try {
                client.GetOutputStream().PauseOutput();
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /**
         * 读取数据
         * 只和split发送对应
         * @return
         */
        public byte[] ReadALL()
        {
            byte[] result = null;
            if (client != null)
            {
                byte[] readBytes = new byte[bufSize];//接收区
                int r = 0;
                try {
                    while (true)
                    {
                        if (client.isClosed())
                        {
                            return null;
                        }
                        r = client.GetInputStream().Read(readBytes);
                        if (r == -1)
                        {
                            result = pack.GetData();
                            break;
                        }
                        else
                        {

                            if (r == 0)
                            {
                                try {
                                    Thread.Sleep(1000);

                                    continue;
                                } catch (Exception e) {
                                    Console.WriteLine(e);
                                }
                            }
                            //
                            byte[] buf = new byte[r];
                            Array.Copy(readBytes, 0, buf, 0, r);
                            if (pack.AddData(buf))
                            {
                                result = pack.GetData();
                                break;
                            }


                        }
                    }

                } catch (Exception e) {

                    Console.WriteLine(e);
                }

            }

            return result;
        }
        public int read(byte[] data)
        {
            try
            {
                return client.read(data);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return -1;
        }

        /**
         * 关闭
         */
        public void Close()
        {
            if (client != null)
            {
                if (sumLen == 0)
                {
                    if (!client.isClosed())
                        client.Shutdown();
                }
                else
                {
                   
                    if (!client.isClosed())
                        client.Shutdown();
                }
            }
        }

        /**
         * 是否关闭
         * @return
         */
        public bool IsClose()
        {
            return client.isClosed();
        }
        /**
         * 设置是读取为主还是写入为主
         * 如果是写入为主，当读取速度慢时，数据覆盖丢失
         * 默认读取为主，还没有读取则不允许覆盖，丢掉数据，等待重复
         * islagerRead=true才有意义
         * 
         */
        public void ResetBufMaster(bool isRead)
        {
            try {
                client.GetInputStream().ResetBufMaster(isRead);
            } catch (IOException e) {

                Console.WriteLine(e);
            }
        }

        /**
         * 设置大数据读取
         * 默认 false
         *
         */
        public void SetLargeRead(bool islarge)
        {
            try {
                client.GetInputStream().SetLargeRead(islarge);
            } catch (IOException e) {
                Console.WriteLine(e);

            }
        }
    }
}
