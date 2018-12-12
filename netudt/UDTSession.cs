
using netudt.packets;
using netudt.util;
using System;
using System.Text;
using System.Threading;

namespace netudt
{
    public abstract class UDTSession {

       

        protected int mode;
        protected volatile bool active;
        private volatile int state = start;
        protected volatile IUDTPacket lastPacket;

        //state constants	
        public const int start = 0;
        public const int handshaking = 1;
        public const int ready = 2;
        public const int keepalive = 3;
        public const int shutdown = 4;

        public const int invalid = 99;

        protected volatile UDTSocket socket;

        protected UDTStatistics statistics;

        protected int receiveBufferSize = 64 * 32768;

        protected ICongestionControl cc;

        //cache dgPacket (peer stays the same always)
        private UDPUserToken dgPacket;

        /**
         * flow window size, i.e. how many data packets are
         * in-flight at a single time
         */
        protected int flowWindowSize = 1024;

        /**
         * remote UDT entity (address and socket ID)
         */
        protected Destination destination;

        /**
         * local port
         */
        protected int localPort;


        public static int DEFAULT_DATAGRAM_SIZE = UDPEndPoint.DATAGRAM_SIZE;

        /**
         * key for a system property defining the CC class to be used
         * @see CongestionControl
         */
        public static string CC_CLASS = "udt.congestioncontrol.class";

        /**
         * Buffer size (i.e. datagram size)
         * This is negotiated during connection setup
         */
        protected int datagramSize = DEFAULT_DATAGRAM_SIZE;

        //null
        protected long initialSequenceNumber = -1;

        protected long mySocketID;

        private static long nextSocketID = 20 + new Random().Next(5000);

        private object lock_obj = new object();

        public UDTSocket Socket
        {
            get { return socket; }
            set { socket = value; }
        }

        public int State
        {
            get { return state; }
            set { state = value; }
        }
        public bool IsActive
        {
            get { return active; }
            set { active = value; }
        }
        public int DatagramSize
        {
            get { return datagramSize; }
            set { datagramSize = value; }
        }

        public int ReceiveBufferSize
        {
            get { return receiveBufferSize; }
            set { receiveBufferSize = value; }
        }


        public int FlowWindowSize
        {
            get { return flowWindowSize; }
            set { flowWindowSize = value; }
        }
        public long InitialSequenceNumber
        {
            get { return initialSequenceNumber; }
            set { initialSequenceNumber = value; }
        }
        public ICongestionControl CongestionControl
        {
            get { return cc; }
        }
        public bool IsReady
        {
            get { return state == ready; }
        }
        public bool IsShutdown
        {
            get { return state == shutdown || state == invalid; }
        }
        public Destination Destination
        {
            get { return destination; }
        }
        public UDTStatistics Statistics
        {
            get { return statistics; }
        }

        public long SocketID
        {
           get{ return mySocketID; }
        }

        public UDPUserToken Datagram
        {
            get{ return dgPacket; }
        }
        public UDTSession(string description, Destination destination) {
            statistics = new UDTStatistics(description);
            mySocketID =Interlocked.Increment(ref nextSocketID);
            this.destination = destination;
            this.dgPacket =new UDPUserToken(destination.EndPoint);
            //String clazzP = System.getProperty(CC_CLASS, UDTCongestionControl.class.getName());
		Object ccObject = null;
        ccObject=new UDTCongestionControl(this);
        //try{
        //	Class<?>clazz=Class.forName(clazzP);
        //	ccObject=clazz.getDeclaredConstructor(UDTSession.class).newInstance(this);
        //}catch(Exception e){
        //	logger.log(Level.WARNING,"Can't setup congestion control class <"+clazzP+">, using default.",e);
        //	ccObject=new UDTCongestionControl(this);
        //}
        cc=(ICongestionControl) ccObject;
        //logger.info("Using "+cc.getClass().getName());
    }


    public abstract void Received(IUDTPacket packet, Destination peer);
        
        public void SetMode(int mode)
        {
            this.mode = mode;
        }
       
       
       
        
        //public UDTSocket getSocket() {
        //    return socket;
        //}



        //public int getState() {
        //    return state;
        //}



        //public void setSocket(UDTSocket socket) {
        //    this.socket = socket;
        //}

        //public void setState(int state) {
        //    logger.info(toString() + " connection state CHANGED to <" + state + ">");
        //    this.state = state;
        //}




        //public bool isActive() {
        //    return active == true;
        //}

        //public void setActive(bool active) {
        //    this.active = active;
        //}





        //public int getDatagramSize() {
        //    return datagramSize;
        //}

        //public void setDatagramSize(int datagramSize) {
        //    this.datagramSize = datagramSize;
        //}

        //public int getReceiveBufferSize() {
        //    return receiveBufferSize;
        //}

        //public void setReceiveBufferSize(int bufferSize) {
        //    this.receiveBufferSize = bufferSize;
        //}



        //public int getFlowWindowSize() {
        //    return flowWindowSize;
        //}

        //public void setFlowWindowSize(int flowWindowSize) {
        //    this.flowWindowSize = flowWindowSize;
        //}





        //public long getInitialSequenceNumber() {
        //    lock (lock_obj)
        //    {
        //        if (initialSequenceNumber == null) {
        //            initialSequenceNumber = 1l;
        //        }
        //        return initialSequenceNumber;
        //    }
        //}

        //public void setInitialSequenceNumber(long initialSequenceNumber) {
        //    lock (lock_obj)
        //    {
        //        this.initialSequenceNumber = initialSequenceNumber;
        //    }
        // }




        public  string toString() {
        StringBuilder sb = new StringBuilder();
        //sb.Append(base.toString());
        sb.Append(" [");
        sb.Append("socketID=").Append(this.mySocketID);
        sb.Append(" ]");
        return sb.ToString();
    }
}
}
