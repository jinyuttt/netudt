/*********************************************************************************
 * Copyright (c) 2010 Forschungszentrum Juelich GmbH 
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * (1) Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the disclaimer at the end. Redistributions in
 * binary form must reproduce the above copyright notice, this list of
 * conditions and the following disclaimer in the documentation and/or other
 * materials provided with the distribution.
 * 
 * (2) Neither the name of Forschungszentrum Juelich GmbH nor the names of its 
 * contributors may be used to endorse or promote products derived from this 
 * software without specific prior written permission.
 * 
 * DISCLAIMER
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 *********************************************************************************/


using System;

namespace netudt.packets
{
    public class DataPacket : IUDTPacket, IComparable<IUDTPacket>
    {

	private byte[] data;
    private long packetSequenceNumber;
    private long messageNumber;
    private long timeStamp;
    private long destinationID;

    private UDTSession session;

   public byte[] Buffer
        {
            get { return data; }
            set { data = value; }
        }

        public  long PacketSequenceNumber
        {
            get { return packetSequenceNumber; }
            set { packetSequenceNumber = value; }
        }
        public long MessageNumber
        {
            get { return messageNumber; }
            set { messageNumber = value; }
        }

        public long DestinationID
        {
            get { return destinationID; }
            set { destinationID = value; }
        }

        public long TimeStamp
        {
            get { return timeStamp; }
            set { timeStamp = value; }
        }
    public DataPacket() {
    }

  
    public DataPacket(byte[] encodedData, int length=0) {
            if (length == 0)
            {
                length = encodedData.Length;
            }
            Decode(encodedData, length);
    }

    void Decode(byte[] encodedData, int length) {
        packetSequenceNumber = PacketUtil.Decode(encodedData, 0);
        messageNumber = PacketUtil.Decode(encodedData, 4);
        timeStamp = PacketUtil.Decode(encodedData, 8);
        destinationID = PacketUtil.Decode(encodedData, 12);
        data = new byte[length - 16];
       Array.Copy(encodedData, 16, data, 0, data.Length);
    }


    //public byte[] getData() {
    //    return this.data;
    //}

    public double getLength() {
        return data.Length;
    }

        /*
         * aplivation data
         * @param
         */

        //public void setData(byte[] data) {
        //    this.data = data;
        //}


        //public long getPacketSequenceNumber() {
        //    return this.packetSequenceNumber;
        //}

        //public void setPacketSequenceNumber(long sequenceNumber) {
        //    this.packetSequenceNumber = sequenceNumber;
        //}



        ////public long getMessageNumber() {
        ////    return this.messageNumber;
        ////}


        ////public void setMessageNumber(long messageNumber) {
        ////    this.messageNumber = messageNumber;
        ////}


        //public long getDestinationID() {
        //    return this.destinationID;
        //}


        //public long getTimeStamp() {
        //    return this.timeStamp;
        //}


        //public void setDestinationID(long destinationID) {
        //    this.destinationID = destinationID;
        //}


        //public void setTimeStamp(long timeStamp) {
        //    this.timeStamp = timeStamp;
        //}

        /**
         * complete header+data packet for transmission
         */

        public byte[] GetEncoded()
        {
            //header.length is 16
            byte[] result = new byte[16 + data.Length];
            Array.Copy(PacketUtil.Encode(packetSequenceNumber), 0, result, 0, 4);
            Array.Copy(PacketUtil.Encode(messageNumber), 0, result, 4, 4);
            Array.Copy(PacketUtil.Encode(timeStamp), 0, result, 8, 4);
            Array.Copy(PacketUtil.Encode(destinationID), 0, result, 12, 4);
            Array.Copy(data, 0, result, 16,data.Length);
            return result;
        }


    public bool IsControlPacket() {
        return false;
    }


    public bool ForSender() {
        return false;
    }


    public bool IsConnectionHandshake() {
        return false;
    }


    public int GetControlPacketType() {
        return -1;
    }


    public UDTSession GetSession() {
        return session;
    }

    public void SetSession(UDTSession session) {
        this.session = session;
    }


    public int CompareTo(IUDTPacket other) {
        return (int)(packetSequenceNumber - other.GetPacketSequenceNumber());
    }

        

        public long GetPacketSequenceNumber()
        {
            return PacketSequenceNumber;
        }
    }
}
