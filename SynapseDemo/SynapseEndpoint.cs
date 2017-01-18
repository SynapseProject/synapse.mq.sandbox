using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ZeroMQ;

namespace SynapseDemo
{
    public enum Mode { NONE, PROXY, CONTROLLER, NODE, PROXIES, CLIENT, WORKER };
    public enum MessageType { NONE, REQUEST, REPLY, STATUS, ACK };

    public class SynapseEndpoint
    {
        public ZContext Context { get; }
        public ZSocket Socket { get; }
        public ZSocketType SocketType { get; }
        public String Identity { get; }
        public String Endpoint { get; }

        public SynapseEndpoint(String endpoint, String identity = null, ZContext context = null, ZSocketType socketType = ZSocketType.DEALER)
        {
            Endpoint = endpoint;
            Context = context;
            SocketType = socketType;
            if (Context == null)
                Context = new ZContext();
            Socket = new ZSocket(Context, SocketType);
            Identity = identity;
            if (Identity == null)
                Identity = Guid.NewGuid().ToString();

            Socket.Identity = Encoding.UTF8.GetBytes(Identity);
        }

        public void Bind()
        {
            Socket.Bind(Endpoint);
            Console.WriteLine(SocketType + " Socket " + Identity + " Bound To " + Endpoint);
        }

        public void Unbind()
        {
            Socket.Unbind(Endpoint);
        }

        public void Connect()
        {
            Socket.Connect(Endpoint);
            Console.WriteLine(SocketType + " Socket " + Identity + " Connected To " + Endpoint);
        }

        public void Disconnect()
        {
            Socket.Disconnect(Endpoint);
        }

        public void SendMessage(String messageId, MessageType messageType, String message, String replyTo = null)
        {
            ZError error;
            String id = messageId;
            if (String.IsNullOrWhiteSpace(id))
                id = Guid.NewGuid().ToString();
            using (ZMessage outgoing = new ZMessage())
            {
                if (replyTo == null)
                    outgoing.Add(new ZFrame(Socket.Identity));
                else
                    outgoing.Add(new ZFrame(Encoding.UTF8.GetBytes(replyTo)));
                outgoing.Add(new ZFrame(id));
                outgoing.Add(new ZFrame(messageType.ToString()));
                outgoing.Add(new ZFrame(message));
                Console.WriteLine("<<< [" + id + "][" + messageType + "] " + message);
                if (!Socket.Send(outgoing, out error))
                {
                    if (error == ZError.ETERM)
                        return;
                    throw new ZException(error);
                }

            }
        }

        public void ReceiveMessages(Func<String, MessageType, String, SynapseEndpoint, String, String> callback, Boolean sendAck = true, SynapseEndpoint replyOn = null)
        {
            ZError error;
            ZMessage request;
            SynapseEndpoint replyUsing = this;

            if (replyOn != null)
                replyUsing = replyOn;

            while (true)
            {
                if (null == (request = Socket.ReceiveMessage(out error)))
                {
                    if (error == ZError.ETERM)
                        return;
                    throw new ZException(error);
                }

                using (request)
                {
                    string identity = request[1].ReadString();
                    String messageId = request[2].ReadString();
                    MessageType messageType = (MessageType)Enum.Parse(typeof(MessageType), request[3].ReadString());
                    string message = request[4].ReadString();

                    //TODO : Debug - Remove Me
                    Console.WriteLine(">>> [" + messageId + "][" + messageType + "] " + message);

                    if (sendAck)
                        replyUsing.SendMessage(messageId, MessageType.ACK, String.Empty, identity);

                    if (callback != null)
                    {
                        String reply = callback(messageId, messageType, message, replyOn, identity);
                        if (!(String.IsNullOrWhiteSpace(reply)))
                            replyUsing.SendMessage(messageId, MessageType.REPLY, reply, identity);
                    }
                }
            }
        }



        public void ReceiveReplies(Func<String, MessageType, String, SynapseEndpoint, String, String> callback)
        {
            ZError error;
            ZMessage incoming;
            ZPollItem poll = ZPollItem.CreateReceiver();

            while (true)
            {
                if (!Socket.PollIn(poll, out incoming, out error, TimeSpan.FromMilliseconds(10)))
                {
                    if (error == ZError.EAGAIN)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    if (error == ZError.ETERM)
                        return;
                    throw new ZException(error);
                }
                using (incoming)
                {
                    String messageId = incoming[0].ReadString();
                    MessageType messageType = (MessageType)Enum.Parse(typeof(MessageType), incoming[1].ReadString());
                    String messageText = incoming[2].ReadString();

                    if (callback != null)
                        callback(messageId, messageType, messageText, this, null);
                }

            }
        }
    }
}
