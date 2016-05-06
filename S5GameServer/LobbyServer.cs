using S5GameServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace S5GameServer
{
    class LobbyServerConnection : ClientHandler
    {
        PlayerAccount account;
        Lobby lobby;

        [Handler(MessageCode.LOBBYSERVERLOGIN)]
        protected void LobbyServerLogin(Message msg)
        {
            var username = msg.Data[0].AsString;
            var unknownNum = msg.Data[1].AsInt;
            var localIP = msg.Data[2].AsString;
            var localSubnet = msg.Data[3].AsString;
            var unknownNum2 = msg.Data[4].AsInt;
            Console.WriteLine("lobby server login ({0} | {1})", unknownNum, unknownNum2);

            account = PlayerAccount.Get(username);
            if (account != null)
            {
                account.LocalIP = localIP;
                account.LocalSubnet = localSubnet;
                account.PublicIP = Connection.IP;

                Connection.Send(new Message(MessageCode.GSSUCCESS, new DNodeList { (int)MessageCode.LOBBYSERVERLOGIN, new DNodeList { Constants.LOBBY_SERVER_ID } }));
            }
        }

        [Handler(LobbyMessageCode.LB_LOBBYINFO)]
        protected void LobbyRoomInfo(Message msg)
        {
            account.GameIdentifier = msg.LobbyData[0].AsBinary;
            Connection.Send(new Message(LobbyMessageCode.LB_GROUPINFO, new DNodeList
            {
                lobby.LobbyID, 448, lobby.LobbyInfo, new DNodeList(), lobby.Players.Select((p) => p.PlayerInfo)
            }));
            lobby.Broadcast(new Message(LobbyMessageCode.LB_MEMBERGROUPJOIN, UserInfoBlock(lobby.LobbyID)));
            //lobby.Broadcast(new Message((LobbyMessageCode)66, new DNodeList { account.GameIdentifier }));
            Connection.Send(msg.LobbySuccessResponse());
        }

        [Handler(LobbyMessageCode.LB_LOBBYJOIN)]
        protected void LBLobbyLogin(Message msg)
        {
            var lobbyID = msg.LobbyData[0].AsInt;
            var unknown = msg.LobbyData[1].AsString;
            var unknown448 = msg.LobbyData[2].AsInt;

            lobby = Lobby.Get(lobbyID);
            if (lobby != null)
            {
                if (!lobby.Players.Contains(account))
                {
                    lobby.Players.Add(account);
                    lobby.LobbyMessage += Lobby_LobbyMessage;
                }
                Connection.Send(msg.LobbySuccessResponse(new DNodeList { lobbyID }));
            }
        }

        [Handler(LobbyMessageCode.LB_GROUPLEAVE)]
        protected void GroupLeave(Message msg)
        {
            if (msg.LobbyData[0].AsInt == lobby.LobbyID)
            {
                LeaveLobby();
                Connection.Send(msg.LobbySuccessResponse(new DNodeList { lobby.LobbyID }));
            }
            else //leaving room
            {
                throw new NotImplementedException();
            }
        }


        protected DNodeList UserInfoBlock(int group)
        {
            return new DNodeList { account.Username, 0, group, account.PublicIP, account.LocalIP, account.GameIdentifier, 0 };
        }

        protected void Lobby_LobbyMessage(object sender, Message msg)
        {
            Connection.Send(msg);
        }

        protected void LeaveLobby()
        {
            lobby.Broadcast(new Message(LobbyMessageCode.LB_MEMBERGROUPLEAVE, new DNodeList { account.Username, lobby.LobbyID }));
            lobby.Players.Remove(account);
            lobby.LobbyMessage -= Lobby_LobbyMessage;
        }

        public override void Disconnect()
        {
            if (account == null || lobby == null)
                return;

            LeaveLobby();
        }
    }

    static class LobbyServer
    {
        public static void Run()
        {
            var msa = new MessageServer<LobbyServerConnection>() { Port = Constants.LOBBY_SERVER_PORT, TimeoutMS = 60000 };
            msa.Run();
        }
    }
}
