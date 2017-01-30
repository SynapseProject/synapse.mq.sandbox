using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZeroMQ;

namespace Encryption
{
    public class Woodhouse
    {
        public static void Start()
        {
            ZContext ctx = new ZContext();

            String[] parms = { "VERBOSE" };

            ZSocket server = new ZSocket(ctx, ZSocketType.PUSH);
            server.AddTcpAcceptFilter("127.0.0.1");
            server.PlainServer = true;
            server.Bind("tcp://*:9000");

            ZSocket client = new ZSocket(ctx, ZSocketType.PULL);
            client.PlainUserName = "admin";
            client.PlainPassword = "secret";
            client.Connect("tcp://127.0.0.1:9000");

            server.Send(new ZFrame("Hello"));

            ZError error;
            ZMessage message = client.ReceiveMessage(out error);

            Console.WriteLine(">> " + message[0].ReadString());
        }
    }
}
