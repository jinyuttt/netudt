using netudt.util;
using System;
using System.IO;
using System.Text;

namespace netudt.packets
{
    public class ConnectionHandshake : ControlPacket {
        private long udtVersion = 4;

        public static long SOCKET_TYPE_STREAM = 0;

        public static long SOCKET_TYPE_DGRAM = 1;

        private long socketType = SOCKET_TYPE_DGRAM; //stream or dgram

        private long initialSeqNo = 0;
        private long packetSize;
        private long maxFlowWndSize;

        public static long CONNECTION_TYPE_REGULAR = 1;

        public static long CONNECTION_TYPE_RENDEZVOUS = 0;

        private long connectionType = CONNECTION_TYPE_REGULAR;//regular or rendezvous mode

        private long socketID;

        private long cookie = 0;

        private long[] peerIP = new long[4];//cd 2018-08-28
        public long UdtVersion
        {
            get { return udtVersion; }
            set { udtVersion = value; }
        }
        public long SocketType
        {
            get { return socketType; }
            set { socketType = value; }
        }
        public long InitialSeqNo
        {
            get { return initialSeqNo; }
            set { initialSeqNo = value; }
        }

        public long PacketSize
        {
            get { return packetSize; }
            set { packetSize = value; }
        }
        public long MaxFlowWndSize
        {
            get { return maxFlowWndSize; }
            set { maxFlowWndSize = value; }
        }
        public long ConnectionType
        {
            get { return connectionType; }
            set { connectionType = value; }
        }
        public long SocketID
        {
            get { return socketID; }
            set { socketID = value; }
        }

        public long Cookie
        {
            get { return cookie; }
            set { cookie = value; }
        }

        public long[] PeerIP
        {
            get { return peerIP; }
            set { peerIP = value; }
        }
        public ConnectionHandshake() {
            this.controlPacketType =(int) ControlPacketType.CONNECTION_HANDSHAKE;
        }

        public ConnectionHandshake(byte[] controlInformation) {
            this.controlPacketType = (int)ControlPacketType.CONNECTION_HANDSHAKE;
            Decode(controlInformation);
        }

        //faster than instanceof...

        public override bool IsConnectionHandshake() {
            return true;
        }

        void Decode(byte[] data) {
            udtVersion = PacketUtil.Decode(data, 0);
            socketType = PacketUtil.Decode(data, 4);
            initialSeqNo = PacketUtil.Decode(data, 8);
            packetSize = PacketUtil.Decode(data, 12);
            maxFlowWndSize = PacketUtil.Decode(data, 16);
            connectionType = PacketUtil.Decode(data, 20);
            socketID = PacketUtil.Decode(data, 24);
            if (data.Length > 28) {
                cookie = PacketUtil.Decode(data, 28);
            }
           
            if (data.Length > 32)
            {
                //IP6
                peerIP[0] = PacketUtil.Decode(data, 32);

                peerIP[1] = PacketUtil.Decode(data, 36);

                peerIP[2] = PacketUtil.Decode(data, 40);

                peerIP[3] = PacketUtil.Decode(data, 44);
            }
        }
       
       
        /// <summary>
        /// IP6
        /// </summary>
        /// <param name="addr"></param>
        public void SetPeerIP(string addr)
        {
            this.peerIP = Tools.IPtoPeer(addr);
        }
        
        public override byte[] EncodeControlInformation() {
            try {
                MemoryStream bos = new MemoryStream(24);
                byte[] buffer = null;
                buffer = PacketUtil.Encode(udtVersion);
                bos.Write(buffer, 0, buffer.Length);
                buffer = PacketUtil.Encode(socketType);
                bos.Write(buffer, 0, buffer.Length);
                buffer = PacketUtil.Encode(initialSeqNo);
                bos.Write(buffer, 0, buffer.Length);
                buffer = PacketUtil.Encode(packetSize);
                bos.Write(buffer, 0, buffer.Length);
                buffer = PacketUtil.Encode(maxFlowWndSize);
                bos.Write(buffer, 0, buffer.Length);
                buffer = PacketUtil.Encode(connectionType);
                bos.Write(buffer, 0, buffer.Length);
                buffer = PacketUtil.Encode(socketID);
                bos.Write(buffer, 0, buffer.Length);
                buffer = PacketUtil.Encode(cookie);
                bos.Write(buffer, 0, buffer.Length);
                //bos.Write(PacketUtil.encode(socketType));
                //bos.Write(PacketUtil.encode(initialSeqNo));
                //bos.Write(PacketUtil.encode(packetSize));
                //bos.Write(PacketUtil.encode(maxFlowWndSize));
                //bos.Write(PacketUtil.encode(connectionType));
                //bos.Write(PacketUtil.encode(socketID));
               // bos.Write(PacketUtil.encode(cookie));//cd 2018-08-28
                for (int i = 0; i < 4; i++)
                {
                    buffer = PacketUtil.Encode(peerIP[i]);
                    bos.Write(buffer, 0, buffer.Length);
                    //bos.write(PacketUtil.encode(peerIP[i]));
                }
                buffer = bos.ToArray();
                bos.Close();
                return buffer;
            } catch (Exception e) {
                // can't happen
                return null;
            }

        }


        
        public override bool Equals(object obj) {
            if (this == obj)
                return true;
            //if (!super.equals(obj))
            //    return false;
            //if (getClass() != obj.getClass())
            //    return false;
            if ((obj.GetType().Equals(this.GetType())) == false)
            {
                return false;
            }
            ConnectionHandshake other = (ConnectionHandshake)obj;
            if (connectionType != other.connectionType)
                return false;
            if (initialSeqNo != other.initialSeqNo)
                return false;
            if (maxFlowWndSize != other.maxFlowWndSize)
                return false;
            if (packetSize != other.packetSize)
                return false;
            if (socketID != other.socketID)
                return false;
            if (socketType != other.socketType)
                return false;
            if (udtVersion != other.udtVersion)
                return false;
            return true;
        }


        
        public string toString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("ConnectionHandshake [");
            sb.Append("connectionType=").Append(connectionType);
            UDTSession session = GetSession();
            if (session != null) {
                sb.Append(", ");
                sb.Append(session.Destination);
            }
            sb.Append(", mySocketID=").Append(socketID);
            sb.Append(", initialSeqNo=").Append(initialSeqNo);
            sb.Append(", packetSize=").Append(packetSize);
            sb.Append(", maxFlowWndSize=").Append(maxFlowWndSize);
            sb.Append(", socketType=").Append(socketType);
            sb.Append(", destSocketID=").Append(destinationID);
            if (cookie > 0) sb.Append(", cookie=").Append(cookie);
            sb.Append("]");
            return sb.ToString();
        }

    } 

}
