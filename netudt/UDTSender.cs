

using System;
using System.Collections.Generic;
using netudt.packets;
using netudt.sender;
using netudt.util;
using System.Collections.Concurrent;
using System.Threading;
using netudt.Log;
using System.IO;
/**
* sender part of a UDT entity
* 
* @see UDTReceiver
*/
namespace netudt
{ 
public class UDTSender {

	

	private  UDPEndPoint endpoint;

	private  UDTSession session;

	private  UDTStatistics statistics;

	//senderLossList stores the sequence numbers of lost packets
	//fed back by the receiver through NAK pakets
	private  SenderLossList senderLossList;
	
	//sendBuffer stores the sent data packets and their sequence numbers
	private ConcurrentDictionary<long,DataPacket> sendBuffer;
	
	//sendQueue contains the packets to send
	private  BlockingCollection<DataPacket>sendQueue;
	
	//thread reading packets from send queue and sending them
	private Thread senderThread;

	//protects against races when reading/writing to the sendBuffer
	private  Object sendLock=new Object();

	//number of unacknowledged data packets
	private  int unacknowledged=0;

	//for generating data packet sequence numbers
	private  long currentSequenceNumber=0;

	//the largest data packet sequence number that has actually been sent out
	private  long largestSentSequenceNumber=-1;

	//last acknowledge number, initialised to the initial sequence number
	private  long lastAckSequenceNumber;

	private volatile bool started=false;

	private volatile bool stopped=false;

	private volatile bool paused=false;

	//used to signal that the sender should start to send
	private volatile CountdownEvent startLatch=new CountdownEvent(1);

	//used by the sender to wait for an ACK
	private CountdownEvent waitForAckLatch =new CountdownEvent(1);

	//used by the sender to wait for an ACK of a certain sequence number
	private CountdownEvent waitForSeqAckLatch =new CountdownEvent(1);

	private  bool storeStatistics;
	
	// cd
	private volatile int bufferNum=0;
	private	volatile bool isModify=true;

	public UDTSender(UDTSession session,UDPEndPoint endpoint){
		if(!session.IsReady)throw new IllegalStateException("UDTSession is not ready.");
		this.endpoint= endpoint;
		this.session=session;
		statistics=session.Statistics;
		senderLossList=new SenderLossList();
		sendBuffer=new ConcurrentDictionary<long, DataPacket>(session.FlowWindowSize,2); 
		sendQueue =new BlockingCollection<DataPacket>(1000);  
		lastAckSequenceNumber=session.InitialSequenceNumber;
		currentSequenceNumber=session.InitialSequenceNumber - 1;
		//waitForAckLatch.set(new CountDownLatch(1));
		//waitForSeqAckLatch.set(new CountDownLatch(1));
		//storeStatistics=bool.getbool("udt.sender.storeStatistics");
       
		InitMetrics();
		DoStart();
	}

	private MeanValue dgSendTime;
	private MeanValue dgSendInterval;
	private MeanThroughput throughput;
	private void InitMetrics(){
		if(!storeStatistics)return;
		dgSendTime=new MeanValue("Datagram send time");
		statistics.AddMetric(dgSendTime);
		dgSendInterval=new MeanValue("Datagram send interval");
		statistics.AddMetric(dgSendInterval);
		throughput=new MeanThroughput("Throughput", session.DatagramSize);
		statistics.AddMetric(throughput);
	}

	/**
	 * start the sender thread
	 */
	public void start(){
            FlashLogger.Info("Starting sender for "+session);
            startLatch.Signal();//减1
		    started=true;
	}

	//starts the sender algorithm
	private void DoStart(){
            senderThread = new Thread(() =>
              {
                  try
                  {
                      while (!stopped)
                      {
                        //wait until explicitely (re)started
                          startLatch.Wait();
                          paused = false;
                          SenderAlgorithm();
                      }
                  }

                  catch (IOException ex)
                  {

                      FlashLogger.Error("", ex);
                  }
                  FlashLogger.Info("STOPPING SENDER for " + session);
              });

            senderThread.Name = UDTThreadFactory.Instance.NewThreadName();
            senderThread.IsBackground = true;

             senderThread.Start();
	}


	/** 
	 * sends the given data packet, storing the relevant information
	 * 
	 * @param data
	 * @throws IOException
	 * @throws InterruptedException
	 */
	private void Send(DataPacket p){
		lock(sendLock){
			if(storeStatistics){
				dgSendInterval.End();
				dgSendTime.Begin();
			}
			endpoint.DoSend(p);
			if(storeStatistics){
				dgSendTime.End();
				dgSendInterval.Begin();
				throughput.End();
				throughput.Begin();
			}
			sendBuffer[p.GetPacketSequenceNumber()]= p;
           Interlocked.Increment(ref unacknowledged);
			
		}
		statistics.incNumberOfSentDataPackets();
	}

