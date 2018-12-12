using netudt.Log;
using netudt.packets;
using netudt.util;
using System;
using System.IO;
using System.Threading;

namespace netudt
{
    public class ClientSession : UDTSession {

       

        private UDPEndPoint endPoint;
        public volatile int connectNum = 0;//cd
        public ClientSession(UDPEndPoint endPoint, Destination dest) : base("ClientSession localPort=" + endPoint.LocalPort, dest)
        {
            this.endPoint = endPoint;
           // logger.info("Created " + toString());
        }

        /**
         * send connection handshake until a reply from server is received
         * TODO check for timeout
         * @throws InterruptedException
         * @throws IOException
         */

        public void Connect() {
            int n = 0;
            while (State != ready)
            {
                SendHandShake();
                if (State == invalid) throw new IOException("Can't connect!");
                n++;
                if (State != ready) Thread.Sleep(500);
            }

            cc.Init();
            //logger.info("Connected, " + n + " handshake packets sent");
            FlashLogger.Info("Connected, " + n + " handshake packets sent");
        }


        public override void Received(IUDTPacket packet, Destination peer) {

            lastPacket = packet;

            if (packet is ConnectionHandshake) {
                ConnectionHandshake hs = (ConnectionHandshake)packet;
                FlashLogger.Info("Received connection handshake from " + peer + "\n" + hs);
                if (State != ready) {
                    if (hs.ConnectionType == 1) {
                        try {
                            //TODO validate parameters sent by peer
                           // long peerSocketID = hs.SocketID;
                            // DestinationID = peerSocketID;
                            // destination(peerSocketID);
                            destination.SocketID = hs.SocketID;
                            SendConfirmation(hs);
                        } catch (Exception ex) {
                            FlashLogger.Warn( "Error creating socket", ex);
                           
                            State = invalid;
                        }
                        return;
                    }
                    else {
                        try {
                            //TODO validate parameters sent by peer
                            //理论上这里是getConnectionType==-1
                            // long peerSocketID = hs.getSocketID();
                            // destination.SetSocketID(peerSocketID);
                            // setState(ready);
                            destination.SocketID = hs.SocketID;
                            State = ready;
                            Thread.Sleep(50);
                            //多个握手序列，使用接收的第一个
                            this.initialSequenceNumber=hs.InitialSeqNo;//cd 必须重置
                            socket = new UDTSocket(endPoint, this);
                        } catch (Exception ex) {
                            FlashLogger.Warn( "Error creating socket", ex);
                          
                            State = invalid;
                        }
                        return;
                    }
                }
            }

            if (State == ready) {

                if (packet is Shutdown) {
                 
                    State = shutdown;
                    active = false;
                    FlashLogger.Info("Connection shutdown initiated by the other side.");
                    return;
                }
                active = true;
                try {
                    if (packet.ForSender()) {
                        socket.GetSender().Receive(lastPacket);
                    } else {
                        socket.GetReceiver().Receive(lastPacket);
                    }
                } catch (Exception ex) {
                    //session is invalid
                    FlashLogger.Error("Error in " + toString(), ex);
                    //setState(invalid);
                    State = invalid;
                }
                return;
            }
        }


        //handshake for connect
        protected void SendHandShake() {
            ConnectionHandshake handshake = new ConnectionHandshake();
            handshake.ConnectionType=ConnectionHandshake.CONNECTION_TYPE_REGULAR;
            handshake.SocketType=ConnectionHandshake.SOCKET_TYPE_DGRAM;
            long initialSequenceNo = SequenceNumber.Random();
              initialSequenceNumber=initialSequenceNo;
            handshake.InitialSeqNo=initialSequenceNo;
            handshake.PacketSize = datagramSize;
            handshake.SocketID=mySocketID;
            handshake.MaxFlowWndSize=flowWindowSize;
            //cd 2018-08-28
            handshake.SetPeerIP(this.endPoint.GetLocalAddress().Address.ToString());
            handshake.SetSession(this);
            FlashLogger.Info("Sending " + handshake);
            endPoint.DoSend(handshake);
            connectNum++;
        }

        internal UDTStatistics getStatistics()
        {
            throw new NotImplementedException();
        }

        //2nd handshake for connect
        protected void SendConfirmation(ConnectionHandshake hs) {
            ConnectionHandshake handshake = new ConnectionHandshake();
            handshake.ConnectionType=-1;
            handshake.SocketID=ConnectionHandshake.SOCKET_TYPE_DGRAM;
            handshake.InitialSeqNo = hs.InitialSeqNo;
            handshake.PacketSize = hs.PacketSize;
            handshake.SocketID = mySocketID;
            handshake.MaxFlowWndSize = flowWindowSize;
            //cd 2018-08-28
            handshake.Cookie = hs.Cookie;
            handshake.PeerIP = hs.PeerIP;
            //
            handshake.SetSession(this);
            FlashLogger.Info("Sending confirmation " + handshake);
            endPoint.DoSend(handshake);
        }


        public IUDTPacket GetLastPkt() {
            return lastPacket;
        }
    }

}
