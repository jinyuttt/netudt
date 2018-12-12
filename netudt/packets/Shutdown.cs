


namespace netudt.packets
{
    public class Shutdown : ControlPacket
    {

        public Shutdown()
        {
            this.controlPacketType =(int) ControlPacketType.SHUTDOWN;
        }


        public override byte[] EncodeControlInformation()
        {
            return null;
        }


        public new bool ForSender()
        {
            return false;
        }
    }
}

