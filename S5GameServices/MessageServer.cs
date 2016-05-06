using S5GameServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace S5GameServer
{
    public class MessageServer
    {
        public int TimeoutMS;
        public int Port;
    }

    public class MessageServer<T> : MessageServer where T : ClientHandler
    {
        internal Dictionary<MessageCode, MessageHandler<T>> MessageHandlers;
        internal Dictionary<LobbyMessageCode, MessageHandler<T>> LobbyHandlers;

        public void Run()
        {
            MessageHandlers = new Dictionary<MessageCode, MessageHandler<T>>();
            LobbyHandlers = new Dictionary<LobbyMessageCode, MessageHandler<T>>();

            foreach (var methodInfo in typeof(T).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                var handlerAttrib = methodInfo.GetCustomAttribute<Handler>();
                if (handlerAttrib == null)
                    continue;

                var handlerDelegate = (MessageHandler<T>)Delegate.CreateDelegate(typeof(MessageHandler<T>), null, methodInfo);

                if (handlerAttrib.IsLobbyHandler)
                    LobbyHandlers.Add(handlerAttrib.LobbyCode, handlerDelegate);
                else
                    MessageHandlers.Add(handlerAttrib.Code, handlerDelegate);
            }

            Task.Run(() =>
            {
                var ipep = new IPEndPoint(IPAddress.Any, Port);
                var tcp = new TcpListener(ipep);
                tcp.Start();

                for (;;)
                {
                    var client = tcp.AcceptTcpClient();
                    Console.WriteLine("new client " + client.Client.RemoteEndPoint.ToString());
                    var chandler = Activator.CreateInstance<T>();
                    var conn = new ClientConnection<T>(this, chandler);
                    ThreadPool.QueueUserWorkItem(conn.HandleClient, client);
                }
            });
        }

    }

    [AttributeUsage(AttributeTargets.Method)]
    public class Handler : Attribute
    {
        public bool IsLobbyHandler;
        public MessageCode Code;
        public LobbyMessageCode LobbyCode;

        public Handler(MessageCode code)
        {
            Code = code;
            IsLobbyHandler = false;
        }

        public Handler(LobbyMessageCode lobbyCode)
        {
            LobbyCode = lobbyCode;
            IsLobbyHandler = true;
        }
    }

    public class ClientHandler
    {
        public ClientConnection Connection { get; internal set; }

        public virtual void Disconnect() { }
    }

    public delegate void MessageHandler<T>(T @this, Message msg);

    public abstract class ClientConnection
    {
        byte[] buffer = new byte[1024];
        TcpClient tcpConn;
        Socket socket;
        NetworkStream stream;
        RsaKeyExchange keyEx = new RsaKeyExchange();

        public string IP { get { return (tcpConn.Client.RemoteEndPoint as IPEndPoint).Address.ToString(); } }
        public Blowfish BlowfishContext = null;

        protected abstract void CallMessageHandler(Message msg);
        protected abstract MessageServer Server { get; }
        protected abstract ClientHandler ClientHandler { get; }

        protected void LogSocketError(SocketError se)
        {
            Console.WriteLine("Socket Error: " + se.ToString());
        }

        public void HandleClient(object state)
        {
            tcpConn = state as TcpClient;
            socket = tcpConn.Client;
            stream = tcpConn.GetStream();

            while (tcpConn.Client.Connected)
            {
                SocketError se;
                var recvResult = socket.BeginReceive(buffer, 0, 6, SocketFlags.None, out se, null, null);
                if (se != SocketError.Success) { LogSocketError(se); break; }

                if (!recvResult.AsyncWaitHandle.WaitOne(Server.TimeoutMS, false)) { Console.WriteLine("timeout header"); break; }

                if (socket.EndReceive(recvResult, out se) != 6) { Console.WriteLine("header len != 6"); break; }

                if (se != SocketError.Success) { LogSocketError(se); break; }

                int msgSize = buffer[2] + 256 * buffer[1] + 256 * 256 * buffer[0];
                if (msgSize > 6)
                {
                    recvResult = socket.BeginReceive(buffer, 6, msgSize - 6, SocketFlags.None, out se, null, null);
                    if (se != SocketError.Success) { LogSocketError(se); break; }

                    if (!recvResult.AsyncWaitHandle.WaitOne(Server.TimeoutMS, false)) { Console.WriteLine("timeout body"); break; }

                    if (socket.EndReceive(recvResult, out se) != msgSize - 6) { Console.WriteLine("body len invalid"); break; }

                    if (se != SocketError.Success) { LogSocketError(se); break; }
                }
                else if (msgSize < 6)
                {
                    Console.WriteLine("Message len = {0} WAT", msgSize);
                    break;
                }

                CallMessageHandler(Message.ParseIncoming(buffer, BlowfishContext).First());
            }
            Console.WriteLine("close client");
            ClientHandler.Disconnect();
            tcpConn.Close();
        }

        protected void HandleKeyExchange(Message msg)
        {
            keyEx.ReadMessage(msg);
            var resp = keyEx.GetBfKeyResponse();
            BlowfishContext = new Blowfish(keyEx.Key);
            Send(resp);
        }

        public void Send(Message msg)
        {
            if (stream.CanWrite)
            {
                if (msg.Code != MessageCode.RSAEXCHANGE && msg.Code != MessageCode.STILLALIVE)
                    Console.WriteLine("OUT: " + msg.ToString());

                var data = msg.Serialize();
                stream.Write(data, 0, data.Length);
            }
            else
                Console.WriteLine("stream closed, cant respond!");
        }
    }

    public class ClientConnection<T> : ClientConnection where T : ClientHandler
    {
        MessageServer<T> server;
        T clientHandler;

        public ClientConnection(MessageServer<T> serv, T cHandler)
        {
            server = serv;
            clientHandler = cHandler;
            clientHandler.Connection = this;
        }

        protected override MessageServer Server { get { return server; } }
        protected override ClientHandler ClientHandler { get { return clientHandler; } }


        static Message StillAlive = new Message(MessageType.GSMessage, MessageCode.STILLALIVE, null, 3, 8);
        protected override void CallMessageHandler(Message msg)
        {
            if (msg.Code != MessageCode.RSAEXCHANGE && msg.Code != MessageCode.STILLALIVE)
                Console.WriteLine(" IN: " + msg.ToString());

            if (msg.Code == MessageCode.RSAEXCHANGE)
            {
                HandleKeyExchange(msg);
                return;
            }
            else if (msg.Code == MessageCode.STILLALIVE)
            {
                Send(StillAlive);
                return;
            }

            if (msg.Code == MessageCode.LOBBY_MSG)
            {
                MessageHandler<T> handler;
                var lobbyCode = (LobbyMessageCode)msg.Data[0].AsInt;
                if (server.LobbyHandlers.TryGetValue(lobbyCode, out handler))
                    handler(clientHandler, msg);
                else
                    Console.WriteLine("Unhandled LobbyMessage [{0}] in {1}!", lobbyCode.ToString(), (typeof(T)).Name);
            }
            else
            {
                MessageHandler<T> handler;
                if (server.MessageHandlers.TryGetValue(msg.Code, out handler))
                    handler(clientHandler, msg);
                else
                    Console.WriteLine("Unhandled Message [{0}] in {1}!", msg.Code.ToString(), (typeof(T)).Name);
            }
        }
    }
}