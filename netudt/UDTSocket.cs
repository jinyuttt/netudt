
using netudt.packets;
using System;
using System.IO;
using System.Threading;
/**
* UDTSocket is analogous to a normal java.net.Socket, it provides input and 
* output streams for the application
* 
* TODO is it possible to actually extend java.net.Socket ?
* 
* 
*/
namespace netudt
{ 
public class UDTSocket {
	//一个session对应一个udtsocket
	//endpoint
	private  UDPEndPoint endpoint;
	
	private volatile bool active;
	
	private volatile bool close=false;//关闭标识，cd
	
    //processing received data
	private UDTReceiver receiver;
	private UDTSender sender;
	
	private  UDTSession session;

	private UDTInputStream inputStream;
	private UDTOutputStream outputStream;

        private object lock_obj = new object();

	/**
     * @param host
     * @param port
     * @param endpoint
     * @throws SocketException,UnknownHostException
     */
	public UDTSocket(UDPEndPoint endpoint, UDTSession session){
		this.endpoint=endpoint;
		this.session=session;
		this.receiver=new UDTReceiver(session,endpoint);
		this.sender=new UDTSender(session,endpoint);
	}
	
	public UDTReceiver GetReceiver() {
		return receiver;
	}

	public void SetReceiver(UDTReceiver receiver) {
		this.receiver = receiver;
	}

	public UDTSender GetSender() {
		return sender;
	}

	public void SetSender(UDTSender sender) {
		this.sender = sender;
	}

	public void setActive(bool active) {
		this.active = active;
	}

	public bool IsActive() {
		return active;
	}

	public UDPEndPoint GetEndpoint() {
		return endpoint;
	}

	 public bool IsClose()
	 {
	     return close;
	 }
	/**
	 * get the input stream for reading from this socket
	 * @return
	 */
	public  UDTInputStream GetInputStream(){
            lock (lock_obj)
            {
                if (inputStream == null)
                {
                    inputStream = new UDTInputStream(this);
                }
                return inputStream;
            }
	}
    
	/**
	 * get the output stream for writing to this socket
	 * @return
	 */
	public  UDTOutputStream GetOutputStream(){
            lock (lock_obj)
            {
                if (outputStream == null)
                {
                    outputStream = new UDTOutputStream(this);
                }

                return outputStream;
            }
	}
	
	public  UDTSession GetSession(){
		return session;
	}



        /**
         * write single block of data without waiting for any acknowledgement
         * @param data
         */
        internal void DoWrite(byte[]data){
		DoWrite(data, 0, data.Length);
		
	}
	
	/**
	 * write the given data 
	 * @param data - the data array
	 * @param offset - the offset into the array
	 * @param length - the number of bytes to write
	 * @throws IOException
	 */
	internal void DoWrite(byte[]data, int offset, int length){
		try{
			DoWrite(data, offset, length, int.MaxValue);
		}catch(Exception ie){
		
			throw ie;
		}
	}
	
	/**
	 * write the given data, waiting at most for the specified time if the queue is full
	 * @param data
	 * @param offset
	 * @param length
	 * @param timeout
	 * @param units
	 * @throws IOException - if data cannot be sent
	 * @throws InterruptedException
	 */
	internal void DoWrite(byte[]data, int offset, int length, int timeout){
		int chunksize=session.DatagramSize-24;//need some bytes for the header

            //ByteBuffer bb=ByteBuffer.wrap(data,offset,length);
           // ArraySegment<byte> segment = new ArraySegment<byte>(data, offset, length);
           // segment.
            long seqNo=0;
            int index = offset;
            int remaining = length;
            while (remaining>0)
            {
                int len = Math.Min(remaining, chunksize);
                byte[] chunk = new byte[len];
                Array.Copy(data, index, chunk, 0, len);
                DataPacket packet = new DataPacket();
                seqNo = sender.GetNextSequenceNumber();
                packet.PacketSequenceNumber = seqNo;
                packet.SetSession(session);
                packet.DestinationID = session.Destination.SocketID;
                packet.Buffer = chunk;
                //put the packet into the send queue
                if (!sender.SendUdtPacket(packet, timeout))
                {
                    throw new IOException("Queue full");
                }
                remaining = remaining - len;
                index += len;
            }
		if(length>0)active=true;
		
	}
	/**
	 * will block until the outstanding packets have really been sent out
	 * and acknowledged
	 */
	internal void Flush(){
		if(!active)return;
		 long seqNo=sender.GetCurrentSequenceNumber();
		if(seqNo<0)throw new IllegalStateException();
		while(!sender.IsSentOut(seqNo)){
			Thread.Sleep(5);
            }
            if (seqNo > -1){
			//wait until data has been sent out and acknowledged
			while(active && !sender.HaveAcknowledgementFor(seqNo)){
				sender.WaitForAck(seqNo);
			}
		}
		//TODO need to check if we can pause the sender...
		//sender.pause();
	}
	
	//writes and wait for ack
	internal void DoWriteBlocking(byte[]data){
		DoWrite(data);
		Flush();
	}
	
	/**
	 * close the connection
	 * @throws IOException
	 */
	public void Close(){
		if(inputStream!=null)inputStream.Close();
		if(outputStream!=null)outputStream.Close();
		active=false;
		close=true;
	}

      
    }
}
