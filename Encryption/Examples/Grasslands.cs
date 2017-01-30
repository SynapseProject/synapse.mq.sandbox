using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZeroMQ;

namespace Encryption
{
    public class Grasslands
    {
        public static void Start()
        {
            ZContext ctx = new ZContext();

            ZSocket server = new ZSocket(ZSocketType.PUSH);
            server.Bind("tcp://*:9000");

            ZSocket client = new ZSocket(ZSocketType.PULL);
            client.Connect("tcp://127.0.0.1:9000");

            server.Send(new ZFrame("Hello"));
            ZMessage message = client.ReceiveMessage();
            Console.WriteLine("Grasslands : " + message[0].ReadString());
        }
    }
}
