using System.Net.Sockets;
using Game;

namespace Vyachka.Chat.Server
{
    public class ClientData
    {
        public Socket ClientSocket { get; set; }
        public TicTacToe.Players Name { get; set; }

        public ClientData(Socket socket, TicTacToe.Players name)
        {
            ClientSocket = socket;
            Name = name;
        }
    }
}
