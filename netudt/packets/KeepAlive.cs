

namespace netudt.packets
{
    public class KeepAlive : ControlPacket
    {

        public KeepAlive()
        {
            this.controlPacketType =(int) ControlPacketType.KEEP_ALIVE;
        }


        public override byte[] EncodeControlInformation()
        {
            return null;
        }
    }
}
