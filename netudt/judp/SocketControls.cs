using netudt;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
/**
* @author jinyu
* 接收端判断
*/
namespace netudt.judp
{ 
public class SocketControls {
 private static SocketControls instance;

private  ConcurrentDictionary<long,judpGroupSocket> hash=new ConcurrentDictionary<long,judpGroupSocket>();
        private object lock_obj = new object();
             
/**
 * 保持有数据的通信对象
 */
private BlockingCollection<UDTSocket> hasSocket=new BlockingCollection<UDTSocket>(1000);
 private SocketControls (){
	  StartThread();
 }


        /**
         * 
        * @Title: startThread
        * @Description: 启动线程监测数据源，移除无用连接
        * @param     参数
        * @return void    返回类型
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
                        //移除已经检查成功的socket
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
                        //每完成一次全部检查;
                        Thread.Sleep(1000);
                    }

                }

            });
            processSocket.IsBackground = true;
            processSocket.Name = ("processSocket");
            processSocket.Start();
        }

  /**
   * 获取单例
   * @return
   */
public static  SocketControls GetInstance() {  
	   
 if (instance == null) {  
	
    instance = new SocketControls();  
}  
 return instance;  
}

        /**
         * 保存UDTSocket
         * @param socket
         */
        public void AddSocket(UDTSocket socket)
        {
            long id = socket.GetSession().Destination.SocketID;//同一个目的
            judpGroupSocket group = null;
            if (!hash.TryGetValue(id, out group))
            {
                group = new judpGroupSocket();
                hash[id] = group;
                group.AddSocket(socket);
            }
        }

        /**
         * 返回有数据的socket
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
