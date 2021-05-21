using System.Net.Sockets;
using Game;

namespace GameServer
{
    public class ClientData
    {
        public Socket ClientSocket { get; set; }
        public Game.TicTacToe.Players Name { get; set; }

        public ClientData(Socket socket, Game.TicTacToe.Players name)
        {
            ClientSocket = socket;
            Name = name;
        }
    }
}
