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
	
	public void run(){
		try{
			//create an UDTServerSocket on a free port
			UDTServerSocket server=new UDTServerSocket(0);

                // do hole punching to allow the client to connect
                IPAddress clientAddress = IPAddress.Parse(clientIP);
                IPEndPoint point = new IPEndPoint(clientAddress, clientPort);
                //Util.DoHolePunch(server.getEndpoint(),(EndPoint) point);
                int localPort = server.getEndpoint().LocalPort;
			//output client port
			writeToOut("OUT: "+localPort);
			
			//now start the send...
			UDTSocket socket=server.Accept();
			UDTOutputStream outStream=socket.GetOutputStream();
			FileInfo file=new FileInfo(localFilename);
			FileStream fis=new FileStream(localFilename,FileMode.Create);
			try{
                    //send file size info
                    long size = file.Length;
				//PacketUtil.Encode(size);
				//out.write(PacketUtil.Encode(size));
                    long start = DateTime.Now.Ticks;
				//and send the file
				//Util.copy(fis, out, size,true);
				long end = DateTime.Now.Ticks;
                    //	System.out.println(socket.GetSession().getStatistics());
                    float mbRate=1000*size/1024/1024/(end-start);
				float mbitRate=8*mbRate;
				//System.out.println("Rate: "+(int)mbRate+" MBytes/sec. "+mbitRate+" mbit/sec.");
			}finally{
				fis.Close();
			}
		}catch(Exception ex){
			
		}
	}
	
	
	//print usage info and exit with error
	private static void usage(){
		Console.WriteLine("usage: send client_ip client_port local_filename [comm_file_name]");
		//System.exit(1);
	}

	public static void main(string[] args){
		if(args.Length<3)usage();
		
		string commFileName=null;
		if(args.Length>3){
			commFileName=args[3];
		}

		string clientIP=args[0];
		int clientPort=int.Parse(args[1]);
		
		FufexSend fs=new FufexSend(clientIP,clientPort,args[2],commFileName);
		fs.run();
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
