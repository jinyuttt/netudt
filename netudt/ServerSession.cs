using netudt.Log;
using netudt.packets;
using netudt.util;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
/**
* server side session in client-server mode
*/
namespace netudt
{
    public class ServerSession : UDTSession {

   

	    private UDPEndPoint endPoint;

        //last received packet (for testing purposes)
        private IUDTPacket lastPacket;

        private long cookie = 0;//cd 2018-08-28
        private string key = "judp";
        private string client = "";
        int n_handshake = 0;
        public ServerSession(IPEndPoint dp, UDPEndPoint endPoint):base("ServerSession localPort=" + endPoint.LocalPort + " peer=" + dp.Address.ToString() + ":" + dp.Port, new Destination(dp))
        {

       
		this.endPoint=endPoint;
		client=dp.Address+":"+dp.Port+":"+DateTime.Now.Ticks;
		FlashLogger.Info("Created "+toString()+" talking to "+dp.Address+":"+dp.Port);
	}

   


    public override void Received(IUDTPacket packet, Destination peer) {
        lastPacket = packet;
        if (packet is ConnectionHandshake) {
            ConnectionHandshake connectionHandshake = (ConnectionHandshake)packet;
                FlashLogger.Info("Received " + connectionHandshake);
              

            if (State <= ready) {
                destination.SocketID=connectionHandshake.SocketID;
                if (State <= handshaking) {
                        State=handshaking;
                }
                try {
                    HandleHandShake(connectionHandshake);
                    n_handshake++;
                    try {
                        //理论上应该先检验cookie
                       
                             State = ready;
                             socket = new UDTSocket(endPoint, this);
                              cc.Init();
                    } catch (Exception uhe) {
                        //session is invalid
                       
                            FlashLogger.Error("", uhe);
                       
                            State = invalid;
                    }
                } catch (IOException ex) {
                    //session invalid
                   
                        FlashLogger.Warn("Error processing ConnectionHandshake", ex);
                      
                        State = invalid;
                }
                return;
            }
            else
            {
                //cd  回复
                try {
                    HandleHandShake(connectionHandshake);
                } catch (IOException e) {
                   
                }
            }

        }else if (packet is KeepAlive) {
            socket.GetReceiver().ResetEXPTimer();
            active = true;
            return;
        }

        if (State== ready) {
            active = true;

            if (packet is KeepAlive) {
                //nothing to do here
                return;
            }else if (packet is Shutdown) {
                try {
                    socket.GetReceiver().Stop();
                } catch (IOException ex) {
                    FlashLogger.Warn("", ex);
                }
                State=shutdown;
               Console.WriteLine("SHUTDOWN ***");
                active = false;
                    FlashLogger.Info("Connection shutdown initiated by the other side.");
                return;
            }

			else{
                try {
                        Console.WriteLine("收到数据包");
                    if (packet.ForSender()) {
                        socket.GetSender().Receive(packet);
                    } else {
                        socket.GetReceiver().Receive(packet);
                    }
                } catch (Exception ex) {
                    //session invalid
                    FlashLogger.Error("", ex);
                        State = invalid;
                   // setState(invalid);
                }
            }
            return;

        }


    }

    /**
	 * for testing use only
	 */
    IUDTPacket getLastPacket() {
        return lastPacket;
    }

        /**
         * handle the connection handshake:<br/>
         * <ul>
         * <li>set initial sequence number</li>
         * <li>send response handshake</li>
         * </ul>
         * @param handshake
         * @param peer
         * @throws IOException
         */
        protected void HandleHandShake(ConnectionHandshake handshake)
        {
            ConnectionHandshake responseHandshake = new ConnectionHandshake();
            //compare the packet size and choose minimun
            long clientBufferSize = handshake.PacketSize;
            long myBufferSize = datagramSize;
            long bufferSize = Math.Min(clientBufferSize, myBufferSize);
            long initialSequenceNumber = handshake.InitialSeqNo;
            this.initialSequenceNumber = initialSequenceNumber;
            this.DatagramSize = (int)bufferSize;
            // setDatagramSize((int) bufferSize);
            responseHandshake.PacketSize = bufferSize;
            responseHandshake.UdtVersion = 4;
            responseHandshake.InitialSeqNo = initialSequenceNumber;
            responseHandshake.ConnectionType = -1;
            responseHandshake.MaxFlowWndSize = handshake.MaxFlowWndSize;
            //tell peer what the socket ID on this side is 
            responseHandshake.SocketID = mySocketID;
            responseHandshake.DestinationID = this.destination.SocketID;
            //cd 2018-08-28
            if (this.cookie == 0)
            {
                this.cookie = CreateCookie();
            }
            responseHandshake.Cookie = cookie;

            responseHandshake.SetSession(this);
            FlashLogger.Info("Sending reply " + responseHandshake);
            endPoint.DoSend(responseHandshake);
        }

        /**
         * cd 
        * @Title: createcCookie
        * @Description: 生产Cookie
        * @param @return    参数
        * @return long    返回类型
         */
        private long CreateCookie()
        {
            byte[] bytes = null;
            byte[] result = new byte[4];

            string src = Tools.String2MD5(client + key);
            try
            {
                bytes = Encoding.UTF8.GetBytes(src);
            }
            catch (Exception e)
            {
                bytes = Encoding.Default.GetBytes(src);
            }

            if (bytes.Length > 4)
            {
                Array.Copy(bytes, result, 4);
            }
            else
            {
                Array.Copy(bytes, result, bytes.Length);
            }
            return BitConverter.ToInt32(result, 0);
        }
}
}

