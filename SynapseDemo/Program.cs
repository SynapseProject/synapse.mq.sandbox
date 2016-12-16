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
        static void Main(string[] args)
        {
            Mode mode = Mode.NONE;
            String identity = Guid.NewGuid().ToString();

            try
            {
                if (args != null && args.Length >= 1)
                    mode = (Mode)Enum.Parse(typeof(Mode), args[0]);

                if (mode == Mode.PROXY)
                {
                    String endpoint = @"tcp://*:5555";
                    String endpoint2 = @"tcp://*:5556";
                    if (args != null && args.Length >= 2)
                        endpoint = args[1];
                    if (args != null && args.Length >= 3)
                        endpoint2 = (args[2]);

                    StartProxy(endpoint, endpoint2);
                }
                else if (mode == Mode.API)
                {
                    String sendOn = @"tcp://localhost:5555";
                    String listenOn = @"tcp://localhost:5558";
                    if (args != null && args.Length >= 2)
                        sendOn = args[1];
                    if (args != null && args.Length >= 3)
                        listenOn = (args[2]);

                    StartApi(sendOn, listenOn);
                }
                else if (mode == Mode.HANDLER)
                {
                    String sendOn = @"tcp://localhost:5557";
                    String listenOn = @"tcp://localhost:5556";
                    if (args != null && args.Length >= 2)
                        sendOn = args[1];
                    if (args != null && args.Length >= 3)
                        listenOn = (args[2]);

                    StartHandler(sendOn, listenOn);
                }
                else if (mode == Mode.PROXIES)
                {
                    String apiInbound = @"tcp://*:5555";
                    String apiOutbound = @"tcp://*:5556";
                    String handlerInbound = @"tcp://*:5557";
                    String handlerOutbound = @"tcp://*:5558";

                    if (args != null && args.Length >= 2)
                        apiInbound = args[1];
                    if (args != null && args.Length >= 3)
                        apiOutbound = args[2];
                    if (args != null && args.Length >= 4)
                        handlerInbound = args[3];
                    if (args != null && args.Length >= 5)
                        handlerOutbound = args[4];

                    StartProxies(apiInbound, apiOutbound, handlerInbound, handlerOutbound);
                }
                else if (mode == Mode.CLIENT)
                {
                    String endpoint = @"tcp://localhost:5555";
                    if (args != null && args.Length >= 2)
                        endpoint = args[1];

                    StartClient(endpoint);
                }
                else if (mode == Mode.WORKER)
                {
                    String endpoint = @"tcp://localhost:5556";
                    if (args != null && args.Length >= 2)
                        endpoint = args[1];

                    StartWorker(endpoint);
                }
                else
                    Usage();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

        }

        static void Usage()
        {
            Console.WriteLine("USAGE : SynapseDemo.exe <mode>   <endpoint> [...]");
            Console.WriteLine("        SynapseDemo.exe API      <endpoint> <endpoint>");
            Console.WriteLine("        SynapseDemo.exe PROXIES  <endpoint> <endpoint> <endpoint> <endpoint>");
            Console.WriteLine("        SynapseDemo.exe HANDLER  <endpoint> <endpoint>");
            Console.WriteLine();
            Console.WriteLine("        SynapseDemo.exe CLIENT   <endpoint>");
            Console.WriteLine("        SynapseDemo.exe PROXY    <endpoint> <endpoint>");
            Console.WriteLine("        SynapseDemo.exe WORKER   <endpoint>");
        }


        static void StartProxy(string inboundEndpoint, string outboundEndpoint, ZContext ctx = null)
        {
            MQProxy proxy = new MQProxy(inboundEndpoint, outboundEndpoint, ctx);
            proxy.Start();
        }

        static void StartProxies(string apiInbound, string apiOutbound, string handlerInbound, string handlerOutbound)
        {
            MQProxy proxy1 = new MQProxy(apiInbound, apiOutbound);
            new Thread(() => proxy1.Start()).Start();

            MQProxy proxy2 = new MQProxy(handlerInbound, handlerOutbound);
            new Thread(() => proxy2.Start()).Start();

            while (true);
        }

        static void StartApi(string sendOn, string listenOn)
        {
            SynapseEndpoint Sender = new SynapseEndpoint(sendOn);
            Sender.Connect();

            SynapseEndpoint Receiver = new SynapseEndpoint(listenOn);
            Receiver.Connect();
            new Thread(() => Receiver.ReceiveMessages(ProcessApiRequest, false, Sender)).Start();

            while (true)
            {
                String message = Console.ReadLine();
                Sender.SendMessage(null, MessageType.REQUEST, message);
            }
        }

        static void StartHandler(String sendOn, String listenOn)
        {
            SynapseEndpoint Sender = new SynapseEndpoint(sendOn);
            Sender.Connect();

            SynapseEndpoint Receiver = new SynapseEndpoint(listenOn);
            Receiver.Connect();
            new Thread(() => Receiver.ReceiveMessages(ProcessHandlerRequest, false, Sender)).Start();

            while (true)
            {
                String input = Console.ReadLine();

                String[] strs = input.Split(',');
                if (strs[0] == "STATUS")
                    Sender.SendMessage(null, MessageType.STATUS, strs[1]);
                else if (strs[0] == "QUERY")
                    Sender.SendMessage(null, MessageType.REQUEST, strs[1]);
            }


        }

        static void StartClient(string endpoint)
        {
            SynapseEndpoint WorkRequest = new SynapseEndpoint(endpoint);
            WorkRequest.Connect();

            new Thread(() => WorkRequest.ReceiveReplies(ProcessMyReplies)).Start();

            while (true)
            {
                String message = Console.ReadLine();
                WorkRequest.SendMessage(null, MessageType.REQUEST, message);
            }
        }

        static void StartWorker(string endpoint)
        {
            SynapseEndpoint WorkReceiver = new SynapseEndpoint(endpoint);
            WorkReceiver.Connect();
            WorkReceiver.ReceiveMessages(ToUpperReplyToSender, true, WorkReceiver);

        }


        // Worker Functions
        static String ProcessHandlerRequest(String messageId, MessageType messageType, String message, SynapseEndpoint socket, String sendStatusTo)
        {
            if (socket != null && messageType == MessageType.REQUEST)
            {
                socket.SendMessage(messageId, MessageType.ACK, "");
                Console.WriteLine(">>> [" + messageId + "]  Request Received : " + message);
                Thread.Sleep(3000);
                socket.SendMessage(messageId, MessageType.REPLY, message.ToUpper());
            }

            return null;
        }

        static String ProcessApiRequest(String messageId, MessageType messageType, String message, SynapseEndpoint socket, String sendStatusTo)
        {
            if (messageType == MessageType.REQUEST)
                return ProcessPlanStatusRequest(messageId, messageType, message, socket, sendStatusTo);
            else if (messageType == MessageType.STATUS)
                return ProcessStatus(messageId, messageType, message, socket, sendStatusTo);
            else
                return null;
        }

        static String ProcessStatus(String messageId, MessageType messageType, String message, SynapseEndpoint socket, String sendStatusTo)
        {
            if (messageType == MessageType.STATUS)
            {
                socket.SendMessage(messageId, MessageType.ACK, "");
                Console.WriteLine(">>> [" + messageId + "]  Status Received : " + message);
            }

            return null;
        }

        static String ProcessPlanStatusRequest(String messageId, MessageType messageType, String message, SynapseEndpoint socket, String sendStatusTo)
        {
            if (messageType == MessageType.REQUEST)
            {
                socket.SendMessage(messageId, MessageType.ACK, "");
                Console.WriteLine(">>> [" + messageId + "] Plan Status Request Received : " + message);
                Thread.Sleep(3000);

                Random rnd = new Random();
                int i = rnd.Next(1, 3);

                String reply = (i==1?"RUNNING":"CANCELLED");
                socket.SendMessage(messageId, MessageType.REPLY, reply);
            }

            return null;
        }


        static String ToUpperReplyToSender(String messageId, MessageType messageType, String message, SynapseEndpoint socket, String sendStatusTo)
        {
            Thread.Sleep(3000);
            if (socket != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    socket.SendMessage(messageId, MessageType.STATUS, "Status Report #" + (i + 1), sendStatusTo);
                    Thread.Sleep(3000);
                }
            }

            return message.ToUpper();
        }

        static String ProcessMyReplies(String messageId, MessageType messageType, String message, SynapseEndpoint socket, String sendStatusTo)
        {
            Console.WriteLine(">>> [" + messageId + "][" + messageType + "] " + message);
            if (messageType == MessageType.REPLY)
                Console.WriteLine("*** Reply Processed : Length = " + message.Length);
            return null;
        }



    }
}
