


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
	private bool islagerRead=false;//�����ݶ�ȡ��ҲҪ�ⲿ���ٶ�ȡ
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
                    // cd  Ϊ�����ݶ�ȡ׼����
                    if (isRWMaster && buffer[insert] == null)
                    {
                        //����Ƕ�ȡΪ�������ܸ��ǣ� cd
                        buffer[insert] = data;
                    }
                    else if (!isRWMaster)
                    {
                        //���Ը��ǣ�дΪ����ֱ�Ӵ洢 cd
                        buffer[insert] = data;
                    }
                    else
                    {
                        //���ܸ��ǣ���û�ж�ȡ���򷵻ض�ʧ cd
                        //����һ��������ظ�������ǰ�����ݣ�hashSeqNo������ɾ����
                        // �Ƚ����뵱ǰ������
                        long id = buffer[insert].getSequenceNumber();
                        if (id > seq)
                        {
                            //�Ѿ��洢���µ����ݣ���˵����ǰ�Ǿ����ݣ�ֱ�Ӷ�ʧ
                            //������Ҫ��������
                            return true;
                        }
                        return false;
                    }

                    if (hashSeqNo.Add(seq))
                    {
                        //û�н��ܹ������� cd
                        // numValidChunks.incrementAndGet();//û�н���ظ����ܵ�����
                        Interlocked.Increment(ref numValidChunks);
                        hashOffset[insert] = seq;
                    }
                }
                else
                {
                    //ԭ���룬С���ݽ�����ȫ����
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
                        //�����Ϊ�գ����ж��Ƿ��Ǹ����� cd
                        // ���������ǵ�ֵ
                        if (highestReadSequenceNumber + 1 < thisSeq)
                        {
                            //�����д��Ϊ������ʧ����
                            if (!this.isRWMaster)
                            {
                                Increment();//�����Ͷ����ˣ���ǰ��ȡ
                            }
                        }
                        else if (highestReadSequenceNumber > thisSeq + 1)
                        {
                            //˵���ط�������ռ����λ�ã��µ�ֵ��û�н�ȥ��
                            //˵���Ѿ���ȡ������
                            if (this.isRWMaster)
                            {
                                //д��Ϊ��ʱ�����ݾ�ֱ�Ӹ����ˣ�����Ҫ�ؿ�
                                buffer[readPosition] = null;//��������
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
	 *  ����ظ����� 
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
	 *  ����ظ�����
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
     * �����Ƕ�ȡΪ������д��Ϊ��
     * �����д��Ϊ��������ȡ�ٶ���ʱ�����ݸ��Ƕ�ʧ
     * Ĭ�϶�ȡΪ������û�ж�ȡ�������ǣ��������ݣ��ȴ��ظ�
     * islagerRead=true��������
     * @param isRead
     */
    public void  ResetBufMaster(bool isRead)
    {
    	this.isRWMaster=isRead;
    }
    
    /**
     * ���ô����ݶ�ȡ
     * Ĭ�� false
     * @param islarge
     */
    public void SetLargeRead(bool islarge)
    {
    	this.islagerRead=islarge;
    }
}
}
