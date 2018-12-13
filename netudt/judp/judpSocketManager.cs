
using netudt.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace netudt.judp
{
    /**
     * @author jinyu
     * 管理接受端的judpSocket
     */
    public class judpSocketManager
    {
      
     
        private  long num = 0;
        /**
         * 测试使用
         */
        private ConcurrentDictionary<WeakReference<judpSocket>, long> dicMap= new ConcurrentDictionary<WeakReference<judpSocket>, long>();

        private UDPEndPoint endPoint = null;
        private static judpSocketManager instance = null;
        private judpSocketManager(UDPEndPoint point)
        {
            this.endPoint = point;
            StartGC();
        }
        
	

    /**
     * 单例
     * @param point
     * @return
     */
    public static  judpSocketManager GetInstance(UDPEndPoint point)
    {

        if (instance == null)
        {

            instance = new judpSocketManager(point);

        }
        return instance;
    }
        private void StartGC()
        {
            Thread gc = new Thread(() =>
              {
                  while(true)
                  {
                     judpSocket socket=null;
                     foreach(var item in dicMap)
                      {
                          if(!item.Key.TryGetTarget(out socket))
                          {
                              UDTSession serversession = endPoint.RemoveSession(item.Value);
                              if (serversession != null)
                              {
                                  serversession.Socket.Close();
                                  serversession.Socket.GetReceiver().Stop();
                                  serversession.Socket.GetSender().Stop();
                                  FlashLogger.Info("移除socket:" + item.Value);
                              }
                          }
                      }
                  }
              });

            gc.IsBackground = true;
            gc.Name = "GCSocket";
          
            if (!gc.IsAlive)
            {
                gc.Start();
            }
        }
    /**
     * 添加judpSocket
     * @param socket
     */
    public void AddSocket(judpSocket socket)
    {
        WeakReference<judpSocket> tmp = new WeakReference<judpSocket>(socket);
        dicMap.TryAdd(tmp, socket.SocketID);
        if (num % 10 == 0)
        {
                GC.Collect(0);
        }
        num++;
    }
}
}
