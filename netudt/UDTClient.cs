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
        * @Description: �ر�����
        * @param @throws IOException    ����
        * @return void    ��������
         */
        public void shutdownNow()
        {
            //����ͻ����Ѿ��������Ҽ�����ݽ�����
            if (clientSession.IsReady && clientSession.IsActive == true)
            {
                //���Ͷ�ιر���Ϣ
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
                //cd ���  �ͻ���һ���رվ���Ч�ˣ����Բ��Ƴ����ö���ر�����ʹ����
                clientEndpoint.RemoveSession(clientSession.SocketID);
                //�رշ���
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
         * �Ѿ���shutdown����ȡ��
        * @Title: close
        * @Description: 10s�ڵȴ����ݷ�����ɺ�ر�
        * @param     ����
        * @return void    ��������
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
                            //û�з������ݣ�ֱ�ӹر�
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
                                //ÿ100ms���һ�η������
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
        * @Description: �ر�����
        * @param     ����
        * @return void    ��������
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
        * @Description: �ж��Ƿ��Ѿ��ر�
        * @param @return    ����
        * @return bool    ��������
         */
        public bool isClosed()
        {
            //�Է��ر�
            return closed || !clientSession.IsActive;
        }
    }
}
