using netudt.packets;
using netudt.util;
using System;
using System.IO;
using System.Threading;
namespace netudt
{
    public class UDTClient
    {

      
        private UDPEndPoint clientEndpoint;
        private ClientSession clientSession;
        private bool closed = false;
        private Thread closeThread = null;//cd
        private int waitClose = 10 * 1000;
        private object lock_obj = new object();
        public UDTClient( int localport=0, string address=null)
        {
            //create endpoint
            clientEndpoint = new UDPEndPoint(localport,address);
           // logger.info("Created client endpoint on port " + localport);
        }

        public UDTClient(string address)
        {
            //create endpoint
            clientEndpoint = new UDPEndPoint(0, address);
            // logger.info("Created client endpoint on port " + clientEndpoint.getLocalPort());
        }

        public UDTClient(UDPEndPoint endpoint)
        {
            clientEndpoint = endpoint;
         
        }

        /**
         * establishes a connection to the given server. 
         * Starts the sender thread.
         * @param host
         * @param port
         * @throws UnknownHostException
         */
        public void Connect(string host, int port)
        {
            //InetAddress address = InetAddress.getByName(host);
            Destination destination = new Destination(host, port);
            //create client session...
            clientSession = new ClientSession(clientEndpoint, destination);
            clientEndpoint.AddSession(clientSession.SocketID, clientSession);

            clientEndpoint.Start();
            clientSession.Connect();
            //wait for handshake
            while (!clientSession.IsReady)
            {
                Thread.Sleep(500);
            }
            //logger.info("The UDTClient is connected");
            Thread.Sleep(500);
        }

        /**
         * sends the given data asynchronously
         * 
         * @param data
         * @throws IOException
         * @throws InterruptedException
         */
        public void send(byte[] data)
        {
            if (closed)
            {
                return;//cd
            }
            clientSession.Socket.DoWrite(data);
        }

        public void sendBlocking(byte[] data)
        {
            if (closed)
            {
                return;//cd
            }
            clientSession.Socket.DoWriteBlocking(data);
        }

        public int read(byte[] data)
        {
            return clientSession.Socket.GetInputStream().Read(data);
        }

        /**
         * flush outstanding data (and make sure it is acknowledged)
         * @throws IOException
         * @throws InterruptedException
         */
        public void Flush()
        {
            clientSession.Socket.Flush();
        }

        /**
         * 
        * @Title: shutdown
        * @Description: 关闭连接
        * @param @throws IOException    参数
        * @return void    返回类型
         */
        public void shutdownNow()
        {
            //如果客户端已经就绪并且激活（数据交互）
            if (clientSession.IsReady && clientSession.IsActive == true)
            {
                //发送多次关闭信息
                Shutdown shutdown = new Shutdown();
                shutdown.DestinationID = clientSession.Destination.SocketID;
                shutdown.SetSession(clientSession);
                try
                {
                    clientEndpoint.DoSend(shutdown);
                  //  TimeUnit.MILLISECONDS.sleep(100);
                }
                catch (Exception e)
                {
                    //logger.log(Level.SEVERE, "ERROR: Connection could not be stopped!", e);
                }
                clientSession.Socket.GetReceiver().Stop();
                clientEndpoint.Stop();
                //cd 添加  客户端一旦关闭就无效了，可以不移除，该对象关闭则不能使用了
                clientEndpoint.RemoveSession(clientSession.SocketID);
                //关闭发送
                clientSession.Socket.GetSender().Stop();
                closed = true;
            }
            clientEndpoint.Stop();
        }

        public UDTInputStream GetInputStream()
        {
            return clientSession.Socket.GetInputStream();
        }

        public UDTOutputStream GetOutputStream()
        {
            return clientSession.Socket.GetOutputStream();
        }

        public UDPEndPoint GetEndpoint()
        {
            return clientEndpoint;
        }

        public UDTStatistics GetStatistics()
        {
            return clientSession.Statistics;
        }

        public long GetSocketID()
        {
            //cd 
            return clientSession.SocketID;
        }


        /**
         * 已经被shutdown方法取代
        * @Title: close
        * @Description: 10s内等待数据发送完成后关闭
        * @param     参数
        * @return void    返回类型
         */

        public void close()
        {
            lock (lock_obj)
            {
                closed = true;
                if (closeThread == null)
                {
                    closeThread = new Thread(() =>
                    {

                        int num = 0;
                        while (true)
                        {
                            //没有发送数据，直接关闭
                            if (clientSession.Socket.GetSender().IsSenderEmpty())
                            {
                                try
                                {
                                    shutdownNow();
                                    break;
                                }
                                catch (IOException e)
                                {

                                 
                                }
                            }
                            else
                            {
                                //每100ms监测一次发送情况
                                try
                                {
                                    Thread.Sleep(100);
                                    num++;
                                    if (waitClose <= num * 100)
                                    {
                                        try
                                        {
                                            shutdownNow();
                                        }
                                        catch (IOException e)
                                        {

                                           // e.printStackTrace();
                                        }
                                        break;
                                    }
                                }
                                catch (Exception e)
                                {

                                   // e.printStackTrace();
                                }
                            }
                        }

                    });
                    // closeThread.
                    closeThread.IsBackground = true;
                    closeThread.Name="closeThread";
                }
                if (!closeThread.IsAlive)
                {
                    closeThread.Start();
                }
            }
        }

        /**
         * 
        * @Title: shutdown
        * @Description: 关闭连接
        * @param     参数
        * @return void    返回类型
         */
        public void Shutdown()
        {
            lock (lock_obj)
            {
                this.close();
            }
        }

        /**
         * 
        * @Title: isClose
        * @Description: 判断是否已经关闭
        * @param @return    参数
        * @return bool    返回类型
         */
        public bool isClosed()
        {
            //对方关闭
            return closed || !clientSession.IsActive;
        }
    }
}
