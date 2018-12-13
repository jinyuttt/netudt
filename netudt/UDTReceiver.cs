using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using netudt.Log;
using netudt.packets;
using netudt.receiver;
using netudt.util;
/**
* receiver part of a UDT entity
* @see UDTSender
*/
namespace netudt
{ 
public class UDTReceiver {

	

	private  UDPEndPoint endpoint;

	private  UDTSession session;

	private  UDTStatistics statistics;

	//record seqNo of detected lostdata and latest feedback time
	private  ReceiverLossList receiverLossList;

	//record each sent ACK and the sent time 
	private  AckHistoryWindow ackHistoryWindow;

	//Packet history window that stores the time interval between the current and the last seq.
	private  PacketHistoryWindow packetHistoryWindow;

	//for storing the arrival time of the last received data packet
	private  long lastDataPacketArrivalTime=0;

	//largest received data packet sequence number(LRSN)
	private  long largestReceivedSeqNumber=0;

	//ACK event related

	//last Ack number
	private long lastAckNumber=0;

	//largest Ack number ever acknowledged by ACK2
	private  long largestAcknowledgedAckNumber=-1;

	//EXP event related

	//a variable to record number of continuous EXP time-out events 
	private  long expCount=0;

	/*records the time interval between each probing pair
    compute the median packet pair interval of the last
	16 packet pair intervals (PI) and the estimate link capacity.(packet/s)*/
	private  PacketPairWindow packetPairWindow;

	//estimated link capacity
	long estimateLinkCapacity;
	// the packet arrival rate
	long packetArrivalSpeed;

	//round trip time, calculated from ACK/ACK2 pairs
	long roundTripTime=0;
	//round trip time variance
	long roundTripTimeVar=0;

	//to check the ACK, NAK, or EXP timer
	private long nextACK;
	//microseconds to next ACK event
	private long ackTimerInterval=Util.GetSYNTime();

	private long nextNAK;
	//microseconds to next NAK event
	private long nakTimerInterval=Util.GetSYNTime();

	private long nextEXP;
	//microseconds to next EXP event
	private long expTimerInterval=100*Util.GetSYNTime();

	//instant when the session was created (for expiry checking)
	private  long sessionUpSince;
	//milliseconds to timeout a new session that stays idle
	private  long IDLE_TIMEOUT = 3*60*1000;

	//buffer size for storing data
	private  long bufferSize;

	//stores received packets to be sent
	private  BlockingCollection<IUDTPacket>handoffQueue;

	private Thread receiverThread;

	private volatile bool stopped=false;

	//(optional) ack interval (see CongestionControl interface)
	private  long ackInterval=-1;

	/**
	 * if set to true connections will not expire, but will only be
	 * closed by a Shutdown message
	 */
	public static bool connectionExpiryDisabled=false;

	private  bool storeStatistics;
        private  long ackSequenceNumber = 0;

        private const int TicksMS = 10000;
	
	
	/**
	 * create a receiver with a valid {@link UDTSession}
	 * @param session
	 */
	public UDTReceiver(UDTSession session,UDPEndPoint endpoint){
		this.endpoint = endpoint;
		this.session=session;
            this.sessionUpSince = DateTime.Now.Ticks;
            this.statistics = session.Statistics;
		if(!session.IsReady)throw new IllegalStateException("UDTSession is not ready.");
		ackHistoryWindow = new AckHistoryWindow(16);
		packetHistoryWindow = new PacketHistoryWindow(16);
		receiverLossList = new ReceiverLossList();
		packetPairWindow = new PacketPairWindow(16);
		largestReceivedSeqNumber=session.InitialSequenceNumber-1;
            bufferSize = session.ReceiveBufferSize;
		handoffQueue=new BlockingCollection<IUDTPacket>(4*session.FlowWindowSize);
		//storeStatistics=bool.getbool("udt.receiver.storeStatistics");
		InitMetrics();
		Start();
	}
	
