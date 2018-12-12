


using System;
using System.Collections.Generic;
using System.Threading;
/**
* 
* The receive buffer stores data chunks to be read by the application
*
* @author schuller
*/
namespace netudt.util
{ 
public class ReceiveBuffer {

	private  AppData[] buffer;

	//the head of the buffer: contains the next chunk to be read by the application, 
	//i.e. the one with the lowest sequence number
	private volatile int readPosition=0;

	//the lowest sequence number stored in this buffer
	private  long initialSequenceNumber;

	//the highest sequence number already read by the application
	private long highestReadSequenceNumber;

        //number of chunks
  private int numValidChunks = 0;

        //lock and condition for poll() with timeout
        //private  Condition notEmpty;
        //private   lock;
        private object lock_obj = new object();
	//the size of the buffer
	private  int size;
	
	//cd 
	private  HashSet<long> hashSeqNo;
	private  Dictionary<int,long> hashOffset;
	private readonly int leftNum=5;
	private bool isRWMaster=true;
	private bool islagerRead=false;//大数据读取；也要外部快速读取
        private const int TicksMs = 10000;

	public ReceiveBuffer(int size, long initialSequenceNumber){
		this.size=size;
		this.buffer=new AppData[size];
		this.initialSequenceNumber=initialSequenceNumber;
        highestReadSequenceNumber =SequenceNumber.Decrement(initialSequenceNumber);
		this.hashSeqNo=new HashSet<long>();
        this.hashOffset = new Dictionary<int, long>(size);
	}

	public bool Offer(AppData data){
		if(numValidChunks==size) {
			return false;
		}
	   
		try{
			long seq=data.getSequenceNumber();
			//if already have this chunk, discard it
			if(SequenceNumber.Compare(seq, initialSequenceNumber)<0)return true;
			//else compute insert position
			int offset=(int)SequenceNumber.SeqOffset(initialSequenceNumber, seq);
			int insert=offset% size;
                if (islagerRead)
                {
                    // cd  为大数据读取准备的
                    if (isRWMaster && buffer[insert] == null)
                    {
                        //如果是读取为主，则不能覆盖， cd
                        buffer[insert] = data;
                    }
                    else if (!isRWMaster)
                    {
                        //可以覆盖，写为主，直接存储 cd
                        buffer[insert] = data;
                    }
                    else
                    {
                        //不能覆盖，还没有读取，则返回丢失 cd
                        //另外一种情况，重复来了以前的数据，hashSeqNo都可能删除了
                        // 比较下与当前的数据
                        long id = buffer[insert].getSequenceNumber();
                        if (id > seq)
                        {
                            //已经存储了新的数据，则说明当前是旧数据，直接丢失
                            //不再需要该数据了
                            return true;
                        }
                        return false;
                    }

                    if (hashSeqNo.Add(seq))
                    {
                        //没有接受过才增长 cd
                        // numValidChunks.incrementAndGet();//没有解决重复接受的问题
                        Interlocked.Increment(ref numValidChunks);
                        hashOffset[insert] = seq;
                    }
                }
                else
                {
                    //原代码，小数据接收完全可以
                    buffer[insert] = data;
                    // numValidChunks.incrementAndGet();
                    Interlocked.Increment(ref numValidChunks);
                }
			//notEmpty.signal();
			return true;
		}
            finally
            {

            }
	}

	/**
	 * return a data chunk, guaranteed to be in-order, waiting up to the
	 * specified wait time if necessary for a chunk to become available.
	 *
	 * @param timeout how long to wait before giving up, in units of
	 *        <tt>unit</tt>
	 * @param unit a <tt>TimeUnit</tt> determining how to interpret the
	 *        <tt>timeout</tt> parameter
	 * @return data chunk, or <tt>null</tt> if the
	 *         specified waiting time elapses before an element is available
	 * @throws InterruptedException if interrupted while waiting
	 */
	public AppData Poll(int timeout) {
            long start = DateTime.Now.Ticks;
            long timeSpan = timeout * TicksMs;
            try
            {
                for (; ; )
                {
                    if (numValidChunks != 0)
                    {
                        return Poll();
                    }
                   if(DateTime.Now.Ticks-start>timeSpan)
                    {
                        break;
                    }
                  
                }
            }
            finally
            {

            }
            return null;
	}


