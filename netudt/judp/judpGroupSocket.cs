


using netudt;
using netudt.Log;
using System;
using System.Collections.Generic;
/**
* 
*     
* 项目名称：judt    
* 类名称：judpGroupSocket    
* 类描述：    按照目的地址分组socket
* 创建人：jinyu    
* 创建时间：2018年8月25日 上午10:53:59    
* 修改人：jinyu    
* 修改时间：2018年8月25日 上午10:53:59    
* 修改备注：    
* @version   1.0   
*
*/
namespace netudt.judp
{
    public class judpGroupSocket
    {
        private List<UDTSocket> list = new List<UDTSocket>();
        public judpGroupSocket()
        {

        }

        /**
         * 添加socket
         * @param socket
         */
        public void AddSocket(UDTSocket socket)
        {
            list.Add(socket);
        }

        /**
         * 获取有数据socket
         * 并且移除其它无用socket
         * @return
         */
        public UDTSocket GetSocket()
        {

            int index = -1;
            int size = list.Count;
            for (int i = 0; i < size; i++)
            {
                try
                {
                    if (index == -1)
                    {
                        if (list[i].GetInputStream().IsHasData)
                        {
                            //已经找到；其余的移除关
                            index = i;
                            i = -1;//重新遍历
                        }
                        //
                        UDTSession session = list[i].GetSession();
                        if (session.State == UDTSession.shutdown)
                        {
                            //说明已经关闭
                            list[i].Close();
                            long id = list[i].GetSession().SocketID;
                            list[i].GetEndpoint().RemoveSession(id);
                            list[i].GetReceiver().Stop();
                            list[i].GetSender().Stop();
                            list[i].GetSender().Pause();
                            list.RemoveAt(i);
                            i--;
                        }
                    }
                    else
                    {
                        //
                        if (i == index)
                        {
                            continue;
                        }
                        else
                        {
                            list[i].Close();
                            long id = list[i].GetSession().SocketID;
                            list[i].GetEndpoint().RemoveSession(id);
                            list[i].GetReceiver().Stop();
                            list[i].GetSender().Stop();
                            list[i].GetSender().Pause();
                            FlashLogger.Info("移除无用socket:" + id);
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            if (index != -1)
            {
                return list[index];
            }
            return null;

        }

        /**
         * 清除所有socket
         */
        public void Clear()
        {
            list.Clear();
        }
    }
}
