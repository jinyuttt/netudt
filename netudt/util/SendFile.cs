/*********************************************************************************
 * Copyright (c) 2010 Forschungszentrum Juelich GmbH 
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * (1) Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the disclaimer at the end. Redistributions in
 * binary form must reproduce the above copyright notice, this list of
 * conditions and the following disclaimer in the documentation and/or other
 * materials provided with the distribution.
 * 
 * (2) Neither the name of Forschungszentrum Juelich GmbH nor the names of its 
 * contributors may be used to endorse or promote products derived from this 
 * software without specific prior written permission.
 * 
 * DISCLAIMER
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 *********************************************************************************/







using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
/**
* helper application for sending a single file via UDT
* Intended to be compatible with the C++ version in 
* the UDT reference implementation
* 
* main method USAGE: java -cp .. udt.util.SendFile <server_port>
*/
namespace netudt.util
{
    public class SendFile : Application
    {

        private int serverPort;

        //TODO configure pool size
        // private ExecutorService threadPool = Executors.newFixedThreadPool(3);

        public SendFile(int serverPort)
        {
            this.serverPort = serverPort;

        }


        public void Configure()
        {
            // super.configure();
        }


        public void run()
        {
            Configure();
            try
            {
                UDTReceiver.connectionExpiryDisabled = true;
                // InetAddress myHost = localIP != null ? InetAddress.getByName(localIP) : InetAddress.getLocalHost();
                UDTServerSocket server = new UDTServerSocket(serverPort, localIP);
                while (true)
                {
                    UDTSocket socket = server.Accept();
                    Thread.Sleep(1000);
                    // threadPool.execute(new RequestRunner(socket));
                }
            }
            catch (Exception ex)
            {
                // throw new RuntimeException(ex);
            }
        }

        /**
         * main() method for invoking as a commandline application
         * @param args
         * @throws Exception
         */
        public static void main(String[] fullArgs)
        {

            string[] args = parseOptions(fullArgs);

            int serverPort = 65321;
            try
            {
                serverPort = int.Parse(args[0]);
            }
            catch (Exception ex)
            {
                usage();
                //System.exit(1);
            }
            SendFile sf = new SendFile(serverPort);
            sf.run();
        }

        public static void usage()
        {
            Console.WriteLine("Usage: java -cp ... udt.util.SendFile <server_port> " +
        "[--verbose] [--localPort=<port>] [--localIP=<ip>]");
        }

        public class RequestRunner
        {

            //private  static Logger logger=Logger.getLogger(RequestRunner.class.getName());

            private UDTSocket socket;
            private NumberFormatInfo format = NumberFormatInfo.CurrentInfo;


            private bool memMapped;
            public RequestRunner(UDTSocket socket)
            {
                this.socket = socket;
                format.PercentDecimalDigits = 3;
                memMapped = false;//true;
            }


            public void run()
            {
                try
                {
                    //logger.info("Handling request from "+socket.getSession().getDestination());
                    UDTInputStream intStream = socket.GetInputStream();
                    UDTOutputStream outtStream = socket.GetOutputStream();
                    byte[] readBuf = new byte[32768];
                    //ByteBuffer bb=ByteBuffer.wrap(readBuf);

                    //read file name info 
                    while (intStream.Read(readBuf) == 0) Thread.Sleep(100);

                    //how many bytes to read for the file name
                    byte[] len = new byte[4];
                    //bb.get(len);
                    if (verbose)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < len.Length; i++)
                        {
                            //sb.Append(int.toString(len[i]));
                            sb.Append(" ");
                        }
                        Console.WriteLine("[SendFile] name length data: " + sb.ToString());
                    }
                    long length = decode(len, 0);
                    if (verbose) Console.WriteLine("[SendFile] name length     : " + length);
                    byte[] fileName = new byte[(int)length];
                    //bb.get(fileName);

                    FileInfo file = new FileInfo(Encoding.UTF8.GetString(fileName));
                    Console.WriteLine("[SendFile] File requested: '" + file.FullName + "'");

                    FileStream fis = null;
                    try
                    {
                        long size = file.Length;
                        Console.WriteLine("[SendFile] File size: " + size);
                        //send size info
                        outtStream.Write(encode64(size));
                        outtStream.Flush();

                        long start = DateTime.Now.Ticks;
                        //and send the file
                        if (memMapped)
                        {
                            // copyFile(file, outtStream);
                        }
                        else
                        {
                            fis = new FileStream(file.FullName, FileMode.Open);
                            // Util.copy(fis, outtStream, size, false);
                        }
                        Console.WriteLine("[SendFile] Finished sending data.");
                        long end = DateTime.Now.Ticks;
                        Console.WriteLine(socket.GetSession().Statistics.toString());
                        double rate = 1000.0 * size / 1024 / 1024 / (end - start);
                        Console.WriteLine("[SendFile] Rate: " + rate.ToString(format) + " MBytes/sec. " + (8 * rate).ToString(format) + " MBit/sec.");
                        //if(bool.getbool("udt.sender.storeStatistics")){
                        //	socket.getSession().getStatistics().writeParameterHistory(new File("udtstats-"+System.currentTimeMillis()+".csv"));
                        //}
                    }
                    finally
                    {
                        socket.GetSender().Stop();
                        if (fis != null) fis.Close();
                    }
                    //logger.info("Finished request from "+socket.getSession().getDestination());
                }
                catch (Exception ex)
                {
                    //ex.printStackTrace();
                    //throw new RuntimeException(ex);
                }
            }




            private static void CopyFile(string file, UDTOutputStream os)
            {
                //FileChannel c=new RandomAccessFile(file,"r").getChannel();
                //MappedByteBuffer b=c.map(MapMode.READ_ONLY, 0, file.length());
                //b.load();
                //byte[]buf=new byte[1024*1024];
                //int len=0;
                //while(true){
                //	len=Math.min(buf.length, b.remaining());
                //	b.get(buf, 0, len);
                //	os.write(buf, 0, len);
                //	if(b.remaining()==0)break;
                //}
                //os.flush();
                //}	
            }
        }
    }
}
