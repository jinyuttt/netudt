
using netudt.packets;
using netudt.util;
using System;
using System.IO;
using System.Net;
/**
* This commandline application is run on the target system to
* send a remote file. <br/>
* 
* Performs UDP hole punching, waits for a client connect and
* sends the specified file. <br/>
* 
* usage: sendfile client_ip client_port local_filename [comm_file_name]
*
* 
*/
namespace netudt.unicore
{ 
public class FufexSend {

	private  string clientIP;
	private  int clientPort;
	private  string localFilename;
	private  string commFilename;
	
	public FufexSend(string clientIP, int clientPort, string localFilename, string commFilename){
		this.clientIP=clientIP;
		this.clientPort=clientPort;
		this.localFilename=localFilename;
		this.commFilename=commFilename;
	}

        public void Run()
        {
            try
            {
                //create an UDTServerSocket on a free port
                UDTServerSocket server = new UDTServerSocket(0);
                // do hole punching to allow the client to connect
                IPAddress clientAddress = IPAddress.Parse(clientIP);
                IPEndPoint point = new IPEndPoint(clientAddress, clientPort);
                //发送一字节确认端口
                Util.DoHolePunch(server.getEndpoint(), point);
                int localPort = server.getEndpoint().LocalPort;//获取真实端口
                                                               //output client port
                writeToOut("OUT: " + localPort);

                //now start the send...
                UDTSocket socket = server.Accept();
                UDTOutputStream outStream = socket.GetOutputStream();
                FileInfo file = new FileInfo(localFilename);
                if (!file.Exists)
                {
                    Console.WriteLine("没有文件：" + localFilename);
                    socket.Close();
                    server.ShutDown();
                    return;
                }
                FileStream fis = new FileStream(localFilename, FileMode.Open);
                try
                {
                    //send file size info
                    long size = fis.Length;
                    PacketUtil.Encode(size);
                    outStream.Write(PacketUtil.Encode(size));
                    long start = DateTime.Now.Ticks;
                    //and send the file
                    Util.CopyFileSender(fis, outStream, size, false);
                    long end = DateTime.Now.Ticks;
                    Console.WriteLine(socket.GetSession().Statistics);
                    float mbRate = 1000 * size / 1024 / 1024 / (end - start);
                    float mbitRate = 8 * mbRate;
                    Console.WriteLine("Rate: " + (int)mbRate + " MBytes/sec. " + mbitRate + " mbit/sec.");
                }
                finally
                {
                    fis.Close();
                    socket.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
	
	
	//print usage info and exit with error
	private static void Usage(){
		Console.WriteLine("usage: send client_ip client_port local_filename [comm_file_name]");
		//System.exit(1);
	}

	public static void main(string[] args){
		if(args.Length<3)Usage();
		
		string commFileName=null;
		if(args.Length>3){
			commFileName=args[3];
		}

		string clientIP=args[0];
		int clientPort=int.Parse(args[1]);
		
		FufexSend fs=new FufexSend(clientIP,clientPort,args[2],commFileName);
		fs.Run();
	}

	private void writeToOut(string line){
		if(commFilename!=null){
			appendToFile(commFilename, line);
		}
		else{
			Console.WriteLine(line);
		}
	}
	
	/**
	 * append a line to the named file (and a newline character)
	 * @param name - the file to write to
	 * @param line - the line to write
	 */
	private void appendToFile(string name, string line){
		FileInfo f=new FileInfo(name);
            using (StreamWriter sw = new StreamWriter(name, true))
            {
                sw.WriteLine(line);
            }
	}
	}
}
