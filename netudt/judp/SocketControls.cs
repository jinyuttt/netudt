using netudt;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
/**
* @author jinyu
* ���ն��ж�
*/
namespace netudt.judp
{ 
public class SocketControls {
 private static SocketControls instance;

private  ConcurrentDictionary<long,judpGroupSocket> hash=new ConcurrentDictionary<long,judpGroupSocket>();
        private object lock_obj = new object();
             
/**
 * ���������ݵ�ͨ�Ŷ���
 */
private BlockingCollection<UDTSocket> hasSocket=new BlockingCollection<UDTSocket>(1000);
 private SocketControls (){
	  StartThread();
 }


        /**
         * 
        * @Title: startThread
        * @Description: �����̼߳������Դ���Ƴ���������
        * @param     ����
        * @return void    ��������
         */
        private void StartThread() {
            Thread processSocket = new Thread(() => {

                List<long> list = new List<long>();
                while (true)
                {
                    foreach (KeyValuePair<long, judpGroupSocket> entry in hash)
                    {
                        UDTSocket socket = entry.Value.GetSocket();
                        if (socket != null)
                        {
                            hasSocket.Add(socket);
                            list.Add(entry.Key);
                        }
                    }

                    //
                    if (list.Count > 0)
                    {
                        //�Ƴ��Ѿ����ɹ���socket
                        for (int i = 0; i < list.Count; i++)
                        {
                            judpGroupSocket group = null;
                            if (hash.TryRemove(list[i], out group))
                            {
                                group.Clear();
                            }
                        }
                        list.Clear();
                    }
                    else
                    {
                        //ÿ���һ��ȫ�����;
                        Thread.Sleep(1000);
                    }

                }

            });
            processSocket.IsBackground = true;
            processSocket.Name = ("processSocket");
            processSocket.Start();
        }

  /**
   * ��ȡ����
   * @return
   */
public static  SocketControls GetInstance() {  
	   
 if (instance == null) {  
	
    instance = new SocketControls();  
}  
 return instance;  
}

        /**
         * ����UDTSocket
         * @param socket
         */
        public void AddSocket(UDTSocket socket)
        {
            long id = socket.GetSession().Destination.SocketID;//ͬһ��Ŀ��
            judpGroupSocket group = null;
            if (!hash.TryGetValue(id, out group))
            {
                group = new judpGroupSocket();
                hash[id] = group;
                group.AddSocket(socket);
            }
        }

        /**
         * ���������ݵ�socket
         * @return
         */
        public UDTSocket GetSocket()
        {
            try
            {
                return hasSocket.Take();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }
}
}
