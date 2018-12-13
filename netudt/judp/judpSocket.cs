


using netudt;
using netudt.packets;
using netudt.util;
using System;
using System.IO;
using System.Threading;
/**
* @author jinyu
*
*����˷��ص�����ӿڶ���
*����socket����������ݵĶ���
*/
namespace netudt.judp
{
    public class judpSocket {
        private int bufSize = 65535;
        private UDTSocket socket = null;
        private bool isClose = false;
        private long sendLen = 0;//��������
        private long socketID = 0;//ID
        private Thread closeThread;
        private const int waitClose = 10 * 1000;
        private PackagetCombin pack = new PackagetCombin();
        //private int readLen=0;
        public int dataLen = 0;
        public void setRecBufferSize(int size)
        {
            bufSize = size;
        }
        public bool getCloseState()
        {
            //�ײ��Ѿ��ر�
            return isClose | socket.IsClose();
        }
        public judpSocket(UDTSocket usocket)
        {
            this.socket = usocket;
            socketID = socket.GetSession().SocketID;
        }

        /**
         * ��ȡID
         * @return
         */
        public long SocketID
        {
            get { return socketID; }
        }

        /**
         * �ر�
         * �ȴ�������ɹرգ�shutdownȡ��
         */
      
    public void Close()
        {
            isClose = true;
            //������ʵ�ر�
            if (sendLen == 0)
            {
                Stop();
               Console.WriteLine("����ر�socket");
            }
            else
            {
                //�й����������򻺳�
                //SocketManager.getInstance().add(socket);

                if (closeThread == null)
                {
                    closeThread = new Thread(()=> {

                        int num = 0;
                        while (true)
                        {
                            if (socket.GetSender().IsSenderEmpty())
                            {
                                Stop();
                                break;
                            }
                            else
                            {
                                
                                   Thread.Sleep(100);
                                    num++;
                                    if (waitClose <= num * 100)
                                    {
                                        Stop();
                                        break;
                                    }
                               
                            }
                        }

                    

                });
                closeThread.IsBackground=true;
                closeThread.Name="closeThread";
            }
                if (!closeThread.IsAlive)
                {
                    closeThread.Start();
                }
          
        }
    }

    /**
     * �����رգ�shutdownȡ��
     */
public void Stop()
    {
        //û�з��������ֱ�ӹرգ�����Ҫ�ȴ����ݷ������
        try {
                socket.Close();
            UDTSession serversession = socket.GetEndpoint().RemoveSession(socketID);
            if (serversession != null)
            {
                serversession.Socket.Close();
                socket.GetReceiver().Stop();
                socket.GetSender().Stop();
                Console.WriteLine("����ر�socket:" + serversession.SocketID);
            }

            serversession = null;
        } catch (IOException e) {
                Console.WriteLine(e);
        }
            Console.WriteLine("����ر�socket");
    }

    /**
     * 
    * @Title: shutdown
    * @Description: 10s�ڹر�ͨ��
    * @param     ����
    * @return void    ��������
     */
    public void Shutdown()
    {
        this.Close();
    }

    /**
     * 
    * @Title: shutdownNow
    * @Description: �����ر�ͨ��
    * @param     ����
    * @return void    ��������
     */
    public void ShutdownNow()
    {
        this.Stop();
    }
    /**
     * ��ȡ����
     * ���ؽ��յ��ֽڴ�С
     */
    public int ReadData(byte[] data)
    {
        if (getCloseState())
        {
            return -1;
        }
        try {
            int r = socket.GetInputStream().Read(data);
            //readLen+=r;
            return r;
        } catch (IOException e) {
                Console.WriteLine(e);
        }
        return -1;
    }

    /**
     * ��ȡȫ������
     */
    public byte[] ReadALL()
    {
        byte[] result = null;
        if (socket != null)
        {
            byte[] readBytes = new byte[bufSize];//������
            int r = 0;
            while (true)
            {
                if (getCloseState())
                {
                    return null;
                }
                r = ReadData(readBytes);
                if (r == -1)
                {
                    result = pack.GetData();
                    break;
                }
                else
                {
                    // readLen+=r;
                    if (r == 0)
                    {
                       
                           Thread.Sleep(100);

                            continue;
                      
                    }
                    //
                    byte[] buf = new byte[r];
                   Array.Copy(readBytes, 0, buf, 0, r);
                    if (pack.AddData(buf))
                    {
                        result = pack.GetData();
                        break;
                    }


                }
            }

        }

        return result;
    }


    /*
     * ��ȡ��ʼ������
     */
    public long GetInitSeqNo()
    {
        if (socket != null)
        {
                return socket.GetSession().InitialSequenceNumber;
        }
        return 0;
    }

    /**
     * ���Ͱ���
     */
    public int GetDataStreamLen()
    {
            return socket.GetSession().DatagramSize;
    }

    /**
     * Ŀ��socket
     * @return
     */
    public Destination GetDestination()
    {

        if (socket != null)
        {
                return socket.GetSession().Destination;
        }
        Destination tmp = null;
        try {
            tmp = new Destination(Tools.GetLocalHostLANAddress().ToString(), 0);
        } catch (Exception e) {
                Console.WriteLine(e);
        }
        return tmp;
    }


    /**
     * ��������
     * �����ݲ��ܷ���
     */
    public bool SendData(byte[] data) {
        if (getCloseState())
        {
            return false;
        }
        try {

            socket.GetOutputStream().Write(data);
            socket.GetOutputStream().Flush();
            sendLen = +1;
            return true;
        } catch (IOException e) {
                Console.WriteLine(e);
        }
        return false;
    }

    /**
     * �ְ���������
     * ���ٴηָ����ݣ�ͬʱ���ͷ
     * ��Ӧ��Ҫ��readALL
     * @param data
     * @return
     */
    public bool SendSplitData(byte[] data) {
        if (getCloseState())
        {
            return false;
        }
        byte[][] result = null;
        if (dataLen == 0)
        {
            result = PackagetSub.splitData(data);
        }
        else
        {
            PackagetSub sub = new PackagetSub();
            result = sub.Split(data, dataLen);
        }
        for (int i = 0; i < result.Length; i++)
        {
            if (!SendData(result[i]))
            {
                //һ�η���ʧ���򷵻�ʧ��
                return false;
            }
        }
        return true;
    }
    /**
     * ��ȡԶ��host
     * @return
     */
    public string getRemoteHost() {
            return socket.GetSession().Destination.GetAddress().ToString();

    }

    /**
     * ��ȡԶ�˶˿�
     * @return
     */
    public int GetRemotePort() {
            return socket.GetSession().Destination.GetPort();
    }

    /**
     * socketid
     * @return
     */
    public long GetID() {

        return socketID;
    }
    /**
     * �����Ƕ�ȡΪ������д��Ϊ��
     * �����д��Ϊ��������ȡ�ٶ���ʱ�����ݸ��Ƕ�ʧ
     * Ĭ�϶�ȡΪ������û�ж�ȡ�������ǣ��������ݣ��ȴ��ط�
     * ���ô����ݶ�ȡ��������
     * @param isRead
     */
    public void setBufferRW(bool isRead)
    {
        try {
            socket.GetInputStream().ResetBufMaster(isRead);
        } catch (IOException e) {
                Console.WriteLine(e);
        }
    }

    /**
     * ���ô����ݶ�ȡ
     * Ĭ�� false
     * @param islarge
     */
    public void setLargeRead(bool islarge)
    {
        try {
            socket.GetInputStream().SetLargeRead(islarge);
        } catch (IOException e) {
                Console.WriteLine(e);
        }
    }

}
}
