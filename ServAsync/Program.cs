using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ServAsync
{
    class Program
    {
        static void Main(string[] args)
        {
            Server asyncSrv = new Server();
            asyncSrv.SetupServer();
            Console.ReadKey();
        }
    }
}
