using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZeroMQ;

namespace Encryption
{
    public class Strawhouse
    {
        public static void Start()
        {
            ZContext ctx = new ZContext();

            ZSocket server = new ZSocket(ZSocketType.PUSH);
            server.AddTcpAcceptFilter("127.0.0.1");
            server.Bind("tcp://*:9000");

            ZSocket client = new ZSocket(ZSocketType.PULL);
            client.Connect("tcp://127.0.0.1:9000");

            server.Send(new ZFrame("Hello"));
            ZMessage message = client.ReceiveMessage();
            Console.WriteLine("Strawhouse : " + message[0].ReadString());
        }
    }
}