	private MeanValue dgReceiveInterval;
	private MeanValue dataPacketInterval;
	private MeanValue processTime;
	private MeanValue dataProcessTime;
	private void InitMetrics(){
		if(!storeStatistics)return;
		dgReceiveInterval=new MeanValue("UDT receive interval");
		statistics.AddMetric(dgReceiveInterval);
		dataPacketInterval=new MeanValue("Data packet interval");
		statistics.AddMetric(dataPacketInterval);
		processTime=new MeanValue("UDT packet process time");
		statistics.AddMetric(processTime);
		dataProcessTime=new MeanValue("Data packet process time");
		statistics.AddMetric(dataProcessTime);
	}


        //starts the sender algorithm
        private void Start() {
            receiverThread = new Thread(() => {

                try {
                    nextACK = Util.getCurrentTime() + ackTimerInterval;
                    nextNAK = (long)(Util.getCurrentTime() + 1.5 * nakTimerInterval);
                    nextEXP = Util.getCurrentTime() + 2 * expTimerInterval;
                    ackInterval = session.CongestionControl.GetAckInterval();
                    while (!stopped) {
                        ReceiverAlgorithm();
                    }
                }
                catch (Exception ex) {

                    FlashLogger.Error("", ex);

                }
                FlashLogger.Info("STOPPING RECEIVER for " + session);

            });
            receiverThread.Name = UDTThreadFactory.Instance.NewThreadName();
            receiverThread.IsBackground = true;
            receiverThread.Start();
        }

	/*
	 * packets are written by the endpoint
	 */
	internal void Receive(IUDTPacket p){
		if(storeStatistics)dgReceiveInterval.End();
		handoffQueue.Add(p);
		if(storeStatistics)dgReceiveInterval.Begin();
	}

	/**
	 * receiver algorithm 
	 * see specification P11.
	 */
	public void ReceiverAlgorithm(){
		//check ACK timer
		long currentTime=Util.getCurrentTime();
		if(nextACK<currentTime){
			nextACK=currentTime+ackTimerInterval;
			ProcessACKEvent(true);
		}
		//check NAK timer
		if(nextNAK<currentTime){
			nextNAK=currentTime+nakTimerInterval;
			ProcessNAKEvent();
			
		}

		//check EXP timer
		if(nextEXP<currentTime){
			nextEXP=currentTime+expTimerInterval;
			ProcessEXPEvent();
		}
            //perform time-bounded UDP receive
            IUDTPacket packet = null;
                handoffQueue.TryTake(out packet,(int)Util.GetSYNTime());
		if(packet!=null){
			//reset exp count to 1
			expCount=1;
			//If there is no unacknowledged data packet, or if this is an 
			//ACK or NAK control packet, reset the EXP timer.
			bool needEXPReset=false;
			if(packet.IsControlPacket()){
				ControlPacket cp=(ControlPacket)packet;
				int cpType=cp.GetControlPacketType();
				if(cpType==(int)ControlPacketType.ACK || cpType==(int)ControlPacketType.NAK){
					needEXPReset=true;
				}
			}
			if(needEXPReset){
				nextEXP=Util.getCurrentTime()+expTimerInterval;
			}
			if(storeStatistics)processTime.Begin();
			
			ProcessUDTPacket(packet);
			
			if(storeStatistics)processTime.End();
		}

            Thread.Yield();
	}