        /**
         * writes a data packet into the sendQueue, waiting at most for the specified time
         * if this is not possible due to a full send queue
         * 
         * @return <code>true</code>if the packet was added, <code>false</code> if the
         * packet could not be added because the queue was full
         * @param p
         * @param timeout
         * @param units
         * @return
         * @throws IOException
         * @throws InterruptedException
         */
        internal bool SendUdtPacket(DataPacket p, int timeout){
		if(!started)start();
		return sendQueue.TryAdd(p,timeout);
	}

	//receive a packet from server from the peer
	internal void Receive(IUDTPacket p){
		if (p is Acknowledgement) {
			Acknowledgement acknowledgement=(Acknowledgement)p;
			OnAcknowledge(acknowledgement);
		}
		else if (p is NegativeAcknowledgement) {
			NegativeAcknowledgement nak=(NegativeAcknowledgement)p;
			OnNAKPacketReceived(nak);
		}
		else if (p is KeepAlive) {
			session.Socket.GetReceiver().ResetEXPCount();
		}
	}

	protected void OnAcknowledge(Acknowledgement acknowledgement){
            waitForAckLatch.Wait();
            waitForSeqAckLatch.Wait();
            //cd 
            long ackNumber = acknowledgement.AckNumber;
		// cd
				if(this.session is ClientSession)
				{
					//cd 
				  if(this.isModify)
				  {
					//已经发送过10000包，不用再修正，认为接收方已经关闭
				   if(bufferNum<10000)
				   {
				    if(ackNumber/100000!=this.session.InitialSequenceNumber/100000)
				    {
				    	//认为不同段的seqNo,则不是通信的接收方session
						 statistics.incNumberOfACKReceived();
						 if(storeStatistics)statistics.StoreParameters();
					     return;
				    }
				   }
				   else
				   {
					   this.isModify=false;//不用再修正
				   }
				  }
				}
            ICongestionControl cc = session.CongestionControl;
            long rtt = acknowledgement.RoundTripTime;
		if(rtt>0){
                long rttVar = acknowledgement.RoundTripTimeVar;
			cc.SetRTT(rtt,rttVar);
			statistics.setRTT(rtt, rttVar);
		}
            long rate = acknowledgement.PacketReceiveRate;
		if(rate>0){
                long linkCapacity = acknowledgement.EstimatedLinkCapacity;
			cc.UpdatePacketArrivalRate(rate, linkCapacity);
			statistics.setPacketArrivalRate(cc.GetPacketArrivalRate(), cc.GetEstimatedLinkCapacity());
		}

		
		cc.OnACK(ackNumber);
		
		statistics.CongestionWindowSize=((long)cc.GetCongestionWindowSize());
		//need to remove all sequence numbers up the ack number from the sendBuffer
		bool removed=false;
            DataPacket packet = null;
		for(long s=lastAckSequenceNumber;s<ackNumber;s++){
			lock (sendLock) {
				removed=sendBuffer.TryRemove(s,out packet);
			}
			if(removed){
                    Interlocked.Decrement(ref unacknowledged);
				    bufferNum++;//cd
			}
		}
		
		lastAckSequenceNumber=Math.Max(lastAckSequenceNumber, ackNumber);
		
		//send ACK2 packet to the receiver
		SendAck2(ackNumber);
		//
		statistics.incNumberOfACKReceived();
		if(storeStatistics)statistics.StoreParameters();
	}

	/**
	 * procedure when a NAK is received (spec. p 14)
	 * @param nak
	 */
	protected void OnNAKPacketReceived(NegativeAcknowledgement nak){
		foreach(int i in nak.GetDecodedLossInfo()){
                senderLossList.Insert(i);
		}
		session.CongestionControl.OnLoss(nak.GetDecodedLossInfo());
		session.Socket.GetReceiver().ResetEXPTimer();
		statistics.incNumberOfNAKReceived();
	
		//if(logger.isLoggable(Level.FINER)){
		//	logger.finer("NAK for "+nak.getDecodedLossInfo().size()+" packets lost, " 
		//			+"set send period to "+session.getCongestionControl().getSendInterval());
		//}
		return;
	}

	//send single keep alive packet -> move to socket!
	protected void SendKeepAlive(){
		KeepAlive keepAlive = new KeepAlive();
		//TODO
		keepAlive.SetSession(session);
		endpoint.DoSend(keepAlive);
	}

	protected void SendAck2(long ackSequenceNumber){
		Acknowledgment2 ackOfAckPkt = new Acknowledgment2();
		ackOfAckPkt.AckSequenceNumber=ackSequenceNumber;
		ackOfAckPkt.SetSession(session);
		ackOfAckPkt.DestinationID=session.Destination.SocketID;
		endpoint.DoSend(ackOfAckPkt);
	}

