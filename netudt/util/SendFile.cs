
using netudt.Log;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace netudt.util
{
    public class SendFile : Application
    {

        private readonly int serverPort;

     

        public SendFile(int serverPort)
        {
            this.serverPort = serverPort;

        }


        public override void Configure()
        {
            // super.configure();
            base.Configure();
        }


        public void Start()
        {
            Task.Factory.StartNew(() =>
            {
                Configure();
                try
                {
                    UDTReceiver.connectionExpiryDisabled = true;
                   
                    UDTServerSocket server = new UDTServerSocket(serverPort, localIP);
                    while (true)
                    {
                        UDTSocket socket = server.Accept();
                        Thread.Sleep(1000);
                       
                        Task.Factory.StartNew(() =>
                        {
                            new RequestRunner(socket).Run();
                        });
                    }
                }
                catch (Exception ex)
                {
                    // throw new RuntimeException(ex);
                }
            });
        }

        /**
         * main() method for invoking as a commandline application
         * @param args
         * @throws Exception
         */
        public static void Main(String[] fullArgs)
        {

            string[] args = ParseOptions(fullArgs);

            int serverPort = 65321;
            try
            {
                serverPort = int.Parse(args[0]);
            }
            catch (Exception ex)
            {
                Usage();
                //System.exit(1);
            }
            SendFile sf = new SendFile(serverPort);
            sf.Start();
        }

        public static void Usage()
        {
            Console.WriteLine("Usage: java -cp ... udt.util.SendFile <server_port> " +
        "[--verbose] [--localPort=<port>] [--localIP=<ip>]");
        }

        public class RequestRunner
        {

           
            private UDTSocket socket;
            private NumberFormatInfo format = NumberFormatInfo.CurrentInfo;


          
            public RequestRunner(UDTSocket socket)
            {
                this.socket = socket;
                format.PercentDecimalDigits = 3;
               
            }


            public void Run()
            {
                try
                {
                    FlashLogger.Info("Handling request from "+socket.GetSession().Destination);
                    UDTInputStream intStream = socket.GetInputStream();
                    UDTOutputStream  outStream = socket.GetOutputStream();
                    byte[] readBuf = new byte[32768];

                    //read file name info 
                    while (intStream.Read(readBuf) == 0) Thread.Sleep(100);

                    //how many bytes to read for the file name
                    byte[] len = new byte[4];
                    //bb.get(len);
                    
                    Array.Copy(readBuf, 0, len,0, 4);//文件名称长度
                    if (verbose)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < len.Length; i++)
                        {
                            sb.Append((len[i]).ToString());
                            sb.Append(" ");
                        }
                        Console.WriteLine("[SendFile] name length data: " + sb.ToString());
                    }
                    long length = Decode(len, 0);
                    if (verbose) Console.WriteLine("[SendFile] name length     : " + length);
                    byte[] fileName = new byte[(int)length];
                    //读取文件名称
                    Array.Copy(readBuf, 4, fileName,0, fileName.Length);
                    FileInfo file = new FileInfo(Encoding.UTF8.GetString(fileName));//兼容java
                    Console.WriteLine("[SendFile] File requested: '" + file.FullName + "'");

                    FileStream fis = null;
                    try
                    {
                        long size = file.Length;
                        Console.WriteLine("[SendFile] File size: " + size);
                        //send size info
                        outStream.Write(Encode64(size));//先写入大小
                        outStream.Flush();//传输完成

                        long start = DateTime.Now.Ticks;
                        fis = new FileStream(file.FullName, FileMode.Open);

                        CopyFile(fis, outStream, size, false);
                        Console.WriteLine("[SendFile] Finished sending data.");
                        long end = DateTime.Now.Ticks;
                        Console.WriteLine(socket.GetSession().Statistics.toString());
                        double rate = 1000.0 * size / 1024 / 1024 / (end - start);
                        Console.WriteLine("[SendFile] Rate: " + rate.ToString(format) + " MBytes/sec. " + (8 * rate).ToString(format) + " MBit/sec.");
                       
                    }
                    finally
                    {
                        socket.GetSender().Stop();
                        if (fis != null) fis.Close();
                    }
                    FlashLogger.Info("Finished request from "+socket.GetSession().Destination);
                }
                catch (Exception ex)
                {
                    FlashLogger.Error(ex.Message);
                 
                }
            }

            /// <summary>
            /// 文件传输
            /// </summary>
            /// <param name="fis">文件流</param>
            /// <param name="outStream">网络传输流</param>
            /// <param name="size">文件大小</param>
            /// <param name="flush">是否及时刷新</param>
            private void CopyFile(FileStream fis,UDTOutputStream outStream,long size,bool flush)
            {
                byte[] buf = new byte[8 * 65536];
                int c;
                long read = 0;
                while (true)
                {
                    c = fis.Read(buf,0,buf.Length);
                    if (c < 0) break;
                    read += c;
                    Console.WriteLine("writing <"+c+"> bytes");
                    outStream.Write(buf, 0, c);
                    if (flush) outStream.Flush();
                    if (read >= size && size > -1) break;

                }
                if (!flush) outStream.Flush();
            }



           
        }
    }
}
