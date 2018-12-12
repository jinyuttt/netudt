



using netudt.Log;
using System.Net;
using System.Threading;

namespace netudt
{ 
public class UDTServerSocket {
	
	
	private  UDPEndPoint endpoint;
	
	private bool started=false;
	
	private volatile bool shutdown=false;

        private object lock_obj = new object();
	
	/**
	 * create a UDT ServerSocket
	 * @param localAddress
	 * @param port - the local port. If 0, an ephemeral port will be chosen
	 */
	public UDTServerSocket(int port, string localAddress=null)
        {
		endpoint=new UDPEndPoint(port,localAddress);
		FlashLogger.Info("Created server endpoint on port "+endpoint.LocalPort);
	}


	
	/**
	 * listens and blocks until a new client connects and returns a valid {@link UDTSocket}
	 * for the new connection
	 * @return
	 */
	public  UDTSocket Accept(){
            lock (lock_obj)
            {
                if (!started)
                {
                    endpoint.Start(true);
                    started = true;
                }
                while (!shutdown)
                {
                    UDTSession session = endpoint.Accept(10000);
                    if (session != null)
                    {
                        //wait for handshake to complete
                        while (!session.IsReady || session.Socket == null)
                        {
                            Thread.Sleep(100);
                        }
                        return session.Socket;
                    }
                }
                return null;
            }
	} 
	
	public void ShutDown(){
		shutdown=true;
		endpoint.Stop();
	}
	
	public UDPEndPoint getEndpoint(){
		return endpoint;
	}
}
}
