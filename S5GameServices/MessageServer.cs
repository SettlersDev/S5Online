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

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, Port));
            listener.Listen(10);
            listener.BeginAccept(NewClient, listener);
        }

        void NewClient(IAsyncResult ar)
        {
            var listener = ar.AsyncState as Socket;
            var clientSocket = listener.EndAccept(ar);
            listener.BeginAccept(NewClient, listener); //accept next client

            clientSocket.ReceiveTimeout = TimeoutMS;
            clientSocket.SendTimeout = TimeoutMS;
            var clientHandler = Activator.CreateInstance<T>();
            var conn = new ClientConnection<T>(this, clientHandler, clientSocket);
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
        protected byte[] buffer = new byte[1024];
        protected Socket socket;
        protected SocketError sockErr;
        protected bool isDisconnected = false;
        protected string endPointDbg, connTypeDbg;

        protected RsaKeyExchange keyEx = new RsaKeyExchange();

        public string IP { get { return (socket.RemoteEndPoint as IPEndPoint).Address.ToString(); } }
        public Blowfish BlowfishContext = null;

        protected abstract void CallMessageHandler(Message msg);
        protected abstract MessageServer Server { get; }
        protected abstract ClientHandler ClientHandler { get; }

        public void WriteDebug(string format, params object[] vals)
        {
            var str = connTypeDbg + format;
            if (vals.Length == 0)
                Console.WriteLine(str);
            else
                Console.WriteLine(str, vals);
        }

        public void WriteError(string format, params object[] vals)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            WriteDebug(format, vals);
            Console.ResetColor();
        }

        protected void HandleException(Exception e)
        {
            if (e is ObjectDisposedException)
                WriteError("Socket Disposed");
            else
                WriteError("Exception: {0}", e.ToString());

            Disconnect();
        }

        protected bool SocketErrors()
        {
            if (sockErr == SocketError.Success)
                return false;

            WriteError("Socket Error: {0}", sockErr.ToString());
            Disconnect();
            return true;
        }

        protected bool IncorrectLength(int receivedLen, int correctLen)
        {
            if (receivedLen == correctLen)
                return false;

            if (receivedLen != 0) //normal disconnect
                WriteError("Incorrect Length: Is {0}B should be {1}B!", receivedLen, correctLen);

            Disconnect();
            return true;
        }

        protected void Disconnect()
        {
            if (isDisconnected)
                return;

            isDisconnected = true;
            WriteDebug("Close Client {0}", endPointDbg);
            ClientHandler.Disconnect();
            try { socket.Close(); } catch { }
        }

        protected void StartReceiveHeader()
        {
            try
            {
                socket.BeginReceive(buffer, 0, 6, SocketFlags.None, out sockErr, ReceivedHeader, null);
                SocketErrors();
            }
            catch (Exception e) { HandleException(e); }
        }

        protected void ReceivedHeader(IAsyncResult ar)
        {
            try
            {
                var recvdBytes = socket.EndReceive(ar, out sockErr);
                if (SocketErrors() || IncorrectLength(recvdBytes, 6))
                    return;

                int msgSize = buffer[2] + 256 * buffer[1] + 256 * 256 * buffer[0];
                if (msgSize == 6)
                {
                    CallMessageHandler(Message.ParseIncoming(buffer, BlowfishContext).First());
                    StartReceiveHeader();
                }
                if (msgSize > 6)
                {
                    socket.BeginReceive(buffer, 6, msgSize - 6, SocketFlags.None, out sockErr, ReceivedBody, msgSize - 6);
                    SocketErrors();
                }
                else if (msgSize < 6)
                    WriteError("Message len = {0} WAT", msgSize);

            }
            catch (Exception e) { HandleException(e); }
        }

        protected void ReceivedBody(IAsyncResult ar)
        {
            try
            {
                var recvdBytes = socket.EndReceive(ar, out sockErr);
                if (SocketErrors() || IncorrectLength(recvdBytes, (int)ar.AsyncState))
                    return;

                CallMessageHandler(Message.ParseIncoming(buffer, BlowfishContext).First());
                StartReceiveHeader();
            }
            catch (Exception e) { HandleException(e); }
        }

        public void Send(Message msg)
        {
            try
            {
                if (isDisconnected)
                    return;

                if (msg.Code != MessageCode.RSAEXCHANGE && msg.Code != MessageCode.STILLALIVE)
                    WriteDebug("OUT: " + msg.ToString());

                var data = msg.Serialize();

                socket.BeginSend(data, 0, data.Length, SocketFlags.None, out sockErr, SentData, data.Length);
                SocketErrors();
            }
            catch (Exception e) { HandleException(e); }
        }

        protected void SentData(IAsyncResult ar)
        {
            try
            {
                var sentBytes = socket.EndSend(ar, out sockErr);
                if (SocketErrors() || IncorrectLength(sentBytes, (int)ar.AsyncState))
                    return;
            }
            catch (Exception e) { HandleException(e); }
        }

        protected void HandleKeyExchange(Message msg)
        {
            keyEx.ReadMessage(msg);
            var resp = keyEx.GetBfKeyResponse();
            BlowfishContext = new Blowfish(keyEx.Key);
            Send(resp);
        }
    }

    public class ClientConnection<T> : ClientConnection where T : ClientHandler
    {
        MessageServer<T> server;
        T clientHandler;

        public ClientConnection(MessageServer<T> serv, T cHandler, Socket clientSock)
        {
            server = serv;
            clientHandler = cHandler;
            clientHandler.Connection = this;
            socket = clientSock;
            StartReceiveHeader();
            endPointDbg = clientSock.RemoteEndPoint.ToString();
            connTypeDbg = (typeof(T)).Name;
            if (connTypeDbg.Length > 21)
                connTypeDbg = connTypeDbg.Substring(0, 21);
            connTypeDbg = connTypeDbg.PadRight(22);
            WriteDebug("New Client {0}", endPointDbg);
        }

        protected override MessageServer Server { get { return server; } }
        protected override ClientHandler ClientHandler { get { return clientHandler; } }


        static Message StillAlive = new Message(MessageType.GSMessage, MessageCode.STILLALIVE, null, 3, 8);
        protected override void CallMessageHandler(Message msg)
        {
            if (msg.Code != MessageCode.RSAEXCHANGE && msg.Code != MessageCode.STILLALIVE)
                WriteDebug("IN:  " + msg.ToString());

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
                    WriteError("Unhandled LobbyMessage [{0}]!", lobbyCode.ToString());
            }
            else
            {
                MessageHandler<T> handler;
                if (server.MessageHandlers.TryGetValue(msg.Code, out handler))
                    handler(clientHandler, msg);
                else
                    WriteError("Unhandled Message [{0}]!", msg.Code.ToString());
            }
        }
    }
}