/*********************************************************************************
 * Copyright (c) 2010 Forschungszentrum Juelich GmbH 
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * (1) Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the disclaimer at the end. Redistributions in
 * binary form must reproduce the above copyright notice, this list of
 * conditions and the following disclaimer in the documentation and/or other
 * materials provided with the distribution.
 * 
 * (2) Neither the name of Forschungszentrum Juelich GmbH nor the names of its 
 * contributors may be used to endorse or promote products derived from this 
 * software without specific prior written permission.
 * 
 * DISCLAIMER
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 *********************************************************************************/





using netudt.util;
using System;
using System.IO;
/**
* The UDTInputStream receives data blocks from the {@link UDTSocket}
* as they become available, and places them into an ordered, 
* bounded queue (the flow window) for reading by the application
* 
* 
*/
namespace netudt
{
    public partial class UDTInputStream : Stream {

	//the socket owning this inputstream
	private  UDTSocket socket;

	private  ReceiveBuffer receiveBuffer;

        //set to 'false' by the receiver when it gets a shutdown signal from the peer
        //see the noMoreData() method
        private volatile bool expectMoreData = false;

	private volatile bool closed=false;

	private volatile bool blocking=true;
	
	private volatile bool hasData=false;//cd

        private byte[] single = new byte[1];
        private AppData currentChunk = null;
        //offset into currentChunk
        int offset = 0;
        long id = -1;

        /**
         * create a new {@link UDTInputStream} connected to the given socket
         * @param socket - the {@link UDTSocket}
         * @throws IOException
         */
        public UDTInputStream(UDTSocket socket){
		this.socket=socket;
		int capacity=socket!=null? 2 * socket.GetSession().FlowWindowSize : 128 ;
		long initialSequenceNum=socket!=null?socket.GetSession().InitialSequenceNumber:1;
		receiveBuffer=new ReceiveBuffer(capacity,initialSequenceNum);
	}

	

	
	public int Read(){
		int b=0;
		while(b==0)
			b=Read(single);

		if(b>0){
			return single[0];
		}
		else {
			return b;
		}
	}


	
	public int Read(byte[]target){
		try{
			int read=0;
			UpdateCurrentChunk(false);
			while(currentChunk!=null){
				byte[]data=currentChunk.data;
				int length=Math.Min(target.Length-read,data.Length-offset);
				Array.Copy(data, offset, target, read, length);
				read+=length;
				offset+=length;
				//check if chunk has been fully read
				if(offset>=data.Length){
					currentChunk=null;
					offset=0;
				}

				//if no more space left in target, exit now
				if(read==target.Length){
					return read;
				}

				UpdateCurrentChunk(blocking && read==0);
			}

			if(read>0)return read;
			if(closed)return -1;
			if(expectMoreData || !receiveBuffer.IsEmpty)return 0;
			//no more data
			return -1;

		}catch(Exception ex){
			
			throw ex;
		}
	}

	/**
	 * Reads the next valid chunk of application data from the queue<br/>
	 * 
	 * In blocking mode,this method will block until data is available or the socket is closed, 
	 * otherwise it will wait for at most 10 milliseconds.
	 * 
	 * @throws InterruptedException
	 */
	private void UpdateCurrentChunk(bool block){
		if(currentChunk!=null)return;

		while(true){
			try{
				if(block){
					currentChunk=receiveBuffer.Poll(1);
					while (!closed && currentChunk==null){
						currentChunk=receiveBuffer.Poll(1000);
					}
				}
				else currentChunk=receiveBuffer.Poll(10);
				
			}catch(Exception ex){
			
				throw ex;
			}
			return;
		}
	}

	/**
	 * new application data
	 * @param data
	 * 
	 */
	internal bool HaveNewData(long sequenceNumber,byte[]data){
		hasData=true;//cd
		return receiveBuffer.Offer(new AppData(sequenceNumber,data));
	}

	
	public override void Close(){
		if(closed)return;
		closed=true;
		noMoreData();
	}

	public UDTSocket GetSocket(){
		return socket;
	}

	/**
	 * sets the blocking mode
	 * @param block
	 */
	public void SetBlocking(bool block){
		this.blocking=block;
	}

	public int GetReceiveBufferSize(){
            return receiveBuffer.GetSize();
	}
	
	/**
	 * notify the input stream that there is no more data
	 * @throws IOException
	 */
	internal void noMoreData(){
            expectMoreData = false;
	}
	
	/**
	 * 判断有没有数据进来
	 * cd
	 * @return
	 */
	 public bool IsHasData
     {
            get { return hasData; }
     }

        public override bool CanRead => throw new NotImplementedException();

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => throw new NotImplementedException();

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /**
* 设置是读取为主还是写入为主
* 如果是写入为主，当读取速度慢时，数据覆盖丢失
* 默认读取为主，还没有读取则不允许覆盖，丢掉数据，等待重复
* islagerRead=true才有意义
* 
*/
        public void ResetBufMaster(bool isRead)
        {
            receiveBuffer.ResetBufMaster(isRead);

        }
	 
	 /**
	  * 设置大数据读取
	  * 默认 false
	  * @param islarge
	  */
	 public void SetLargeRead(bool islarge)
	 {
		 receiveBuffer.SetLargeRead(islarge);
	 }

        public override void Flush()
        {
            throw new NotImplementedException();
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

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
