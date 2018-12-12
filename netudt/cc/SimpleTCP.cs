

using System.Collections.Generic;
/**
* simple TCP CC algorithm from the paper 
* "Optimizing UDP-based Protocol Implementations" by Y. Gu and R. Grossmann
*/
namespace netudt.cc
{
    public class SimpleTCP : UDTCongestionControl {

        public SimpleTCP(UDTSession session):base(session) {
           
        }

       
        public void init() {
            packetSendingPeriod = 0;
            congestionWindowSize = 2;
            SetAckInterval(2);
        }

       
        public new void OnACK(long ackSeqno) {
            congestionWindowSize += 1 / congestionWindowSize;
        }

       
        public void OnLoss(List<int> lossInfo) {
            congestionWindowSize *= 0.5;
        }

    }
}
