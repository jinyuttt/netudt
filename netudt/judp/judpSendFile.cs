


using netudt;
using netudt.Log;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
/**    
*     
* 项目名称：judp    
* 类名称：judpSendFile    
* 类描述：    文件发送
* 创建人：jinyu    
* 创建时间：2017年8月27日 下午4:30:42    
* 修改人：jinyu    
* 修改时间：2017年8月27日 下午4:30:42    
* 修改备注：    
* @version     
*     
*/
namespace netudt.judp
{ 
public class judpSendFile {
    private  int serverPort;
    private  string host;
   
    public judpSendFile(int serverPort){
        this.serverPort=serverPort;
        this.host=null;

    }
    public judpSendFile(string localIP,int serverPort){
        this.serverPort=serverPort;
        this.host=localIP;

    }


        /**
         * 
        * @Title: startSend
        * @Description: 开始发送
        * @param     参数
        * @return void    返回类型
         */
        public void StartSend()
        {
            Task.Factory.StartNew(()=>{

                try {
                    UDTReceiver.connectionExpiryDisabled = true;
                    judpServer server = null;
                    if (host == null)
                    {
                        server = new judpServer(serverPort);
                    }
                    else
                    {
                        server = new judpServer(host, serverPort);
                    }
                    server.Start();
                    while (true) {
                        judpSocket socket = server.Accept();
                        Thread.Sleep(1000);
                        //threadPool.execute(new RequestRunner(socket));
                        Task.Factory.StartNew(() =>
                        {
                            new RequestRunner(socket).run();
                        });
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                }
            } );
            
     
  }


    public static void Usage(){
            Console.WriteLine("Usage: java -cp ... udt.util.SendFile <server_port> " +
        "[--verbose] [--localPort=<port>] [--localIP=<ip>]");
    }

        public class RequestRunner
        {

            //private final static Logger logger=Logger.getLogger(RequestRunner.class.getName());

            private judpSocket socket = null;

            private NumberFormatInfo format = NumberFormatInfo.CurrentInfo;

            private bool memMapped;

            private bool verbose;
            public RequestRunner(judpSocket socket)
            {
                this.socket = socket;
                format.NumberDecimalDigits = 3;
                memMapped = false;//true;
            }


            public void run()
            {
                try
                {
                    // FlashLogger.Info("Handling request from "+socket.);

                    byte[] readBuf = new byte[32768];

                    MemoryStream stream = new MemoryStream(readBuf);
                    //read file name info 
                    int r = 0;
                    while (true)
                    {
                        r = socket.ReadData(readBuf);
                        if (r == 0)
                            Thread.Sleep(100);
                        else
                        {
                            break;
                        }
                    }
                    if (r == -1)
                    {
                        socket.Close();
                        return;
                    }
                    //how many bytes to read for the file name
                    byte[] len = new byte[4];
                    stream.Read(len, 0, 4);
                    if (verbose)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < len.Length; i++)
                        {
                            sb.Append(len[i].ToString());
                            sb.Append(" ");
                        }
                        Console.WriteLine("[SendFile] name length data: " + sb.ToString());
                    }
                    long length = ApplicationCode.decode(len, 0);
                    if (verbose) Console.WriteLine("[SendFile] name length: " + length);
                    byte[] fileName = new byte[(int)length];
                    stream.Read(fileName, 0, fileName.Length);
                    string file = Encoding.UTF8.GetString(fileName);
                    Console.WriteLine("[SendFile] File requested: '" + file + "'");

                    Thread.CurrentThread.Name = ("sendFile_" + file);
                    FileStream fis = new FileStream(file, FileMode.Open);
                    try
                    {
                        long size = fis.Length;
                        fis.Close();
                        Console.WriteLine("[SendFile] File size: " + size);
                        //send size info
                        socket.SendData(ApplicationCode.encode64(size));

                        long start = DateTime.Now.Ticks;
                        //                    //and send the file

                        ApplicationCode.CopySocketFile(file, socket, socket.GetDataStreamLen());

                        Console.WriteLine("[SendFile] Finished sending data.");
                        long end = DateTime.Now.Ticks;
                        //System.out.println(socket.getSession().getStatistics().toString());
                        double rate = 1000.0 * size / 1024 / 1024 / (end - start);
                        Console.WriteLine("[SendFile] Rate: " + rate.ToString(format) + " MBytes/sec. " + (8 * rate).ToString(format) + " MBit/sec.");
                        //                    if(Boolean.getBoolean("udt.sender.storeStatistics")){
                        //                        socket.getSession().getStatistics().writeParameterHistory(new File("udtstats-"+System.currentTimeMillis()+".csv"));
                        //                    }
                    }
                    finally
                    {
                        socket.Close();
                        if (fis != null) fis.Close();
                    }
                    FlashLogger.Info("Finished request from " + socket.GetDestination());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

            }
        }

   
    }
}