	/**
	 * sender algorithm
	 */
	long iterationStart;
	public void SenderAlgorithm(){
		while(!paused){
			iterationStart=Util.getCurrentTime();
			
			//if the sender's loss list is not empty 
			if (!senderLossList.IsEmpty) {
                    long entry = senderLossList.GetFirstEntry();
				HandleResubmit(entry);
				FlashLogger.Info("senderLossList:"+entry);
			}

			else
			{
                    //if the number of unacknowledged data packets does not exceed the congestion 
                    //and the flow window sizes, pack a new packet
                    int unAcknowledged = unacknowledged;

                    if (unAcknowledged < session.CongestionControl.GetCongestionWindowSize()
						 && unAcknowledged<session.FlowWindowSize){
                        //check for application data
                        DataPacket dp = null;
                       sendQueue.TryTake(out dp,(int)Util.SYN);
					if(dp!=null){
						Send(dp);
                            largestSentSequenceNumber = dp.PacketSequenceNumber;
					}
					else{
						statistics.incNumberOfMissingDataEvents();
					}
				}else{
					//congestion window full, wait for an ack
					if(unAcknowledged>=session.CongestionControl.GetCongestionWindowSize()){
						statistics.incNumberOfCCWindowExceededEvents();
					}
					WaitForAck();
				}
			}

			//wait
			if(largestSentSequenceNumber % 16 !=0){
				long snd=(long)session.CongestionControl.GetSendInterval();
				long passed=Util.getCurrentTime()-iterationStart;
				int x=0;
				while(snd-passed>0){
					//can't wait with microsecond precision :(
					if(x==0){
						statistics.incNumberOfCCSlowDownEvents();
						x++;
					}
					passed=Util.getCurrentTime()-iterationStart;
					if(stopped)return;
				}
			}
		}
	}

	/**
	 * re-submits an entry from the sender loss list
	 * @param entry
	 */
	protected void HandleResubmit(long seqNumber){
		try {
                //retransmit the packet and remove it from  the list
                DataPacket pktToRetransmit = null;
             sendBuffer.TryGetValue(seqNumber,out pktToRetransmit);
			if(pktToRetransmit!=null){
				endpoint.DoSend(pktToRetransmit);
				statistics.incNumberOfRetransmittedDataPackets();
			}
		}catch (Exception e) {
			
                FlashLogger.Error("", e);
		}
	}

	/**
	 * for processing EXP event (see spec. p 13)
	 */
	internal void PutUnacknowledgedPacketsIntoLossList(){
		lock (sendLock) {
			foreach(long l in sendBuffer.Keys){
				senderLossList.Insert(l);
			}
		}
	}

	/**
	 * the next sequence number for data packets.
	 * The initial sequence number is "0"
	 */
	public long GetNextSequenceNumber(){
		currentSequenceNumber=SequenceNumber.Increment(currentSequenceNumber);
		return currentSequenceNumber;
	}

	public long GetCurrentSequenceNumber(){
		return currentSequenceNumber;
	}

	/**
	 * returns the largest sequence number sent so far
	 */
	public long GetLargestSentSequenceNumber(){
		return largestSentSequenceNumber;
	}
	/**
	 * returns the last Ack. sequence number 
	 */
	public long getLastAckSequenceNumber(){
		return lastAckSequenceNumber;
	}

        internal bool HaveAcknowledgementFor(long sequenceNumber){
		return SequenceNumber.Compare(sequenceNumber,lastAckSequenceNumber)<=0;
	}

        internal bool IsSentOut(long sequenceNumber)
        {
            return SequenceNumber.Compare(largestSentSequenceNumber, sequenceNumber) >= 0;
        }

        internal bool HaveLostPackets(){
		return !senderLossList.IsEmpty;
	}

	/**
	 * wait until the given sequence number has been acknowledged
	 * 
	 * @throws InterruptedException
	 */
	public void WaitForAck(long sequenceNumber){
		while(!session.IsShutdown && !HaveAcknowledgementFor(sequenceNumber)){
                waitForSeqAckLatch.Reset();
                waitForSeqAckLatch.Wait(10);

           // waitForSeqAckLatch.get().await(10, TimeUnit.MILLISECONDS);
		}
	}

	/**
	 * wait for the next acknowledge
	 * @throws InterruptedException
	 */
	public void WaitForAck(){
            waitForAckLatch.Reset();
            waitForAckLatch.Wait(2);

       // waitForAckLatch.get().await(2, TimeUnit.MILLISECONDS);
	}


	public void Stop(){
		stopped=true;
	}
	
	public void Pause(){
		startLatch=new CountdownEvent(1);
		paused=true;
	}
	
	/**
	 * 发送的数据清空
	 * cd
	 * @return
	 */
	public bool IsSenderEmpty()
	{
		if(senderLossList.IsEmpty&&sendBuffer.IsEmpty&&sendQueue.Count==0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
}
}