        /**
         * return a data chunk, guaranteed to be in-order. 
         */
        public AppData Poll()
        {
            if (numValidChunks == 0)
            {
                return null;
            }
            AppData r = buffer[readPosition];
            if (r != null)
            {
                long thisSeq = r.getSequenceNumber();
                if (1 == SequenceNumber.SeqOffset(highestReadSequenceNumber, thisSeq))
                {
                    Increment();
                    highestReadSequenceNumber = thisSeq;
                }
                else
                {

                    if (this.islagerRead)
                    {
                        //cd 
                        //如果不为空，则判断是否是覆盖了 cd
                        // 不正常覆盖的值
                        if (highestReadSequenceNumber + 1 < thisSeq)
                        {
                            //如果是写入为主，则丢失数据
                            if (!this.isRWMaster)
                            {
                                Increment();//这样就丢掉了，往前读取
                            }
                        }
                        else if (highestReadSequenceNumber > thisSeq + 1)
                        {
                            //说明重发的数据占用了位置，新的值还没有进去；
                            //说明已经读取了数据
                            if (this.isRWMaster)
                            {
                                //写入为主时，数据就直接覆盖了，不需要重空
                                buffer[readPosition] = null;//丢掉数据
                            }

                        }
                    }
                    return null;
                }
            }
            else
            {
                Console.WriteLine("empty HEAD at pos=" + readPosition);
                Thread.Sleep(1000);
                Thread.Yield();

            }
            if (this.islagerRead)
            {
                // cd
                if (readPosition > this.size - leftNum)
                {
                    ClearHash(readPosition);
                }
            }
            return r;
        }
	
	public int GetSize(){
		return size;
	}

	void Increment(){
		buffer[readPosition]=null;
		readPosition++;
		
		if(readPosition==size)
			{readPosition=0;
			if(this.islagerRead)
			 {
				//cd 
			   ClearDeHash(this.size-leftNum);
			 }
			}
		
            Interlocked.Decrement(ref numValidChunks);
        }

	public bool IsEmpty{
            get { return numValidChunks == 0; }
	}

	/**
	 *  清除重复检验 
	 *  cd 
	 * @param position
	 */
    private void ClearHash(int position)
    {
    	for(int i=0;i<position;i++)
    	{
                long seqNo = 0;

                if (hashOffset.TryGetValue(i, out seqNo))
                {
                    hashSeqNo.Remove(seqNo);
                    hashOffset.Remove(i);
                }
    		
    	}
    }
    /**
	 *  清除重复检验
	 *  cd 
	 * @param position
	 */
    private void ClearDeHash(int position)
    {
            for (int i = this.size - 1; i > position - 1; i--)
            {
                long seqNo = 0;
                if (hashOffset.TryGetValue(i, out seqNo))
                {
                    hashSeqNo.Remove(seqNo);
                    hashOffset.Remove(i);
                }
            }
    }
    
    /**
     * 设置是读取为主还是写入为主
     * 如果是写入为主，当读取速度慢时，数据覆盖丢失
     * 默认读取为主，还没有读取则不允许覆盖，丢掉数据，等待重复
     * islagerRead=true才有意义
     * @param isRead
     */
    public void  ResetBufMaster(bool isRead)
    {
    	this.isRWMaster=isRead;
    }
    
    /**
     * 设置大数据读取
     * 默认 false
     * @param islarge
     */
    public void SetLargeRead(bool islarge)
    {
    	this.islagerRead=islarge;
    }
}
}
