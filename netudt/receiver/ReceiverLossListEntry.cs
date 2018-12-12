

using netudt.util;
using System;
/**
* an entry in the {@link ReceiverLossList}
*/
namespace netudt.receiver
{
    public class ReceiverLossListEntry : IComparable<ReceiverLossListEntry> {

        private long sequenceNumber;
        private long lastFeedbacktime;
        private long k = 2;

        /**
         * constructor
         * @param sequenceNumber
         */
        public ReceiverLossListEntry(long sequenceNumber) {
            if (sequenceNumber <= 0) {
                throw new IllegalArgumentException("Got sequence number " + sequenceNumber);
            }
            this.sequenceNumber = sequenceNumber;
            this.lastFeedbacktime = Util.getCurrentTime();
        }


        /**
         * call once when this seqNo is fed back in NAK
         */
        public void Feedback() {
            k++;
            lastFeedbacktime = Util.getCurrentTime();
        }

        public long GetSequenceNumber() {
            return sequenceNumber;
        }

        /**
         * k is initialised as 2 and increased by 1 each time the number is fed back
         * @return k the number of times that this seqNo has been feedback in NAK
         */
        public long getK() {
            return k;
        }

        public long getLastFeedbackTime() {
            return lastFeedbacktime;
        }

        /**
         * order by increasing sequence number
         */

        public int CompareTo(ReceiverLossListEntry o) {
            return (int)(sequenceNumber - o.sequenceNumber);
        }



        public string toString() {
            return sequenceNumber + "[k=" + k + ",time=" + lastFeedbacktime + "]";
        }



        public override int GetHashCode()
        {

            int prime = 31;
            int result = 1;
            result = prime * result + (int)(k ^ (k.RightMove(32)));
            result = prime * result
                    + (int)(sequenceNumber ^ (sequenceNumber.RightMove(32)));
            return result;
        }



        public override bool Equals(object obj) {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (!this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            ReceiverLossListEntry other = (ReceiverLossListEntry)obj;
            if (sequenceNumber != other.sequenceNumber)
                return false;
            return true;
        }
    }
}
