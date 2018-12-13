


using netudt;
using netudt.packets;
using netudt.util;
using System;
using System.IO;
using System.Threading;
/**
* @author jinyu
*
*服务端返回的网络接口对象
*保存socket并检查有数据的对象
*/
namespace netudt.judp
{
    public class judpSocket {
        private int bufSize = 65535;
        private UDTSocket socket = null;
        private bool isClose = false;
        private long sendLen = 0;//发送数据
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
            //底层已经关闭
            return isClose | socket.IsClose();
        }
        public judpSocket(UDTSocket usocket)
        {
            this.socket = usocket;
            socketID = socket.GetSession().SocketID;
        }

        /**
         * 获取ID
         * @return
         */
        public long SocketID
        {
            get { return socketID; }
        }

        /**
         * 关闭
         * 等待数据完成关闭，shutdown取代
         */
      
    public void Close()
        {
            isClose = true;
            //不能真实关闭
            if (sendLen == 0)
            {
                Stop();
               Console.WriteLine("物理关闭socket");
            }
            else
            {
                //有过发送数据则缓冲
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
     * 立即关闭，shutdown取代
     */
public void Stop()
    {
        //没有发送则可以直接关闭，不需要等待数据发送完成
        try {
                socket.Close();
            UDTSession serversession = socket.GetEndpoint().RemoveSession(socketID);
            if (serversession != null)
            {
                serversession.Socket.Close();
                socket.GetReceiver().Stop();
                socket.GetSender().Stop();
                Console.WriteLine("物理关闭socket:" + serversession.SocketID);
            }

            serversession = null;
        } catch (IOException e) {
                Console.WriteLine(e);
        }
            Console.WriteLine("物理关闭socket");
    }

    /**
     * 
    * @Title: shutdown
    * @Description: 10s内关闭通信
    * @param     参数
    * @return void    返回类型
     */
    public void Shutdown()
    {
        this.Close();
    }

    /**
     * 
    * @Title: shutdownNow
    * @Description: 立即关闭通信
    * @param     参数
    * @return void    返回类型
     */
    public void ShutdownNow()
    {
        this.Stop();
    }
    /**
     * 读取数据
     * 返回接收的字节大小
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
     * 读取全部数据
     */
    public byte[] ReadALL()
    {
        byte[] result = null;
        if (socket != null)
        {
            byte[] readBytes = new byte[bufSize];//接收区
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
     * 获取初始化序列
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
     * 发送包长
     */
    public int GetDataStreamLen()
    {
            return socket.GetSession().DatagramSize;
    }

    /**
     * 目的socket
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
     * 发送数据
     * 空数据不能发送
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
     * 分包发送数据
     * 会再次分割数据，同时添加头
     * 对应的要用readALL
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
                //一次发送失败则返回失败
                return false;
            }
        }
        return true;
    }
    /**
     * 获取远端host
     * @return
     */
    public string getRemoteHost() {
            return socket.GetSession().Destination.GetAddress().ToString();

    }

    /**
     * 获取远端端口
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
     * 设置是读取为主还是写入为主
     * 如果是写入为主，当读取速度慢时，数据覆盖丢失
     * 默认读取为主，还没有读取则不允许覆盖，丢掉数据，等待重发
     * 设置大数据读取才有意义
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
     * 设置大数据读取
     * 默认 false
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
