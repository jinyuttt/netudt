
using System;
using System.IO;

namespace netudt
{ 
public class UDTOutputStream :Stream{

	private  UDTSocket socket;
	
	private volatile bool closed;

        public override bool CanRead { get; }

        public override bool CanSeek { get; }

        public override bool CanWrite { get; }

        public override long Length { get; }

        public override long Position { get; set; }

        public UDTOutputStream(UDTSocket socket){
		this.socket=socket;	
	}
	
	
	public void Write(int args){
		CheckClosed();
		socket.DoWrite(new byte[]{(byte)args});
	}

	
	public override void Write(byte[] b, int off, int len) {
		CheckClosed();
		socket.DoWrite(b, off, len);
	}

	
	public void Write(byte[] b) {
		Write(b,0,b.Length);
	}
	
	
	public override void Flush(){
		try{
			CheckClosed();
			socket.Flush();
		}catch(Exception ie){
			
			throw ie;
		}
	}
	
	/**
	 * This method signals the UDT sender that it can pause the 
	 * sending thread. The UDT sender will resume when the next 
	 * write() call is executed.<br/>
	 * For example, one can use this method on the receiving end 
	 * of a file transfer, to save some CPU time which would otherwise
	 * be consumed by the sender thread.
	 */
	public void PauseOutput(){
		socket.GetSender().Pause();
	}
	
	
	/**
	 * close this output stream
	 */
	
	public override void Close(){
		if(closed)return;
		closed=true;
            base.Close();
	}
	
	private void CheckClosed(){
		if(closed)throw new IOException("Stream has been closed");
	}

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}
