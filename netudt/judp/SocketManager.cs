

/**
 * 
 *     
 * ��Ŀ���ƣ�judt    
 * �����ƣ�SocketManager    
 * ��������   ������ͨ�Ŷ��� 
 * �����ˣ�jinyu    
 * ����ʱ�䣺2018��8��25�� ����2:59:22    
 * �޸��ˣ�jinyu    
 * �޸�ʱ�䣺2018��8��25�� ����2:59:22    
 * �޸ı�ע��    �����Ѿ�����ʹ�ã�ֻ����Ϊ���ӱ���
 * @version     
 *
 */
public class SocketManager {
//	private static SocketManager instance;  
//	LinkedBlockingQueue<CacheSocket>  objClient=new LinkedBlockingQueue<CacheSocket>();
//	
//	LinkedBlockingQueue<CacheSocket> objudtsocket=new LinkedBlockingQueue<CacheSocket>();
//	Map<Object, Object> mapSocket = new HashMap<>();
//	Map<Object, Object> mapClient= new HashMap<>();
//	private ReferenceQueue<? super judpSocket> referenceSocketQueue=new ReferenceQueue<>();
//	private ReferenceQueue<? super judpClient> referenceJClientQueue=new ReferenceQueue<>();
//	ScheduledExecutorService pool = Executors.newSingleThreadScheduledExecutor(); 
//	private long waitTime=30*1000;
//	private static final Logger logger=Logger.getLogger(SocketManager.class.getName());
//	  private SocketManager (){
//		  startThread();
//		   startGC();
//	  }
//	  
//	   public static synchronized SocketManager getInstance() {  
//		   
//	  if (instance == null) {  
//		
//	     instance = new SocketManager();  
//	  
//	   
//   }  
//	  return instance;  
//   }
//	   
//	   /*
//	    * ��ʱ��������
//	    * 
//	    */
//	   private void startGC()
//	   {
//	       pool.scheduleWithFixedDelay(new Runnable() {
//
//            @Override
//            public void run() {
//                System.gc();
//                
//            }
//	           
//	       },2*waitTime,waitTime,TimeUnit.MILLISECONDS);
//	   }
//	   
//	   /**
//	    * �����߳��Ѽ��ر�����
//	    * 
//	    */
//private void startThread()
//{
//	Thread judpclientGC=new Thread(new Runnable() {
//
//		@Override
//		public void run() {
//			startClientGC();
//			//�ͻ�����ʧ������ײ���ʧ
//			//������û�е��ùر�
//		}
//		
//	});
//	judpclientGC.setDaemon(true);
//	judpclientGC.setName("judpclientGC");
//	
//	Thread udtclientThead=new Thread(new Runnable() {
//
//		@Override
//		public void run() {
//			startClient();
//			//ֱ�ӹر�
//		}
//		
//	});
//	udtclientThead.setDaemon(true);
//	udtclientThead.setName("udtclientThead");
//	
//	Thread  judpsocketGC=new Thread(new Runnable() {
//
//		@Override
//		public void run() {
//			startsocketGC();
//		//judpsocket��ʧ
//		//����ײ���ʧudtThread
//		}
//		
//	});
//	
//	judpsocketGC.setDaemon(true);
//	judpsocketGC.setName("judpsocketGC");
//	
//
//	Thread udtsocketThread=new Thread(new Runnable() {
//
//		@Override
//		public void run() {
//			startUDTSocket();
//			
//		}
//		
//	});
//	udtsocketThread.setDaemon(true);
//	udtsocketThread.setName("udtsocketThread");
//	
//	//
//	judpclientGC.start();
//	udtclientThead.start();
//	judpsocketGC.start();
//	udtsocketThread.start();
//}
//	   /*
//	    * �����ʧ
//	    * 
//	    */
//	private void startsocketGC()
//	{
//		 WeakReference<?> k = null;
//		while(true) {
//			try {
//				k = (WeakReference<?>) referenceSocketQueue.remove();
//				if(k==null)
//				{
//					TimeUnit.SECONDS.sleep(5);
//					continue;
//				}
//			} catch (InterruptedException e1) {
//			
//				e1.printStackTrace();
//				continue;
//			}
//			UDTSocket socket=(UDTSocket) mapSocket.remove(k);
//			if(socket!=null)
//			{
//				if(!socket.isClose())
//				{
//					//��û���ر�
//					//��û�е��ù��������رն���
//	  		        //��û�е��ã����ܴ���2�������1�����˹رշ����������˹رն��У�2û�н���رն��У�
//	  	            //��ν���Ĺرյ�û��Ӱ��
//                    add(socket);
//				}
//			}
//
//	        }  
//	}
//	
//	
//	
//	/*
//	 * �����ʧ����
//	 */
//	private void startClientGC()
//	{
//		
//		 WeakReference<?> k = null;
//			while(true) {
//				
//				try {
//					k = (WeakReference<?>) referenceJClientQueue.remove();
//					if(k==null)
//					{
//						TimeUnit.SECONDS.sleep(5);
//						continue;
//					}
//				} catch (InterruptedException e) {
//					
//					e.printStackTrace();
//					continue;
//				}
//			
//				  UDTClient client=  (UDTClient) mapClient.remove(k);
//				  add(client);
//				  //�����ʧ���ײ����ر�
//		        }  
//	}
//	
//	/*
//	 * �����ù��رյ�
//	 */
//	private  void startClient()
//	{
//		while(true)
//		{
//			try
//			{
//			 CacheSocket cacheData=objClient.take();
//			long left=System.currentTimeMillis()-cacheData.start;
//			if(left<waitTime)
//			{
//				TimeUnit.MILLISECONDS.sleep(waitTime-left);
//			}
//			//�����ùرջ���ʱ�䵽
//			  UDTClient client=(UDTClient) cacheData.obj;
//			  if(!client.isClose())
//			  client.shutdown();
//			//  client.getEndpoint().removeSession(client.getSocketID()); �Ѿ���ӵ�shutdown������
//			  logger.info("client�˳�");
//			}
//			catch(Exception ex)
//			{
//				ex.printStackTrace();
//			}
//		}
//	}
//	
//	/*
//	 * ��ʧ����رն��е�ֱ�ӹر�
//	 */
//	private   void startUDTSocket()
//	{
//	
//		while(true)
//		{
//			try
//			{
//			CacheSocket cacheData=objudtsocket.take();
//			long left=System.currentTimeMillis()-cacheData.start;
//			if(left<waitTime)
//			{
//				//�Ƚ��ȳ�
//				TimeUnit.MILLISECONDS.sleep(waitTime-left);
//				
//			}
//			//
//			UDTSocket udtsocket=(UDTSocket) cacheData.obj;
//			if(udtsocket!=null)
//			{
//				try {
//					udtsocket.close();
//					udtsocket.getReceiver().stop();
//					udtsocket.getSender().stop();
//				} catch (IOException e) {
//					e.printStackTrace();
//				}
//				logger.info("����ʱ�䵽socket����رգ�socket�˳���"+udtsocket.getSession().getSocketID());
//				 udtsocket.getEndpoint().removeSession(udtsocket.getSession().getSocketID());
//				 UDTSession session=udtsocket.getSession();
//				 if(session!=null)
//				   session.setActive(false);
//				    //���ڷ���ack��Ϣ
//				 session=null;
//			
//			}
//			  udtsocket=null;
//			}
//			catch(Exception ex)
//			{
//				ex.printStackTrace();
//			}
//		}
//	}
////	 /*
////	  * �ر�
////	  */
////	public void add(judpSocket socket)
////	{
////		try {
////			CacheSocket cache=new CacheSocket();
////		    WeakReference<judpSocket> weakReference = new WeakReference<judpSocket>(socket, referenceSocketQueue);
////			cache.obj=weakReference;
////			objsocket.put(cache);
////		    mapSocket.put(weakReference,socket.socketID);
////		} catch (InterruptedException e) {
////			// TODO Auto-generated catch block
////			e.printStackTrace();
////		}
////	}
//	public void add(UDTSocket usocket)
//	{
//		try {
//			CacheSocket cacheData=new CacheSocket();
//			cacheData.obj=usocket;
//			objudtsocket.put(cacheData);
//		} catch (InterruptedException e) {
//			e.printStackTrace();
//		}
//	}
//	/*
//	 * �ر�
//	 */
//	public void add(UDTClient rClient)
//	{
//		try {
//			CacheSocket cacheData=new CacheSocket();
//			cacheData.obj=rClient;
//			objClient.put(cacheData);
//		} catch (InterruptedException e) {
//			e.printStackTrace();
//		}
//	}
//	
//	/*
//	 * ����judpClientʱ����
//	 * �������δ�ر�client��û��������
//	 * 
//	 */
//	public void addGC(judpClient client,UDTClient rClient)
//	{
//		    WeakReference<judpClient> weakReference = new WeakReference<judpClient>(client,referenceJClientQueue);
//		    mapClient.put(weakReference,rClient);//ֱ����
//	}
//	
//	/*
//	 * ����judpSocket���룬
//	 * �������δ�ر�judpSocket��û��������
//	 */
//	public void addGC(judpSocket socket,UDTSocket usocket)
//	{
//		    WeakReference<judpSocket> weakReference = new WeakReference<judpSocket>(socket, referenceSocketQueue);
//		   // mapSocket.put(weakReference,usocket.getSession().getSocketID());
//		    //all_UDTSocket.put(usocket.getSession().getSocketID(), usocket);//ͨ��ID����UDTSocket
//		    //��ʧʱ�Ѿ�û��UDTSocket���������ٽ���رն��У��Ѿ��ر���
//		    mapSocket.put(weakReference, usocket);
//		  
//		    
//	}
}
