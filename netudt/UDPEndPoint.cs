using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using netudt.Log;
using System.Collections.Generic;
using netudt.packets;

namespace netudt
{
    public class UDPEndPoint
    {
        private int port;

        private Socket dgSocket;

        //active sessions keyed by socket ID
        private ConcurrentDictionary<long, UDTSession> sessions = new ConcurrentDictionary<long, UDTSession>();

        //last received packet
        private IUDTPacket lastPacket;

        //if the endpoint is configured for a server socket,
        //this queue is used to handoff new UDTSessions to the application
        private BlockingCollection<UDTSession> sessionHandoff = new BlockingCollection<UDTSession>(1);

        private bool serverSocketMode = false;

        //has the endpoint been stopped?
        private volatile bool stopped = false;

        public static int DATAGRAM_SIZE = 1400;

        private volatile int sessionnum = 0;//cd 添加 临时统计

        byte[] dp = new byte[DATAGRAM_SIZE];

        private long lastDestID = -1;
        private UDTSession lastSession;

        private int n = 0;

        private Object lock_obj = new Object();

        /**
         * create an endpoint on the given socket
         * 
         * @param socket -  a UDP datagram socket
         */
        public UDPEndPoint(Socket socket)
        {
            this.dgSocket = socket;
            IPEndPoint point = dgSocket.LocalEndPoint as IPEndPoint;
            port = point.Port;

        }



        /**
         * Bind to the given address and port
         * @param localAddress
         * @param localPort - the port to bind to. If the port is zero, the system will pick an ephemeral port.
         * @throws SocketException
         * @throws UnknownHostException
         */
        public UDPEndPoint(int localPort = 0, string localAddress = null)
        {
            dgSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            if (localAddress == null)
            {
                dgSocket.Bind(new IPEndPoint(IPAddress.Any, localPort));
            }
            else
            {
                dgSocket.Bind(new IPEndPoint(IPAddress.Parse(localAddress), localPort));
            }
            if (localPort > 0) this.port = localPort;
            else port = (dgSocket.LocalEndPoint as IPEndPoint).Port;

            //set a time out to avoid blocking in doReceive()
            dgSocket.ReceiveTimeout = 100000;
            //buffer size
            dgSocket.ReceiveBufferSize=64 * 1024;
        }


        public void Start(bool serverSocketModeEnabled)
        {
            serverSocketMode = serverSocketModeEnabled;
            //start receive thread
            Thread receive = new Thread(() =>
              {
                  try
                  {
                      DoReceive();
                  }
                  catch (Exception ex)
                  {
                      FlashLogger.Error( "", ex);
                  }
              });
            receive.Name = "Recv";
            receive.IsBackground = true;
            receive.Start();

        }

       

        public void Start()
        {
            Start(false);
        }

        public void Stop()
        {
            stopped = true;
            dgSocket.Close();
            sessions.Clear();//cd 2018-08-25
        }

        /**
         * @return the port which this client is bound to
         */
        public int LocalPort
        {
            get { if (port == 0) { port = (dgSocket.LocalEndPoint as IPEndPoint).Port; }; return port; }
        }
        /**
         * @return Gets the local address to which the socket is bound
         */
        public IPEndPoint GetLocalAddress()
        {
            return dgSocket.LocalEndPoint as IPEndPoint;
        }

        public Socket GetSocket()
        {
            return dgSocket;
        }

        IUDTPacket getLastPacket()
        {
            return lastPacket;
        }

        public void AddSession(long destinationID, UDTSession session)
        {
            FlashLogger.Info("Storing session <" + destinationID + ">");
            sessionnum++;
            sessions[destinationID]=session;
          
        }

        public UDTSession GetSession(long destinationID)
        {
            UDTSession session = null;
             sessions.TryGetValue(destinationID,out session);
            return session;
        }

