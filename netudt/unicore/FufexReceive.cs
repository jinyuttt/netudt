





using netudt.util;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
/**
* Receive part of the UNICORE integration code.
*  
* usage: recvfile server_ip local_filename mode [comm_out comm_in]
* 
* 
*/
namespace netudt.unicore
{ 
public class FufexReceive  {

	private  string serverIP;
	private  string localFile;
	private  bool append;
	private  string commOut;
	private  string commIn;

	public FufexReceive(string serverIP, string localFile, bool append, string commOut, string commIn){
		this.serverIP=serverIP;
		this.localFile=localFile;
		this.append=append;
		this.commIn=commIn;
		this.commOut=commOut;
	}

	
	public void run(){
		try{
			//open the UDPEndpoint
			UDTClient client=new UDTClient();
                int localPort = client.GetEndpoint().LocalPort;
			//write the port to output
			writeToOut("OUT: "+localPort);
			
			//read peer port from input file or stdin
			string peerPortS=ReadFromIn();
			int serverPort=int.Parse(peerPortS);
			
			//connect...
			client.Connect(serverIP,serverPort);
			var inStream = client.GetInputStream();
			
			//read file size info (an 4-byte int) 
			byte[]sizeInfo=new byte[4];
            inStream.Read(sizeInfo);
                long size = BitConverter.ToInt32(sizeInfo,0);
			
			//now read file data
			FileStream fos=new FileStream(localFile,FileMode.Append);
			try{
				//Util.copy(inStream, fos, size, false);
			}finally{
				fos.Close();
			}
			
			
		}catch(Exception ex){
			
		}
	}

	public static void main(string[] args){
		if(args.Length<3)usage();
		
		string serverIP=args[0];
		string localFile=args[1];
		bool append=args[2].Equals("A");
		string commIn=null;
		string commOut=null;
		if(args.Length>3){
			commOut=args[3];
			commIn=args[4];
		}
		FufexReceive fr=new FufexReceive(serverIP,localFile,append,commOut,commIn);
		fr.run();
	}

	//print usage info and exit with error
	private static void usage(){
		Console.WriteLine("usage: recvfile server_ip local_filename mode [comm_out comm_in]");
		 
	}

       

        private string ReadFromIn(){
            return null;
		//InputStream in=System.in;
		//if(commIn!=null){
		//	File file=new File(commIn);
		//	while(!file.exists()){
		//		Thread.sleep(2000);
		//	}
		//	in=new FileStream(file);
		//}
		//BufferedReader br = new BufferedReader(new InputStreamReader(in));
		//try{
		//	return br.readLine();
		//}ly{
		//	if(commIn!=null)in.close();//do not close System.in
		//}
	}
	
	
	private void writeToOut(string line){
		if(commOut!=null){
			appendToFile(commOut, line);
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
	private void appendToFile(string name, string line) {
		FileInfo f=new FileInfo(name);
            //FileStream fos=new FileStream(name,FileMode.Append);
            using (StreamWriter sw = new StreamWriter(name, true))
            {
                try {
                    sw.WriteLine(line);
                    //fos.write('\n');
                }
                finally {
                    sw.Close();
                }
            }
	}
    }
}
