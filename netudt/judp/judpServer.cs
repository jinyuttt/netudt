



using netudt;
using netudt.Log;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
/**
* @author jinyu
* 服务端接收封装
* 服务端
*/
namespace netudt.judp
{
    public class judpServer {
        private UDTServerSocket server = null;
        private bool isStart = true;
        private  bool isSucess = true;
        private bool isRWMaster = true;//与默认值一致
        private bool islagerRead = false;
  
        /**
         * 关闭服务端
         */
        
        public void Close()
        {
            isStart = false;
            server.getEndpoint().Stop();
        }

        /**
         * 
         * @param port 端口
         */
        public judpServer(int port)
        {

            try {
                server = new UDTServerSocket(port);
            } catch (SocketException e) {
               // logger.log(Level.WARNING, "绑定失败：" + e.getMessage());
                FlashLogger.Warn("绑定失败：" + e.Message);
                isSucess = false;
            } catch (Exception e) {
                isSucess = false;
                Console.WriteLine(e);
            }
        }

        /**
         * 
         * @param localIP 本地IP
         * @param port  端口
         */
        public judpServer(string localIP, int port)
        {
            try {
               
                server = new UDTServerSocket(port,localIP);

            } catch (SocketException e) {
                FlashLogger.Warn("绑定失败：" + e.Message);
                isSucess = false;
            } catch (Exception e) {
                isSucess = false;
                Console.WriteLine(e);
            }
        }



        /**
         * 启动接收
         */
        public bool Start()
        {
            if (!isStart || !isSucess)
            {

                FlashLogger.Warn("已经关闭的监听或监听端口不能使用");
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
     * 设置是读取为主还是写入为主
     * 如果是写入为主，当读取速度慢时，数据覆盖丢失
     * 默认读取为主，还没有读取则不允许覆盖，丢掉数据，等待重复
     * 设置大数据读取才有意义
     * @param isRead
     */
    public void setBufferRW(bool isRead)
    {
        this.isRWMaster = isRead;

    }

    /**
     * 设置大数据读取
     * 默认 false
     * @param islarge
     */
    public void setLargeRead(bool islarge)
    {
        this.islagerRead = islarge;
    }
    /**
     * 返回连接的socket
     */
    public judpSocket Accept()
    {
        UDTSocket socket = SocketControls.GetInstance().GetSocket();
        if (socket == null)
        {
            //再次获取下一个
            socket = SocketControls.GetInstance().GetSocket();
        }
        //包装
        judpSocket jsocket = new judpSocket(socket);
            judpSocketManager.GetInstance(socket.GetEndpoint()).AddSocket(jsocket);
        return jsocket;

    }
}
}
