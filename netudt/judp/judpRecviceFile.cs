

using netudt;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
/**    
*     
* ��Ŀ���ƣ�judp    
* �����ƣ�judpRecviceFile    
* ��������    ���������ļ�
* �����ˣ�jinyu    
* ����ʱ�䣺2017��8��27�� ����4:30:29    
* �޸��ˣ�jinyu    
* �޸�ʱ�䣺2017��8��27�� ����4:30:29    
* �޸ı�ע��    
* @version     
*     
*/
namespace netudt.judp
{
    public class judpRecviceFile
    {
        private int serverPort;
        private string serverHost;
        private string remoteFile;
        private string localFile;
        private NumberFormatInfo format;
        //
        public string localIP = null;
        public int localPort = 0;

        public judpRecviceFile(string serverHost, int serverPort, string remoteFile, string localFile)
        {
            this.serverHost = serverHost;
            this.serverPort = serverPort;
            this.remoteFile = remoteFile;
            this.localFile = localFile;
            format = NumberFormatInfo.CurrentInfo;
            format.NumberDecimalDigits = 3;

        }

        /// <summary>
        /// ���ý��յ��ļ�
        /// </summary>
        /// <param name="remoteFile">Զ���ļ�</param>
        /// <param name="localFile">д�뱾���ļ�</param>
        public void SetFile(string remoteFile, string localFile)
        {
            this.remoteFile = remoteFile;
            this.localFile = localFile;
        }

        /// <summary>
        /// ��ʼ����
        /// </summary>
        public void Start()
        {
            try
            {
                UDTReceiver.connectionExpiryDisabled = true;
                //UDTClient client=localPort!=-1?new UDTClient(myHost,localPort):new UDTClient(myHost);
                judpClient client = new judpClient(localIP, localPort);
                client.Connect(serverHost, serverPort);
                Console.WriteLine("[ReceiveFile] Requesting file " + remoteFile);
                string reqFile = remoteFile;
                string rspFile = localFile;
                Thread recfile = new Thread(() =>
                {
                    byte[] fName = Encoding.UTF8.GetBytes(remoteFile);

                    //send file name info
                    byte[] nameinfo = new byte[fName.Length + 4];
                    Array.Copy(ApplicationCode.Encode(fName.Length), 0, nameinfo, 0, 4);
                    Array.Copy(fName, 0, nameinfo, 4, fName.Length);
                    client.SendData(nameinfo);
                    client.PauseOutput();
                    //read size info (an 64 bit number) 
                    byte[] sizeInfo = new byte[8];

                    int total = 0;
                    while (total < sizeInfo.Length)
                    {
                        int r = client.read(sizeInfo);
                        if (r < 0) break;
                        total += r;
                    }
                    long size = ApplicationCode.decode(sizeInfo, 0);

                    //  File file = new File(new string(rspFile));
                    Console.WriteLine("[ReceiveFile] Write to local file <" + rspFile + ">");
                    FileStream fos = null;
                    try
                    {
                        fos = new FileStream(rspFile, FileMode.Append);

                        Console.WriteLine("[ReceiveFile] Reading <" + size + "> bytes.");
                        long start = DateTime.Now.Ticks;
                        ApplicationCode.CopySocketFile(fos, client, size, false);
                        long end = DateTime.Now.Ticks;
                        double rate = 1000.0 * size / 1024 / 1024 / (end - start);
                        Console.WriteLine("[ReceiveFile] Rate: " + rate.ToString(format) + " MBytes/sec. "
                                + (8 * rate).ToString(format) + " MBit/sec.");
                        Console.WriteLine("�����ļ���ɣ�" + rspFile);
                        client.Close();

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        try
                        {
                            fos.Close();
                        }
                        catch (IOException e)
                        {

                            Console.WriteLine(e);
                        }
                    }
                });
                recfile.IsBackground = true;
                recfile.Name = ("�ļ�����_" + localFile);
                recfile.Start();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
    }
}
