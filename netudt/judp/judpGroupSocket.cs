


using netudt;
using netudt.Log;
using System;
using System.Collections.Generic;
/**
* 
*     
* ��Ŀ���ƣ�judt    
* �����ƣ�judpGroupSocket    
* ��������    ����Ŀ�ĵ�ַ����socket
* �����ˣ�jinyu    
* ����ʱ�䣺2018��8��25�� ����10:53:59    
* �޸��ˣ�jinyu    
* �޸�ʱ�䣺2018��8��25�� ����10:53:59    
* �޸ı�ע��    
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
         * ���socket
         * @param socket
         */
        public void AddSocket(UDTSocket socket)
        {
            list.Add(socket);
        }

        /**
         * ��ȡ������socket
         * �����Ƴ���������socket
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
                            //�Ѿ��ҵ���������Ƴ���
                            index = i;
                            i = -1;//���±���
                        }
                        //
                        UDTSession session = list[i].GetSession();
                        if (session.State == UDTSession.shutdown)
                        {
                            //˵���Ѿ��ر�
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
                            FlashLogger.Info("�Ƴ�����socket:" + id);
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
         * �������socket
         */
        public void Clear()
        {
            list.Clear();
        }
    }
}