        /**
         * 移除session
         * cd
         * @param socketid
         * @return
         */
        public UDTSession RemoveSession(long socketid)
        {
            //cd
            sessionnum--;
            UDTSession session = null;
            FlashLogger.Info("Storing Sessionnum:" + sessionnum);
             sessions.TryRemove(socketid,out session);
            return session;
        }
        public ICollection<UDTSession> GetSessions()
        {
            return sessions.Values;
        }

        /**
         * wait the given time for a new connection
         * @param timeout - the time to wait
         * @param unit - the {@link TimeUnit}
         * @return a new {@link UDTSession}
         * @throws InterruptedException
         */
        internal UDTSession Accept(int timeout)
        {
            UDTSession session = null;
            sessionHandoff.TryTake(out session, timeout);
            return session;
        }




        /**
         * single receive, run in the receiverThread, see {@link #start()}
         * <ul>
         * <li>Receives UDP packets from the network</li> 
         * <li>Converts them to UDT packets</li>
         * <li>dispatches the UDT packets according to their destination ID.</li>
         * </ul> 
         * @throws IOException
         */


        protected void DoReceive()
        {
            EndPoint remotePoint = new IPEndPoint(IPAddress.Any, 0);
            while (!stopped)
            {
                try
                {
                    try
                    {
                        //v.end();

                        //will block until a packet is received or timeout has expired

                       int len= dgSocket.ReceiveFrom(dp, ref remotePoint);

                        //v.begin();

                        Destination peer = new Destination(remotePoint);
                        IUDTPacket packet = PacketFactory.CreatePacket(dp, len);
                        lastPacket = packet;

                        //handle connection handshake 
                        if (packet.IsConnectionHandshake())
                        {
                            lock (lock_obj)
                            {
                                long id = packet.DestinationID;
                                UDTSession session = null;
                                sessions.TryGetValue(id, out session);
                                if (session == null)
                                {
                                    session = new ServerSession((IPEndPoint)remotePoint, this);
                                    AddSession(session.SocketID, session);
                                    //TODO need to check peer to avoid duplicate server session
                                    if (serverSocketMode)
                                    {
                                        FlashLogger.Info("Pooling new request.");
                                        sessionHandoff.Add(session);
                                        FlashLogger.Info("Request taken for processing.");
                                    }
                                }
                                peer.SocketID=((ConnectionHandshake)packet).SocketID;
                                session.Received(packet, peer);
                            }
                        }
                        else
                        {
                            //dispatch to existing session
                            long dest = packet.DestinationID;
                            UDTSession session;
                            if (dest == lastDestID)
                            {
                                session = lastSession;
                            }
                            else
                            {
                               // session =
                               sessions.TryGetValue(dest,out session);//cd 获取session
                                lastSession = session;
                                lastDestID = dest;
                            }
                            if (session == null)
                            {
                                n++;
                                if (n % 100 == 1)
                                {
                                    FlashLogger.Warn("Unknown session <" + dest + "> requested from <" + peer + "> packet type " + packet);
                                }
                            }
                            else
                            {
                                Console.WriteLine("收到包");
                                session.Received(packet, peer);
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        if (ex.Message.Equals("socket closed") && stopped)
                        {
                            //cd
                            //已经正常关闭
                        }
                        else
                        {
                            FlashLogger.Info("SocketException: " + ex.Message);
                        }
                    }
                    catch (Exception ste)
                    {
                        //can safely ignore... we will retry until the endpoint is stopped
                    }

                }
                catch (Exception ex)
                {
                    FlashLogger.Warn( "Got: " + ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// 发送网络数据
        /// </summary>
        /// <param name="packet"></param>
        internal void DoSend(IUDTPacket packet)
        {
            byte[] data = packet.GetEncoded();
           var dgp = packet.GetSession().Datagram;
            dgSocket.SendTo(data, dgp.Remote);
        }


        public string toString()
        {
            return "UDPEndpoint port=" + port;
        }

        public void SendRaw(UDPUserToken p)
        {
            dgSocket.SendTo(p.Buffer,p.Offset,p.Length,SocketFlags.None,  p.Remote);
        }
    }
}