	/**
	 * process ACK event (see spec. p 12)
	 */
	protected void ProcessACKEvent(bool isTriggeredByTimer){
		//(1).Find the sequence number *prior to which* all the packets have been received
		 long ackNumber;
		ReceiverLossListEntry entry=receiverLossList.getFirstEntry();
		if (entry==null) {
			ackNumber = largestReceivedSeqNumber + 1;
		} else {
			ackNumber = entry.GetSequenceNumber();
		}
		//(2).a) if ackNumber equals to the largest sequence number ever acknowledged by ACK2
		if (ackNumber == largestAcknowledgedAckNumber){
			//do not send this ACK
			return;
		}else if (ackNumber==lastAckNumber) {
			//or it is equals to the ackNumber in the last ACK  
			//and the time interval between these two ACK packets
			//is less than 2 RTTs,do not send(stop)
			long timeOfLastSentAck=ackHistoryWindow.getTime(lastAckNumber);
			if(Util.getCurrentTime()-timeOfLastSentAck< 2*roundTripTime){
				return;
			}
		}
		 long ackSeqNumber;
		//if this ACK is not triggered by ACK timers,send out a light Ack and stop.
		if(!isTriggeredByTimer){
			ackSeqNumber=SendLightAcknowledgment(ackNumber);
			return;
		}
		else{
			//pack the packet speed and link capacity into the ACK packet and send it out.
			//(7).records  the ACK number,ackseqNumber and the departure time of
			//this Ack in the ACK History Window
			ackSeqNumber=sendAcknowledgment(ackNumber);
		}
		AckHistoryEntry sentAckNumber= new AckHistoryEntry(ackSeqNumber,ackNumber,Util.getCurrentTime());
		ackHistoryWindow.Add(sentAckNumber);
		//store ack number for next iteration
		lastAckNumber=ackNumber;
	}

	/**
	 * process NAK event (see spec. p 13)
	 */
	protected void ProcessNAKEvent(){
		//find out all sequence numbers whose last feedback time larger than is k*RTT
		List<long>seqNumbers=receiverLossList.getFilteredSequenceNumbers(roundTripTime,true);
		SendNAK(seqNumbers);
	}

	/**
	 * process EXP event (see spec. p 13)
	 */
	protected void ProcessEXPEvent(){
		if(session.Socket==null)return;
		UDTSender sender=session.Socket.GetSender();
		//put all the unacknowledged packets in the senders loss list
		sender.PutUnacknowledgedPacketsIntoLossList();
		if(expCount>16 && (DateTime.Now.Ticks-sessionUpSince)/TicksMS > IDLE_TIMEOUT){
			if(!connectionExpiryDisabled &&!stopped){
				SendShutdown();
                    Stop();
				FlashLogger.Info("Session "+session+" expired.");
				return;
			}
		}
		if(!sender.HaveLostPackets()){
			SendKeepAlive();
		}
		expCount++;
	}

	protected void ProcessUDTPacket(IUDTPacket p){
		//(3).Check the packet type and process it according to this.
		
		if(!p.IsControlPacket()){
			DataPacket dp=(DataPacket)p;
			if(storeStatistics){
                    dataPacketInterval.End();
                    dataProcessTime.Begin();
			}
			OnDataPacketReceived(dp);
			if(storeStatistics){
                    dataProcessTime.End();
                    dataPacketInterval.Begin();
			}
			
		}

		else if (p.GetControlPacketType()==(int)ControlPacketType.ACK2){
			Acknowledgment2 ack2=(Acknowledgment2)p;
			onAck2PacketReceived(ack2);
		}

		else if (p is Shutdown){
			OnShutdown();
		}

	}

	//every nth packet will be discarded... for testing only of course
	public static int dropRate=0;
	
	//number of received data packets
	private int n=0;
	
