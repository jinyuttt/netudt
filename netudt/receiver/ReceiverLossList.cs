



using netudt.util;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
namespace netudt.receiver
{
    public class ReceiverLossList {

        private  ConcurrentQueue<ReceiverLossListEntry> backingList;
        private Dictionary<ReceiverLossListEntry,string> keys = null;
        private Dictionary<ReceiverLossListEntry, string> removeKeys = null;
        public ReceiverLossList() {
            backingList = new ConcurrentQueue<ReceiverLossListEntry>();
            keys = new Dictionary<ReceiverLossListEntry, string>(32);
            removeKeys = new Dictionary<ReceiverLossListEntry, string>();
        }

        public void Insert(ReceiverLossListEntry entry) {
            lock(backingList) {
                if (!keys.ContainsKey(entry)) {
                    backingList.Enqueue(entry);
                    keys[entry] = null;
                }
            }
        }

        public void remove(long seqNo) {
            //  backingList.Add(new ReceiverLossListEntry(seqNo),null);
            removeKeys.Add(new ReceiverLossListEntry(seqNo), null);


        }

        public bool Contains(ReceiverLossListEntry obj) {
            // return backingList.contains(obj);
            return keys.ContainsKey(obj);
        }

        public bool IsEmpty {
            get { return backingList.Count == 0; }
        }

        /**
         * read (but NOT remove) the first entry in the loss list
         * @return
         */
        public ReceiverLossListEntry getFirstEntry() {
            ReceiverLossListEntry item = null;
            backingList.TryDequeue(out item);
            return item;
        }

        public int Size {
            get { return backingList.Count; }
        }

        /**
         * return all sequence numbers whose last feedback time is larger than k*RTT
         * 
         * @param RTT - the current round trip time
         * @param doFeedback - true if the k parameter should be increased and the time should 
         * be reset (using {@link ReceiverLossListEntry#feedback()} )
         * @return
         */
        public List<long> getFilteredSequenceNumbers(long RTT, bool doFeedback) {
            List<long> result = new List<long>();
            ReceiverLossListEntry[] sorted = new ReceiverLossListEntry[backingList.Count];
                backingList.CopyTo(sorted,0);
            Array.Sort(sorted);
            foreach (ReceiverLossListEntry e in sorted) {
                if ((Util.getCurrentTime() - e.getLastFeedbackTime()) > e.getK() * RTT) {
                    result.Add(e.GetSequenceNumber());
                    if (doFeedback) e.Feedback();
                }
            }
            return result;
        }


        public string toString() {
            return backingList.ToString();
        }
    }
	
}
