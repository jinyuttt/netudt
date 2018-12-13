
using netudt;
using System;
using System.IO;
using System.Net;
using System.Text;
/**
* helper methods 
*/
namespace netudt.util
{ 
public class Util {

	/**
	 * get the current timer value in microseconds
	 * @return
	 */
	public static long getCurrentTime(){
		return  DateTime.Now.Ticks/1000;
	}
	
	
	public static  long SYN=10000;
	
	public static  double SYN_D=10000.0;
	
	/**
	 * get the SYN time in microseconds. The SYN time is 0.01 seconds = 10000 microseconds
	 * @return
	 */
	public static  long GetSYNTime(){
		return 10000;
	}
	
	public static double GetSYNTimeD(){
		return 10000.0;
	}
	
	/**
	 * get the SYN time in seconds. The SYN time is 0.01 seconds = 10000 microseconds
	 * @return
	 */
	public static double GetSYNTimeSeconds(){
		return 0.01;
	}
	/**
	 * read a line terminated by a new line '\n' character
	 * @param input - the input string to read from 
	 * @return the line read or <code>null</code> if end of input is reached
	 * @throws IOException
	 */
	public static string ReadLine(UDTInputStream input){
		return ReadLine(input, '\n');
	}
	
	/**
	 * read a line from the given input stream
	 * @param input - the input stream
	 * @param terminatorChar - the character used to terminate lines
	 * @return the line read or <code>null</code> if end of input is reached
	 * @throws IOException
	 */
	public static string ReadLine(UDTInputStream input, char terminatorChar){
		MemoryStream bos=new MemoryStream();
		while(true){
			int c=input.Read();
			if(c<0 && bos.Length==0)return null;
			if(c<0 || c==terminatorChar)break;
			else bos.Write(BitConverter.GetBytes(c),0,4);
		}
		return bos.Length>0? Encoding.Default.GetString(bos.ToArray()): null;
	}

        /**
         * copy input data from the source stream to the target stream
         * @param source - input stream to read from
         * @param target - output stream to write to
         * @throws IOException
         */

        //public static void copy(UDTInputStream source, UDTOutputStream target)
        //{
        //    Copy(source, target, -1, false);
        //}
	
	/**
	 * copy input data from the source stream to the target stream
	 * @param source - input stream to read from
	 * @param target - output stream to write to
	 * @param size - how many bytes to copy (-1 for no limit)
	 * @param flush - whether to flush after each write
	 * @throws IOException
	 */
	//public static void Copy(UDTInputStream source, UDTOutputStream target, long size, bool flush){
	//	byte[]buf=new byte[8*65536];
	//	int c;
	//	long read=0;
	//	while(true){
	//		c=source.Read(buf);
	//		if(c<0)break;
	//		read+=c;
	//		//System.out.println("writing <"+c+"> bytes");
	//		target.Write(buf, 0, c);
	//		if(flush)target.Flush();
	//		if(read>=size && size>-1)break;

 //   }
	//	if(!flush)target.Flush();
	//}
	
	/**
	 * perform UDP hole punching to the specified client by sending 
	 * a dummy packet. A local port will be chosen automatically.
	 * 
	 * @param client - client address
	 * @return the local port that can now be accessed by the client
	 * @throws IOException
	 */
	public static void DoHolePunch(UDPEndPoint endpoint,EndPoint point){
    UDPUserToken  p =new UDPUserToken((IPEndPoint)point);
    p.Buffer = new byte[1];
    p.Length = 1;
		//p.setAddress(client);
		//p.setPort(clientPort);
		endpoint.SendRaw(p);
	}

	//public static string hexString(MessageDigest digest){
 //   //byte[] messageDigest = digest.digest();
 //   //StringBuilder hexString = new StringBuilder();
 //   //for (int i=0;i<messageDigest.length;i++) {
 //   //	String hex = Integer.toHexString(0xFF & messageDigest[i]); 
 //   //	if(hex.length()==1)hexString.append('0');
 //   //	hexString.append(hex);
 //   //}
 //   //return hexString.toString();
 //   return "";
	//}


            /// <summary>
            /// 接收文件
            /// </summary>
            /// <param name="fos">文件写入流</param>
            /// <param name="inStream">网络接收流</param>
            /// <param name="size">文件大小</param>
            /// <param name="flush">是否及时刷新</param>
        public static void CopyFileReceiver(FileStream fos, UDTInputStream inStream, long size, bool flush)
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

        /// <summary>
        /// 文件传输
        /// </summary>
        /// <param name="fis">文件读取流</param>
        /// <param name="outStream">网络传输流</param>
        /// <param name="size">文件大小</param>
        /// <param name="flush">是否及时刷新</param>
        public static void CopyFileSender(FileStream fis, UDTOutputStream outStream, long size, bool flush)
        {
            byte[] buf = new byte[8 * 65536];
            int c;
            long read = 0;
            while (true)
            {
                c = fis.Read(buf, 0, buf.Length);
                if (c < 0) break;
                read += c;
                Console.WriteLine("writing <" + c + "> bytes");
                outStream.Write(buf, 0, c);
                if (flush) outStream.Flush();
                if (read >= size && size > -1) break;

            }
            if (!flush) outStream.Flush();
        }
    }
}
