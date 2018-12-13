


using System;
using System.IO;
using System.Threading;
/**
* 文件操作
*
*/
namespace netudt.judp
{
    public class ApplicationCode {
        static readonly  int bufsize = 10 * 1024 * 1024;//读取文件用
        static float speed = 0;//M
        internal static long decode(byte[] data, int start) {
            long result = (data[start + 3] & 0xFF) << 24
                         | (data[start + 2] & 0xFF) << 16
                         | (data[start + 1] & 0xFF) << 8
                         | (data[start] & 0xFF);
            return result;
        }

     internal   static byte[] Encode(long value) {
            byte m4 = (byte)(value >> 24);
            byte m3 = (byte)(value >> 16);
            byte m2 = (byte)(value >> 8);
            byte m1 = (byte)(value);
            return new byte[] { m1, m2, m3, m4 };
        }

        internal static byte[] encode64(long value) {
            byte m4 = (byte)(value >> 24);
            byte m3 = (byte)(value >> 16);
            byte m2 = (byte)(value >> 8);
            byte m1 = (byte)(value);
            return new byte[] { m1, m2, m3, m4, 0, 0, 0, 0 };
        }

        /**
         * 
            
         * 读取文件发送  
           
         * @param   文件，socket,发送包大小    
            
         * @return  
           
         *
         */
        
      internal  static void CopySocketFile(string file, judpSocket target, int packagetLen)
        {
            //byte[]buf=new byte[8*65536];
            FileStream fis = null;
            try {
                fis = new FileStream(file,FileMode.Open);
            } catch (FileNotFoundException e) {
                return;
            }
            byte[] buf = new byte[bufsize];
            int c = 0;
            long read = 0;
            long size = fis.Length;
            byte[] data = null;
            int sendCount = 0;
            long sendSum = 0;
            if (packagetLen <= 0)
            {
                packagetLen = 65535;
            }
             int sendLen = packagetLen - 24;
            long waitTime = 0;
            if (speed > 0)
            {
                waitTime = (long)(speed * 1000);

            }
           Console.WriteLine("sendFile_"+file + ",socketID:" + target.SocketID);
            while (true) {
                try {
                    c = fis.Read(buf, 0, buf.Length);
                } catch (IOException e) {

                    Console.WriteLine(e.Message);
                }
                if (c < 0) break;
                read += c;
                if (sendCount > 128)
                {
                    try {
                        Thread.Sleep(1000);
                        sendCount = 0;

                        Console.WriteLine("文件发送" + file + "," + sendSum);
                    } catch (Exception e) {
                        Console.WriteLine(e.Message);
                    }
                }
                if (c < sendLen)
                {
                    data = new byte[c];
                   Array.Copy(buf, 0, data, 0, c);
                    target.SendData(data);
                    sendCount++;
                    sendSum++;
                }
                else
                {
                    int offset = 0;
                    int len = c;
                    while (len > 0)
                    {
                        int clen = len > sendLen ? sendLen : len;
                        data = new byte[clen];
                        Array.Copy(buf, offset, data, 0, clen);
                        target.SendData(data);
                        len = len - clen;
                        offset += clen;
                        sendCount++;
                        sendSum++;
                        if (waitTime > 0 && sendSum % waitTime == 0)
                        {
                          
                                Thread.Sleep(1000);
                           
                        }

                    }


                }
                if (read >= size && size > -1) break;
            }

        }

        
        /*
         * 接收数据
         * 拷贝数据
         */
    internal   static void CopySocketFile(FileStream fos, judpClient target, long size, bool flush)
        {
          
            byte[] buf = new byte[bufsize];
            int c = 0;
            long read = 0;
            while (true) {
                c = target.read(buf);
                if (c < 0) break;
                try
                {
                    read += c;
                    
                    fos.WriteAsync(buf, 0, c);
                    if (flush) fos.Flush();
                    if (read >= size && size > -1) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            if (!flush)
                try {
                    fos.Flush();
                } catch (IOException e) {
                    Console.WriteLine(e.Message);
                }

        }
    }
}
