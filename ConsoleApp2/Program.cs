using netudt;
using System;
using System.Text;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            UDTServerSocket socket = new UDTServerSocket(7777, "127.0.0.1");
            UDTSocket dTSocket=  socket.Accept();
            byte[] buffer = new byte[1470];
            while (!dTSocket.IsClose())
            {
                int r=dTSocket.GetInputStream().Read(buffer);
                Console.WriteLine(Encoding.Default.GetString(buffer,0,r));
            }
        }
    }
}