	protected void OnDataPacketReceived(DataPacket dp){
            long currentSequenceNumber = dp.PacketSequenceNumber;
		
		//for TESTING : check whether to drop this packet
//		n++;
//		//if(dropRate>0 && n % dropRate == 0){
//			if(n % 1111 == 0){	
//				logger.info("**** TESTING:::: DROPPING PACKET "+currentSequenceNumber+" FOR TESTING");
//				return;
//			}
//		//}
		bool OK=session.Socket.GetInputStream().HaveNewData(currentSequenceNumber,dp.Buffer);
		if(!OK){
		
			//need to drop packet...
			return;
		}
		
		long currentDataPacketArrivalTime = Util.getCurrentTime();

		/*(4).if the seqNo of the current data packet is 16n+1,record the
		time interval between this packet and the last data packet
		in the packet pair window*/
		if((currentSequenceNumber%16)==1 && lastDataPacketArrivalTime>0){
			long interval=currentDataPacketArrivalTime -lastDataPacketArrivalTime;
			packetPairWindow.Add(interval);
		}
		
		//(5).record the packet arrival time in the PKT History Window.
		packetHistoryWindow.Add(currentDataPacketArrivalTime);

		
		//store current time
		lastDataPacketArrivalTime=currentDataPacketArrivalTime;
		
		
		//(6).number of detected lossed packet
		/*(6.a).if the number of the current data packet is greater than LSRN+1,
			put all the sequence numbers between (but excluding) these two values
			into the receiver's loss list and send them to the sender in an NAK packet*/
		if(SequenceNumber.Compare(currentSequenceNumber,largestReceivedSeqNumber+1)>0){
			SendNAK(currentSequenceNumber);
			
		}
		else if(SequenceNumber.Compare(currentSequenceNumber,largestReceivedSeqNumber)<0){
				/*(6.b).if the sequence number is less than LRSN,remove it from
				 * the receiver's loss list
				 */
				receiverLossList.remove(currentSequenceNumber);
				
		}
		
		statistics.incNumberOfReceivedDataPackets();

		//(7).Update the LRSN
		if(SequenceNumber.Compare(currentSequenceNumber,largestReceivedSeqNumber)>0){
			largestReceivedSeqNumber=currentSequenceNumber;
		}

		//(8) need to send an ACK? Some cc algorithms use this
		if(ackInterval>0){
			if(n % ackInterval == 0)ProcessACKEvent(false);
		}
	}

	/**
	 * write a NAK triggered by a received sequence number that is larger than
	 * the largestReceivedSeqNumber + 1
	 * @param currentSequenceNumber - the currently received sequence number
	 * @throws IOException
	 */
	protected void SendNAK(long currentSequenceNumber){
		NegativeAcknowledgement nAckPacket= new NegativeAcknowledgement();
		nAckPacket.AddLossInfo(largestReceivedSeqNumber+1, currentSequenceNumber);
		nAckPacket.SetSession(session);
		nAckPacket.DestinationID=session.Destination.SocketID;
		//put all the sequence numbers between (but excluding) these two values into the
		//receiver loss list
		for(long i=largestReceivedSeqNumber+1;i<currentSequenceNumber;i++){
			ReceiverLossListEntry detectedLossSeqNumber= new ReceiverLossListEntry(i);
			receiverLossList.Insert(detectedLossSeqNumber);
		}
		endpoint.DoSend(nAckPacket);
		//logger.info("NAK for "+currentSequenceNumber);
		statistics.incNumberOfNAKSent();
	}

	protected void SendNAK(List<long>sequenceNumbers){
		if(sequenceNumbers.Count==0)return;
		NegativeAcknowledgement nAckPacket= new NegativeAcknowledgement();
		nAckPacket.AddLossInfo(sequenceNumbers);
		nAckPacket.SetSession(session);
            nAckPacket.DestinationID = session.Destination.SocketID;
		endpoint.DoSend(nAckPacket);
		statistics.incNumberOfNAKSent();
		
	}

	protected long SendLightAcknowledgment(long ackNumber){
		Acknowledgement acknowledgmentPkt=buildLightAcknowledgement(ackNumber);
		endpoint.DoSend(acknowledgmentPkt);
		statistics.incNumberOfACKSent();
            return acknowledgmentPkt.AckSequenceNumber;
	}

	protected long sendAcknowledgment(long ackNumber){
		Acknowledgement acknowledgmentPkt = buildLightAcknowledgement(ackNumber);
		//set the estimate link capacity
		estimateLinkCapacity=packetPairWindow.getEstimatedLinkCapacity();
		acknowledgmentPkt.EstimatedLinkCapacity=(estimateLinkCapacity);
		//set the packet arrival rate
		packetArrivalSpeed=packetHistoryWindow.getPacketArrivalSpeed();
		acknowledgmentPkt.PacketReceiveRate=(packetArrivalSpeed);

		endpoint.DoSend(acknowledgmentPkt);

		statistics.incNumberOfACKSent();
		statistics.setPacketArrivalRate(packetArrivalSpeed, estimateLinkCapacity);
            return acknowledgmentPkt.AckSequenceNumber;
	}

