using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GraphicClient
{
    class Client
    {
        Socket socket;

        public Client(string ipAddr, string port)
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ipAddr), int.Parse(port));
            socket = new Socket(remoteEP.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(remoteEP);
        }

        public string Receive()
        {
            byte[] bytes = new byte[1024];

            int bytesRec = socket.Receive(bytes);
            return Encoding.ASCII.GetString(bytes, 0, bytesRec);
        }
    }
}
