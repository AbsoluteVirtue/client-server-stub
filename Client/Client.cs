using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Client
{
    class Client
    {
        private static Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        static void Main(string[] args)
        {
            Console.Title = "Client, ver. 0.3";

            LoopConnect();
            LoopSend();
            //Console.ReadKey();
        }

        private static void LoopSend()
        {
            while (true)
            {
                Console.Write("$");
                String input = Console.ReadLine();
                if (input == "cls")
                {
                    Console.Clear();
                    Console.Write("$");
                    input = Console.ReadLine();
                }
                if (input == "quit")
                {
                    return;
                }
                byte[] data = Encoding.ASCII.GetBytes(input);
                _clientSocket.Send(data);

                byte[] buffer = new byte[1024];
                int inc = _clientSocket.Receive(buffer);
                byte[] incBuffer = new byte[inc];
                Array.Copy(buffer, incBuffer, inc);

                String text = Encoding.ASCII.GetString(incBuffer);
                Console.WriteLine(text);
            }
        }

        private static void LoopConnect()
        {
            int attempts = 0;

            while (!_clientSocket.Connected)
            {
                try
                {
                    attempts++;
                    _clientSocket.Connect(IPAddress.Loopback, 100);
                }
                catch (SocketException se)
                {
                    Console.Clear();
                    Console.WriteLine("Connetcion attempts: {0}", attempts);
                }
            }

            Console.Clear();
            Console.WriteLine("Connected...");
        }
    }
}
