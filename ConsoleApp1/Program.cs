using System;
using netudt;

namespace ConsoleApp1
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
