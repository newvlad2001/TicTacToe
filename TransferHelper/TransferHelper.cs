using System;
using System.Net.Sockets;
using System.Text;

namespace Helper
{
    public static class TransferHelper
    {
        public static void Send(string msg, Socket socket)
        {
            byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
            byte[] msgLengthBytes = BitConverter.GetBytes(msg.Length);
            try
            {
                socket.Send(msgLengthBytes);
                socket.Send(msgBytes);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        public static string Receive(Socket socket)
        {
            string dataStr;
            try
            {
                byte[] lengthBytes = new byte[4];
                int bytesRead = socket.Receive(lengthBytes);
                if (bytesRead == 0)
                {
                    return null;
                }

                int length = BitConverter.ToInt32(lengthBytes);
                byte[] data = new byte[length];
                bytesRead = socket.Receive(data);
                if (bytesRead == 0)
                {
                    return null;
                }

                dataStr = Encoding.UTF8.GetString(data);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }

            return dataStr;
        }
    }
}