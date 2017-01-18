using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ZeroMQ;

namespace SynapseDemo
{
    class Program
    {
        public enum Mode { NONE, PUBLISHER, SUBSCRIBER };
        public enum MessageType { NONE, REQUEST, REPLY, STATUS, ACK };

        static void Main(string[] args)
        {
            Mode mode = Mode.NONE;
            String endpoint = String.Empty;

            if (args != null && args.Length >= 1)
                mode = (Mode)Enum.Parse(typeof(Mode), args[0]);

            if (mode == Mode.PUBLISHER)
            {
                endpoint = @"tcp://*:5555";
                if (args != null && args.Length >= 2)
                    endpoint = args[1];
            }
            else if (mode == Mode.SUBSCRIBER)
            {
                endpoint = @"tcp://localhost:5555";
                if (args != null && args.Length >= 2)
                    endpoint = args[1];
            }

            try
            {
                if (mode == Mode.PUBLISHER)
                    StartPublisher(endpoint);
                else if (mode == Mode.SUBSCRIBER)
                    StartSubscriber(endpoint);
                else
                    Usage();
            }
            catch (Exception e)
            {
//                String msg = String.Format("Failed to {0} to {1} as a {2}.", (bind ? "Bind" : "Connect"), endpoint, mode);
//                Console.WriteLine(msg);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

        }

        static void Usage()
        {
            Console.WriteLine("USAGE : SynapseDemo.exe <mode>   <endpoint>");
            Console.WriteLine("        SynapseDemo.exe SUBSCRIBER <endpoint>");
            Console.WriteLine("        SynapseDemo.exe PUBLISHER  <endpoint>");
        }

        static void StartSubscriber(String endpoint)
        {
            using (ZContext context = new ZContext())
            using (ZSocket receiver = new ZSocket(context, ZSocketType.SUB))
            {
                receiver.SubscribeAll();
                receiver.Connect(endpoint);
                Console.WriteLine("Subscriber Connected To " + endpoint);

                while (true)
                {
                    using (ZMessage request = receiver.ReceiveMessage())
                    {
                        String req = request[0].ReadString();
                        Console.WriteLine(">>> " + req);
                    }
                }
            }
        }

        static void StartPublisher(String endpoint, bool bind = false)
        {
            using (ZContext context = new ZContext())
            using (ZSocket sender = new ZSocket(context, ZSocketType.PUB))
            {
                sender.Bind(endpoint);
                Console.WriteLine("Publisher Bound To " + endpoint);

                Console.Write("<<< ");
                String msg = Console.ReadLine();

                while (true)
                {
                    using (ZMessage message = new ZMessage())
                    {
                        message.Add(new ZFrame(msg));
                        sender.Send(message);
                    }

                    Console.Write("<<< ");
                    msg = Console.ReadLine();
                }
            }
        }
    }
}
