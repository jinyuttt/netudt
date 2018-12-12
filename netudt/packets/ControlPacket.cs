
using System;
using netudt;
namespace netudt.packets
{
    public abstract class ControlPacket : IUDTPacket
    {
        protected int controlPacketType;

        protected long messageNumber;

        protected long timeStamp;

        protected long destinationID;

        protected byte[] controlInformation;

        private UDTSession session;
        public long MessageNumber
        {
            get { return messageNumber; }
            set { messageNumber = value; }
        }
        public long TimeStamp
        {
            get { return timeStamp; }
            set { timeStamp = value; }
        }
        public long DestinationID
        {
            get { return destinationID; }
            set { destinationID = value; }
        }
        public ControlPacket()
        {

        }


        public int GetControlPacketType()
        {
            return controlPacketType;
        }


        //public long getMessageNumber() {
        //    return messageNumber;
        //}

        //public void setMessageNumber(long messageNumber) {
        //    this.messageNumber = messageNumber;
        //}



        //public long getTimeStamp() {
        //    return timeStamp;
        //}


        //public void setTimeStamp(long timeStamp) {
        //    this.timeStamp = timeStamp;
        //}



        //public long getDestinationID() {
        //    return destinationID;
        //}

        //public void setDestinationID(long destinationID) {
        //    this.destinationID = destinationID;
        //}


        /**
         * return the header according to specification p.5
         * @return
         */
        byte[] GetHeader()
        {
            byte[] res = new byte[16];
            Array.Copy(PacketUtil.EncodeControlPacketType(controlPacketType), 0, res, 0, 4);
            Array.Copy(PacketUtil.Encode(GetAdditionalInfo()), 0, res, 4, 4);
            Array.Copy(PacketUtil.Encode(timeStamp), 0, res, 8, 4);
            Array.Copy(PacketUtil.Encode(destinationID), 0, res, 12, 4);
            return res;
        }

        /**
         * this method gets the "additional info" for this type of control packet
         */
        protected long GetAdditionalInfo()
        {
            return 0L;
        }


        /**
         * this method builds the control information
         * from the control parameters
         * @return
         */
        public abstract byte[] EncodeControlInformation();

        /**
         * complete header+ControlInformation packet for transmission
         */


        public byte[] GetEncoded()
        {
            byte[] header = GetHeader();
            byte[] controlInfo = EncodeControlInformation();
            byte[] result = controlInfo != null ?
                    new byte[header.Length + controlInfo.Length] :
                    new byte[header.Length];
            Array.Copy(header, 0, result, 0, header.Length);
            if (controlInfo != null)
            {
                Array.Copy(controlInfo, 0, result, header.Length, controlInfo.Length);
            }
            return result;

        }


        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if ((obj.GetType().Equals(this.GetType())) == false)
            {
                return false;
            }
            ControlPacket other = (ControlPacket)obj;
            if (controlPacketType != other.controlPacketType)
                return false;
            if (destinationID != other.destinationID)
                return false;
            if (timeStamp != other.timeStamp)
                return false;
            return true;
        }


        public virtual bool IsControlPacket()
        {
            return true;
        }


        public virtual bool ForSender()
        {
            return true;
        }


        public virtual bool IsConnectionHandshake()
        {
            return false;
        }


        public UDTSession GetSession()
        {
            return session;
        }

        public void SetSession(UDTSession session)
        {
            this.session = session;
        }


        public virtual long GetPacketSequenceNumber()
        {
            return -1;
        }


        public int CompareTo(IUDTPacket other)
        {
            return (int)(GetPacketSequenceNumber() - other.GetPacketSequenceNumber());
        }

    }
}
