
using System.Globalization;
using System.Net;
using System;
using System.Text;
using System.IO;

namespace netudt.util
{ 
public class ReceiveFile : Application{

	private  int serverPort;
	private  string serverHost;
	private  string remoteFile;
	private  string localFile;
	private  NumberFormatInfo format;
	
	public ReceiveFile(string serverHost, int serverPort, string remoteFile, string localFile){
		this.serverHost=serverHost;
		this.serverPort=serverPort;
		this.remoteFile=remoteFile;
		this.localFile=localFile;
            format = NumberFormatInfo.CurrentInfo;
            format.NumberDecimalDigits = 3;
        //format.setMaximumFractionDigits(3);
        }
	
	
	public void Run(){
		Configure();
		verbose=true;
		try{
			UDTReceiver.connectionExpiryDisabled=true;
            
            UDTClient client = new UDTClient(localPort,localIP);
			client.Connect(this.serverHost, this.serverPort);
			UDTInputStream inStream=client.GetInputStream();
			UDTOutputStream outStream= client.GetOutputStream();
            Console.WriteLine("[ReceiveFile] Requesting file "+remoteFile);
            byte[] fName =Encoding.UTF8.GetBytes(remoteFile);//兼容java
			//send file name info
			byte[]nameinfo=new byte[fName.Length+4];
			Array.Copy(Encode(fName.Length), 0, nameinfo, 0, 4);
            Array.Copy(fName, 0, nameinfo, 4, fName.Length);
            outStream.Write(nameinfo);
            outStream.Flush();
                //pause the sender to save some CPU time
            outStream.PauseOutput();
			
			//read size info (an 64 bit number) 
			byte[]sizeInfo=new byte[8];
			
			int total=0;
			while(total<sizeInfo.Length){
				int r= inStream.Read(sizeInfo);
				if(r<0)break;
				total+=r;
			}
            //读取文件长度
			long size=Decode(sizeInfo, 0);
			
			FileInfo file=new FileInfo(localFile);
			Console.WriteLine("[ReceiveFile] Write to local file <"+file.FullName+">");
			FileStream fos=new FileStream(file.FullName,FileMode.Append);//准备写入文件
			
			try{
                    Console.WriteLine("[ReceiveFile] Reading <"+size+"> bytes.");
                    long start = DateTime.Now.Ticks;
                    //and read the file data
                    //Util.copy(in, os, size, false);
                    CopyFile(fos, inStream, size, false);
                    long end = DateTime.Now.Ticks;
				    double rate=1000.0*size/1024/1024/(end-start);
                    Console.WriteLine("[ReceiveFile] Rate: "+rate.ToString(format)+" MBytes/sec. "
						+(8*rate).ToString(format) +" MBit/sec.");
			
				  client.Shutdown();
				
				if(verbose) Console.WriteLine(client.GetStatistics());
				
			}finally{
				fos.Close();
			}		
		}catch(Exception ex){
			//throw new RuntimeException(ex);
		}
	}

        private void CopyFile(FileStream fos, UDTInputStream inStream, long size, bool flush)
        {
            byte[] buf = new byte[8 * 65536];
            int c;
            long read = 0;
            while (true)
            {
                c = inStream.Read(buf, 0, buf.Length);
                if (c < 0) break;
                read += c;
                Console.WriteLine("writing <" + c + "> bytes");
                fos.Write(buf, 0, c);
                if (flush) inStream.Flush();
                if (read >= size && size > -1) break;

            }
            if (!flush) inStream.Flush();
        }

        public static void Main(string[] fullArgs){
		int serverPort=65321;
		string serverHost="localhost";
		string remoteFile="";
		string localFile="";
		
		string[] args=ParseOptions(fullArgs);
		
		try{
			serverHost=args[0];
			serverPort=int.Parse(args[1]);
			remoteFile=args[2];
			localFile=args[3];
		}catch(Exception ex){
			Usage();
			//System.exit(1);
		}
		
		ReceiveFile rf=new ReceiveFile(serverHost,serverPort,remoteFile, localFile);
		rf.Run();
	}
	
	public static void Usage(){
		Console.WriteLine("Usage: java -cp .. udt.util.ReceiveFile " +
				"<server_ip> <server_port> <remote_filename> <local_filename> " +
				"[--verbose] [--localPort=<port>] [--localIP=<ip>]");
	}
}
	
}
