
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
	
	
	public void run(){
		configure();
		verbose=true;
		try{
			UDTReceiver.connectionExpiryDisabled=true;
                // IPAddress myHost = localIP != null ? IPAddress.Parse(localIP) : IPAddress.Any;

                UDTClient client = new UDTClient(localPort,localIP);
			//client.Connect(this.serverHost, this.serverPort);
			UDTInputStream inStarem=client.GetInputStream();
			UDTOutputStream outStarem = client.GetOutputStream();

                //System.out.println("[ReceiveFile] Requesting file "+remoteFile);
                byte[] fName =Encoding.UTF8.GetBytes(remoteFile);

			
			//send file name info
			byte[]nameinfo=new byte[fName.Length+4];
			Array.Copy(encode(fName.Length), 0, nameinfo, 0, 4);
                Array.Copy(fName, 0, nameinfo, 4, fName.Length);

                outStarem.Write(nameinfo);
                outStarem.Flush();
                //pause the sender to save some CPU time
                outStarem.PauseOutput();
			
			//read size info (an 64 bit number) 
			byte[]sizeInfo=new byte[8];
			
			int total=0;
			while(total<sizeInfo.Length){
				int r= inStarem.Read(sizeInfo);
				if(r<0)break;
				total+=r;
			}
			long size=decode(sizeInfo, 0);
			
			FileInfo file=new FileInfo(localFile);
			Console.WriteLine("[ReceiveFile] Write to local file <"+file.FullName+">");
			FileStream fos=new FileStream(file.FullName,FileMode.Create);
			MemoryStream os=new MemoryStream(1024*1024);
			try{
                    Console.WriteLine("[ReceiveFile] Reading <"+size+"> bytes.");
                    long start = DateTime.Now.Ticks;
                    //and read the file data
                    //Util.copy(in, os, size, false);
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
	
	
	public static void main(string[] fullArgs){
		int serverPort=65321;
		string serverHost="localhost";
		string remoteFile="";
		string localFile="";
		
		string[] args=parseOptions(fullArgs);
		
		try{
			serverHost=args[0];
			serverPort=int.Parse(args[1]);
			remoteFile=args[2];
			localFile=args[3];
		}catch(Exception ex){
			usage();
			//System.exit(1);
		}
		
		ReceiveFile rf=new ReceiveFile(serverHost,serverPort,remoteFile, localFile);
		rf.run();
	}
	
	public static void usage(){
		Console.WriteLine("Usage: java -cp .. udt.util.ReceiveFile " +
				"<server_ip> <server_port> <remote_filename> <local_filename> " +
				"[--verbose] [--localPort=<port>] [--localIP=<ip>]");
	}
}
	
}
