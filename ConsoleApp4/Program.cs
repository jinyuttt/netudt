using netudt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp4
{
    class Program
    {
        static void Main(string[] args)
        {
            UDTClient client = new UDTClient();
            client.Connect("127.0.0.1", 7777);
            while (true)
            {

                client.send(System.Text.Encoding.Default.GetBytes(DateTime.Now.ToString()));
                Console.ReadLine();
            }
        }
    }
}
