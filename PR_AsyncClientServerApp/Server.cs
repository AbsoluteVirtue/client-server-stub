using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace PR_AsyncClientServerApp
{
    class Server
    {
        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static List<Socket> _clientSockets = new List<Socket>();
        private static byte[] _buffer = new byte[1024];

        static SynchronizedLog sl = new SynchronizedLog();
        static String workDir = Directory.GetCurrentDirectory();
        static String filePath = workDir + "\\log.txt";

        private enum Commands 
        { 
            none,
            echo,
            time,
            invert,
            log,
            help
        };

        static void Main(string[] args)
        {
            Console.Title = "Server, ver. 0.4";
            using (StreamWriter file = new StreamWriter(filePath, true))
            {
                file.WriteLine(DateTime.Now + ": Server started.");
            }
            SetupServer();
            Console.ReadKey();
        }

        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            _serverSocket.Bind(new IPEndPoint(0, 100));//IPAddress.Any
            _serverSocket.Listen(5);//backlog = 5
            sl.Write(filePath, String.Format("{0}: Server socket {1} bound({2}).", DateTime.Now.ToString(), _serverSocket.SocketType, _serverSocket.IsBound));
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            Socket socket = _serverSocket.EndAccept(ar);
            _clientSockets.Add(socket);
            Console.WriteLine("Client connected...");
            sl.Write(filePath, String.Format("{0}: Client {1} connected.", DateTime.Now.ToString(), socket.Handle));
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;//current client socket
            int received = socket.EndReceive(ar);
            byte[] dataBuffer = new byte[received];
            Array.Copy(_buffer, dataBuffer, received);

            String text = Encoding.ASCII.GetString(dataBuffer);
            sl.Write(filePath, String.Format("{0}: Message received: {1}.", DateTime.Now.ToString(), text));
            Console.WriteLine("Received: {0}", text);
            String response = String.Empty;
            List<String> arguments = new List<string>();

            Match m;
            String inputString = text;
            String parsedCommand = String.Empty;
            String pattern = @"(?<op>[\w]{3,})(\s+(?<arg>[\w]+))*";
            try
            {
                m = Regex.Match(inputString, pattern,
                      RegexOptions.IgnoreCase | RegexOptions.Compiled,
                      TimeSpan.FromSeconds(1));
                while (m.Success)
                {
                    parsedCommand = m.Groups["op"].Value;
                    sl.Write(filePath, String.Format("{0}: Request: {1}.", DateTime.Now.ToString(), parsedCommand.ToUpper()));
                    Console.WriteLine("Command: " + parsedCommand);
                    Console.WriteLine("Catch group: " + m.Groups["arg"].Value);

                    foreach (Capture cap in m.Groups["arg"].Captures)
                    {
                        sl.Write(filePath, String.Format("{0}: Argument[{1}]: {2}.", DateTime.Now.ToString(), cap.Index, cap.Value));
                        Console.WriteLine("Capture: " + cap.Value);
                        arguments.Add(cap.Value);
                    }

                    m = m.NextMatch();
                }   
            }
            catch (RegexMatchTimeoutException)
            {
                Console.WriteLine("The matching operation timed out.");
                sl.Write(filePath, String.Format("{0}: The matching operation timed out.", DateTime.Now.ToString()));
            }

            //check if parsedCommand isn't empty
            Commands command;
            try
            {
                command = (Commands)Enum.Parse(typeof(Commands), parsedCommand.ToLower());
            }
            catch (Exception ex)
            {
                sl.Write(filePath, String.Format("{0}: ERROR: {1}", DateTime.Now.ToString(), ex.Message));
                Console.WriteLine(ex.Message);
                command = Commands.none;
            }

            switch (command)
            {
                case Commands.time:
                    {
                        response = DateTime.Now.ToString();
                    } break;
                case Commands.echo: 
                    {
                        response = text.Substring(parsedCommand.Length + 1);
                        sl.Write(filePath, String.Format("{0}: Reply: {1}", DateTime.Now.ToString(), response));
                    } break;
                case Commands.log:
                    {
                        response = sl.Read(filePath);
                    } break;
                case Commands.invert:
                    {
                        String stub = text.Substring(parsedCommand.Length + 1);
                        for (int i = stub.Length - 1; i != -1; --i)
                        {
                            response += stub[i];
                        }
                        sl.Write(filePath, String.Format("{0}: Reply: {1}", DateTime.Now.ToString(), response));
                    } break;
                case Commands.help:
                    {
                        response = @"
ECHO <message> - echoes received message
INVERT <message> - inverts received message
LOG - displays server log file
TIME - displays server local time
";
                    } break;
                case Commands.none: 
                default:
                    {
                        response = "Error: unknown command.";
                        sl.Write(filePath, String.Format("{0}: Reply: {1}", DateTime.Now.ToString(), response));
                    } break;
            }


            byte[] data = Encoding.ASCII.GetBytes(response);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndSend(ar);
        }

        //private static void SendText(String text)
        //{
        //    byte[] data = Encoding.ASCII.GetBytes(text);
        //    socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
        //}

    }
}