	//builds a "light" Acknowledgement
	private Acknowledgement buildLightAcknowledgement(long ackNumber){
		Acknowledgement acknowledgmentPkt = new Acknowledgement();
		//the packet sequence number to which all the packets have been received
		acknowledgmentPkt.AckNumber=ackNumber;
		//assign this ack a unique increasing ACK sequence number
		acknowledgmentPkt.AckSequenceNumber=++ackSequenceNumber;
		acknowledgmentPkt.RoundTripTime=roundTripTime;
		acknowledgmentPkt.RoundTripTimeVar=roundTripTimeVar;
		//set the buffer size
		acknowledgmentPkt.BufferSize=(bufferSize);

            acknowledgmentPkt.DestinationID = session.Destination.SocketID;
		acknowledgmentPkt.SetSession(session);

		return acknowledgmentPkt;
	}

	/**
	 * spec p. 13: <br/>
	  1) Locate the related ACK in the ACK History Window according to the 
         ACK sequence number in this ACK2.  <br/>
      2) Update the largest ACK number ever been acknowledged. <br/>
      3) Calculate new rtt according to the ACK2 arrival time and the ACK 
         departure time, and update the RTT value as: RTT = (RTT * 7 + 
         rtt) / 8.  <br/>
      4) Update RTTVar by: RTTVar = (RTTVar * 3 + abs(RTT - rtt)) / 4.  <br/>
      5) Update both ACK and NAK period to 4 * RTT + RTTVar + SYN.  <br/>
	 */
	protected void onAck2PacketReceived(Acknowledgment2 ack2){
		AckHistoryEntry entry=ackHistoryWindow.getEntry(ack2.AckSequenceNumber);
		if(entry!=null){
			long ackNumber=entry.getAckNumber();
			largestAcknowledgedAckNumber=Math.Max(ackNumber, largestAcknowledgedAckNumber);
			long rtt=entry.getAge();
			if(roundTripTime>0)roundTripTime = (roundTripTime*7 + rtt)/8;
			else roundTripTime = rtt;
			roundTripTimeVar = (roundTripTimeVar* 3 + Math.Abs(roundTripTimeVar- rtt)) / 4;
			ackTimerInterval=4*roundTripTime+roundTripTimeVar+Util.GetSYNTime();
			nakTimerInterval=ackTimerInterval;
			statistics.setRTT(roundTripTime, roundTripTimeVar);
		}
	}

	protected void SendKeepAlive(){
		KeepAlive ka=new KeepAlive();
            ka.DestinationID = session.Destination.SocketID;
		ka.SetSession(session);
		endpoint.DoSend(ka);
	}

	protected void SendShutdown(){
		Shutdown s=new Shutdown();
            s.DestinationID = session.Destination.SocketID;
		s.SetSession(session);
		endpoint.DoSend(s);
	}

	

	internal void ResetEXPTimer(){
		nextEXP=Util.getCurrentTime()+expTimerInterval;
		expCount=0;
	}

        internal void ResetEXPCount(){
		expCount=0;
	}
	
	public void setAckInterval(long ackInterval){
		this.ackInterval=ackInterval;
	}

        internal void OnShutdown(){
		Stop();
	}

	public void Stop(){
		stopped=true;
            session.Socket.Close();
		//stop our sender as well
		    session.Socket.GetSender().Stop();
	}

	
	public string toString(){
		StringBuilder sb=new StringBuilder();
		sb.Append("UDTReceiver ").Append(session).Append("\n");
		sb.Append("LossList: "+receiverLossList);
		return sb.ToString();
	}
}
}
