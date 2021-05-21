using Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Game;

namespace GameServer
{
    public static class Server
    {
        private static List<ClientData> _clients = new List<ClientData>();
        private static Game.TicTacToe _game;
        private static bool _isXConnected = false;
        private static bool _isOConnected = false;
        
        static void CommunicateWithClient(object objData)
        {
            ClientData clientData = (ClientData) objData;

            string input;
            while ((input = TransferHelper.Receive(clientData.ClientSocket)) != null)
            {
                Console.WriteLine(input);
                string[] splittedData = input.Split('/');
                if (splittedData[0] == "button")
                {
                    _game.Move((int) Char.GetNumericValue(splittedData[1][1]),
                        (int) Char.GetNumericValue(splittedData[1][2]), clientData.Name == Game.TicTacToe.Players.X);
                }
                else if (splittedData[0] == "restart")
                {
                    SendAll("restart");
                    if (_isOConnected && _isXConnected)
                        SendAll("start");
                    TransferHelper.Send("turn/false", clientData.ClientSocket);
                    _game = new Game.TicTacToe();
                }

                foreach (ClientData client in _clients)
                {
                    TransferHelper.Send(input, client.ClientSocket);
                    if (clientData != client)
                    {
                        TransferHelper.Send("turn/true", client.ClientSocket);
                    }

                    if (_game.IsWinner(clientData.Name == Game.TicTacToe.Players.X))
                    {
                        TransferHelper.Send("win/" + clientData.Name + " wins the game", client.ClientSocket);
                    }
                    else if (_game.IsDraw())
                    {
                        TransferHelper.Send("draw", client.ClientSocket);
                    }
                }
            }

            clientData.ClientSocket.Shutdown(SocketShutdown.Both);
            clientData.ClientSocket.Close();
            _clients.Remove(clientData);
            Console.WriteLine($"{clientData.Name} disconnected.");
            
            SendAll("disconnected");
            if (clientData.Name == Game.TicTacToe.Players.X)
            {
                _isXConnected = false;
            }
            else
            {
                _isOConnected = false;
            }
        }

        private static string GetData(string type)
        {
            string value = "";
            bool isValid = false;
            while (!isValid)
            {
                Console.Write($"Please, enter {type}: ");
                value = Console.ReadLine();
                switch (type)
                {
                    case "port":
                        isValid = int.TryParse(value, out _);
                        break;
                }

                if (!isValid)
                {
                    Console.WriteLine($"Error with {type} value!");
                }
            }

            return value;
        }

        private static void SendAll(string msg)
        {
            foreach (ClientData client in _clients)
            {
                TransferHelper.Send(msg, client.ClientSocket);
            }
        }
        
        static void Main(string[] args)
        {
            string hostName = Dns.GetHostName();
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");

            foreach (var ip in Dns.GetHostEntry(hostName).AddressList)
            {
                if (!ip.ToString().Contains(":") && !ip.ToString().Equals("127.0.0.1"))
                {
                    ipAddress = ip;
                }
            }

            int port = int.Parse(GetData("port"));
            Console.WriteLine($"IP address: {ipAddress}");

            IPEndPoint ipPoint = new IPEndPoint(ipAddress, port);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipPoint);
            socket.Listen(10);

            while (true)
            {
                if (!_isXConnected || !_isOConnected)
                {
                    Socket clientSocket = socket.Accept();
                    if (clientSocket != null)
                    {
                        ClientData data = null;
                        if (!_isXConnected)
                        {
                            _isXConnected = true;
                            data = new ClientData(clientSocket,
                                Game.TicTacToe.Players.X);
                            TransferHelper.Send("playerSign/" + data.Name, clientSocket);
                        }
                        else
                        {
                            _isOConnected = true;
                            data = new ClientData(clientSocket,
                                Game.TicTacToe.Players.O);
                            TransferHelper.Send("playerSign/" + data.Name, clientSocket);
                        }
                        
                        foreach (ClientData client in _clients)
                        {
                            if (client != data)
                                TransferHelper.Send("turn/true", client.ClientSocket);
                            else 
                                TransferHelper.Send("turn/false", client.ClientSocket);
                        }
                        
                        Console.WriteLine($"{data.Name} connected");
                        _clients.Add(data);

                        Thread thread = new Thread(CommunicateWithClient);
                        thread.Start(data);

                        if (_isOConnected && _isXConnected)
                        {
                            SendAll("start");
                            _game = new Game.TicTacToe();
                        }
                    }
                }
            }
        }

    }
}