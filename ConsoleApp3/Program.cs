using netudt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    class Program
    {
        static void Main(string[] args)
        {
            UDTServerSocket socket = new UDTServerSocket(7777, "127.0.0.1");
            UDTSocket dTSocket = socket.Accept();
            byte[] buffer = new byte[1470];
            while (!dTSocket.IsClose())
            {
                int r = dTSocket.GetInputStream().Read(buffer);
                if(r>0)
                Console.WriteLine(Encoding.Default.GetString(buffer, 0, r));
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
