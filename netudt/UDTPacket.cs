using System;
namespace netudt
{
    public interface IUDTPacket : IComparable<IUDTPacket>
    {

        long MessageNumber { get; set; }
        long TimeStamp { get; set; }
        long DestinationID { get; set; }
         //long getMessageNumber();

         //void setMessageNumber(long messageNumber);

         //void setTimeStamp(long timeStamp);

         //long getTimeStamp();

         //void setDestinationID(long destinationID);

         //long getDestinationID();

         bool IsControlPacket();

         int GetControlPacketType();

         byte[] GetEncoded();

    
         bool ForSender();

         bool IsConnectionHandshake();

         UDTSession GetSession();

         long GetPacketSequenceNumber();
      
    }
}
