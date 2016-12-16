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
        public enum Mode { NONE, SENDER, RECEIVER };
        public enum MessageType { NONE, REQUEST, REPLY, STATUS, ACK };

        static void Main(string[] args)
        {
            Mode mode = Mode.NONE;
            String endpoint = String.Empty;
            bool bind = true;

            if (args != null && args.Length >= 1)
                mode = (Mode)Enum.Parse(typeof(Mode), args[0]);

            if (mode == Mode.SENDER)
            {
                endpoint = @"tcp://localhost:5555";
                bind = false;
                if (args != null && args.Length >= 2)
                    endpoint = args[1];
                if (args != null && args.Length >= 3)
                    bind = Boolean.Parse(args[2]);
            }
            else if (mode == Mode.RECEIVER)
            {
                endpoint = @"tcp://*:5555";
                bind = true;
                if (args != null && args.Length >= 2)
                    endpoint = args[1];
                if (args != null && args.Length >= 3)
                    bind = Boolean.Parse(args[2]);
            }

            try
            {
                if (mode == Mode.RECEIVER)
                    StartReceiver(endpoint, bind);
                else if (mode == Mode.SENDER)
                    StartSender(endpoint, bind);
                else
                    Usage();
            }
            catch (Exception e)
            {
                String msg = String.Format("Failed to {0} to {1} as a {2}.", (bind ? "Bind" : "Connect"), endpoint, mode);
                Console.WriteLine(msg);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

        }

        static void Usage()
        {
            Console.WriteLine("USAGE : SynapseDemo.exe <mode>   <endpoint> [...]");
            Console.WriteLine("        SynapseDemo.exe RECEIVER <endpoint> <bind>");
            Console.WriteLine("        SynapseDemo.exe SENDER   <endpoint> <bind>");
        }

        static void StartReceiver(String endpoint, bool bind = true)
        {
            using (ZContext context = new ZContext())
            using (ZSocket receiver = new ZSocket(context, ZSocketType.REP))
            {
                if (bind)
                {
                    receiver.Bind(endpoint);
                    Console.WriteLine("Receiver Bound To " + endpoint);
                }
                else
                {
                    receiver.Connect(endpoint);
                    Console.WriteLine("Receiver Connected To " + endpoint);
                }

                while (true)
                {
                    using (ZFrame request = receiver.ReceiveFrame())
                    {
                        String req = request.ReadString();
                        Console.WriteLine(">>> " + req);
                        Thread.Sleep(10000);
                        receiver.Send(new ZFrame(req.ToUpper()));
                        Console.WriteLine("<<< " + req.ToUpper());
                    }
                }
            }
        }

        static void StartSender(String endpoint, bool bind = false)
        {
            using (ZContext context = new ZContext())
            using (ZSocket sender = new ZSocket(context, ZSocketType.REQ))
            {
                if (bind)
                {
                    sender.Bind(endpoint);
                    Console.WriteLine("Sender Bound To " + endpoint);
                }
                else
                {
                    sender.Connect(endpoint);
                    Console.WriteLine("Sender Connected To " + endpoint);
                }

                Console.Write("<<< ");
                String msg = Console.ReadLine();

                while (true)
                {
                    sender.Send(new ZFrame(msg.Trim()));

                    using (ZFrame reply = sender.ReceiveFrame())
                    {
                        Console.WriteLine(">>> " + reply.ReadString());
                    }

                    Console.Write("<<< ");
                    msg = Console.ReadLine();
                }
            }
        }
    }
}
