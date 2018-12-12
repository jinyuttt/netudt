


using System;
using System.IO;

namespace netudt.packets
{
    public class MessageDropRequest : ControlPacket
    {
        //Bits 35-64: Message number

        private long msgFirstSeqNo;
        private long msgLastSeqNo;
        public long  MsgFirstSeqNo
        {
            get { return msgFirstSeqNo; }
            set { msgFirstSeqNo = value; }
        }

        public long MsgLastSeqNo
        {
            get { return msgLastSeqNo; }
            set { msgLastSeqNo = value; }
        }

        public MessageDropRequest()
        {
            this.controlPacketType =(int) ControlPacketType.MESSAGE_DROP_REQUEST;
        }

        public MessageDropRequest(byte[] controlInformation)
        {
            this.controlPacketType =(int) ControlPacketType.MESSAGE_DROP_REQUEST;
            //this.controlInformation=controlInformation;
            decode(controlInformation);
        }

        void decode(byte[] data)
        {
            msgFirstSeqNo = PacketUtil.Decode(data, 0);
            msgLastSeqNo = PacketUtil.Decode(data, 4);
        }

        //public long getMsgFirstSeqNo()
        //{
        //    return msgFirstSeqNo;
        //}

        //public void setMsgFirstSeqNo(long msgFirstSeqNo)
        //{
        //    this.msgFirstSeqNo = msgFirstSeqNo;
        //}

        //public long getMsgLastSeqNo()
        //{
        //    return msgLastSeqNo;
        //}

        //public void setMsgLastSeqNo(long msgLastSeqNo)
        //{
        //    this.msgLastSeqNo = msgLastSeqNo;
        //}


        public override byte[] EncodeControlInformation()
        {
            try
            {
                MemoryStream bos = new MemoryStream();
                bos.Write(PacketUtil.Encode(msgFirstSeqNo),0,8);
                bos.Write(PacketUtil.Encode(msgLastSeqNo),0,8);
                return bos.ToArray();
            }
            catch (Exception e)
            {
                // can't happen
                return null;
            }

        }


        public override  bool Equals(object obj)
        {
            if (this == obj)
                return true;
           if(!this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            MessageDropRequest other = (MessageDropRequest)obj;
            if (msgFirstSeqNo != other.msgFirstSeqNo)
                return false;
            if (msgLastSeqNo != other.msgLastSeqNo)
                return false;
            return true;
        }

    }

}
