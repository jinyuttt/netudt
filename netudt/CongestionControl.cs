

using System.Collections.Generic;
/**
* congestion control interface
*/
namespace netudt
{
    public interface ICongestionControl
    {

        /**
         * Callback function to be called (only) at the start of a UDT connection.
         * when the UDT socket is conected 
         */
          void Init();

        /**
         * set roundtrip time and associated variance
         * @param rtt - round trip time in microseconds
         * @param rttVar - round trip time variance in microseconds
         */
       void SetRTT(long rtt, long rttVar);

        /**
         * update packet arrival rate and link capacity with the
         * values received in an ACK packet
         * @param rate - packet rate in packets per second
         * @param linkCapacity - estimated link capacity in packets per second
         */
       void UpdatePacketArrivalRate(long rate, long linkCapacity);

        /**
         * get the current value of the packet arrival 
         */
         long GetPacketArrivalRate();

        /**
         * get the current value of the estimated link capacity 
         */
         long GetEstimatedLinkCapacity();

        /**
         * get the current value of the inter-packet interval in microseconds
         */
        double GetSendInterval();

        /**
         * get the congestion window size
         */
        double GetCongestionWindowSize();

        /**
         * get the ACK interval. If larger than 0, the receiver should acknowledge
         * every n'th packet
         */
         long GetAckInterval();

        /**
         * set the ACK interval. If larger than 0, the receiver should acknowledge
         * every n'th packet
         */
         void SetAckInterval(long ackInterval);

        /**
         * Callback function to be called when an ACK packet is received.
         * @param ackSeqno - the data sequence number acknowledged by this ACK.
         * see spec. page(16-17)
         */
         void OnACK(long ackSeqno);

        /**
         * Callback function to be called when a loss report is received.
         * @param lossInfo - list of sequence number of packets
         */
         void OnLoss(List<int> lossInfo);

        /**
         * Callback function to be called when a timeout event occurs
         */
         void OnTimeout();

        /**
         * Callback function to be called when a data packet is sent.
         * @param packetSeqNo - the data packet sequence number
         */
        void OnPacketSend(long packetSeqNo);

        /**
         * Callback function to be called when a data packet is received.
         * @param packetSeqNo - the data packet sequence number.
         */
        void OnPacketReceive(long packetSeqNo);

        /**
         * Callback function to be called when a UDT connection is closed.
         */
         void Close();
    }
}