



using netudt;
using netudt.Log;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
/**
* @author jinyu
* ����˽��շ�װ
* �����
*/
namespace netudt.judp
{
    public class judpServer {
        private UDTServerSocket server = null;
        private bool isStart = true;
        private  bool isSucess = true;
        private bool isRWMaster = true;//��Ĭ��ֵһ��
        private bool islagerRead = false;
  
        /**
         * �رշ����
         */
        
        public void Close()
        {
            isStart = false;
            server.getEndpoint().Stop();
        }

        /**
         * 
         * @param port �˿�
         */
        public judpServer(int port)
        {

            try {
                server = new UDTServerSocket(port);
            } catch (SocketException e) {
               // logger.log(Level.WARNING, "��ʧ�ܣ�" + e.getMessage());
                FlashLogger.Warn("��ʧ�ܣ�" + e.Message);
                isSucess = false;
            } catch (Exception e) {
                isSucess = false;
                Console.WriteLine(e);
            }
        }

        /**
         * 
         * @param localIP ����IP
         * @param port  �˿�
         */
        public judpServer(string localIP, int port)
        {
            try {
               
                server = new UDTServerSocket(port,localIP);

            } catch (SocketException e) {
                FlashLogger.Warn("��ʧ�ܣ�" + e.Message);
                isSucess = false;
            } catch (Exception e) {
                isSucess = false;
                Console.WriteLine(e);
            }
        }



        /**
         * ��������
         */
        public bool Start()
        {
            if (!isStart || !isSucess)
            {

                FlashLogger.Warn("�Ѿ��رյļ���������˿ڲ���ʹ��");
                return false;
            }
            Thread serverThread = new Thread(() =>
            {

                while (isStart)
                {
                    try
                    {
                        UDTSocket csocket = server.Accept();
                        try
                        {
                            csocket.GetInputStream().SetLargeRead(islagerRead);
                            csocket.GetInputStream().ResetBufMaster(isRWMaster);
                        }
                        catch (IOException e)
                        {
                            Console.WriteLine(e);
                        }

                        SocketControls.GetInstance().AddSocket(csocket);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }


            });
            serverThread.IsBackground = true;
            serverThread.Name = ("judpServer_" + DateTime.Now.Ticks/10000);
            serverThread.Start();
            return true;
        }
    /**
     * �����Ƕ�ȡΪ������д��Ϊ��
     * �����д��Ϊ��������ȡ�ٶ���ʱ�����ݸ��Ƕ�ʧ
     * Ĭ�϶�ȡΪ������û�ж�ȡ�������ǣ��������ݣ��ȴ��ظ�
     * ���ô����ݶ�ȡ��������
     * @param isRead
     */
    public void setBufferRW(bool isRead)
    {
        this.isRWMaster = isRead;

    }

    /**
     * ���ô����ݶ�ȡ
     * Ĭ�� false
     * @param islarge
     */
    public void setLargeRead(bool islarge)
    {
        this.islagerRead = islarge;
    }
    /**
     * �������ӵ�socket
     */
    public judpSocket Accept()
    {
        UDTSocket socket = SocketControls.GetInstance().GetSocket();
        if (socket == null)
        {
            //�ٴλ�ȡ��һ��
            socket = SocketControls.GetInstance().GetSocket();
        }
        //��װ
        judpSocket jsocket = new judpSocket(socket);
            judpSocketManager.GetInstance(socket.GetEndpoint()).AddSocket(jsocket);
        return jsocket;

    }
}
}
