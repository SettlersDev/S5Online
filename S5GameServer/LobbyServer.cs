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
        Lobby actLobby;
        GameRoom actRoom;

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
        protected void LobbyInfo(Message msg)
        {
            account.GameIdentifier = msg.LobbyData[0].AsBinary;
            Connection.Send(new Message(LobbyMessageCode.LB_GROUPINFO, new DNodeList
            {
                actLobby.LobbyID, 448, actLobby.LobbyInfo,
                actLobby.Rooms.Values.Select((r) => r.RoomInfo),
                actLobby.Players.Select((p) => p.PlayerInfo)
            }));
            actLobby.Broadcast(new Message(LobbyMessageCode.LB_MEMBERGROUPJOIN, UserInfoBlock(actLobby.LobbyID)));
            //lobby.Broadcast(new Message((LobbyMessageCode)66, new DNodeList { account.GameIdentifier }));
            Connection.Send(msg.LobbySuccessResponse());
        }

        [Handler(LobbyMessageCode.LB_LOBBYJOIN)]
        protected void LBLobbyLogin(Message msg)
        {
            var lobbyID = msg.LobbyData[0].AsInt;
            var unknown = msg.LobbyData[1].AsString;
            var unknown448 = msg.LobbyData[2].AsInt;

            actLobby = Lobby.Get(lobbyID);
            if (actLobby != null)
            {
                if (!actLobby.Players.Contains(account))
                {
                    actLobby.Players.Add(account);
                    actLobby.LobbyMessage += ForwardMessage;
                }
                Connection.Send(msg.LobbySuccessResponse(new DNodeList { lobbyID }));
            }
        }

        [Handler(LobbyMessageCode.LB_GROUPLEAVE)]
        protected void GroupLeave(Message msg)
        {
            var id = msg.LobbyData[0].AsInt;

            if (id == actLobby.LobbyID)
            {
                LeaveLobby();
            }
            else if (id == actRoom.ID)
            {
                LeaveRoom();
            }
            else
                throw new NotImplementedException();

            Connection.Send(msg.LobbySuccessResponse(new DNodeList { id }));
        }

        [Handler(LobbyMessageCode.LB_ROOMCREATE)]
        protected void RoomCreate(Message msg)
        {
            //GAM 4 | 2 LOBBY_MSG(209)[LB_ROOMCREATE("12"), ["871" "yoq4711yoq's Spiel-20:21:46" "SETTLERSHOK" "7" "8" "0" Bin{ 00 2C 6B 02 02 00 00 00 73 DF DD E9 D8 2C 6B 02} "" "SHOKPC1.05" "SHOKPC1.05" Bin{ }]]
            //SRV 12 | 4 LOBBY_MSG(209)[LB_GSSUCCESS("38"), ["12"["-126" "yoq4711yoq's Spiel-20:21:46" "51"]]]
            //SRV 12 | 4 LOBBY_MSG(209)[LB_GROUPNEW("54"), ["7" "yoq4711yoq's Spiel-20:21:46" "-126" "51" "871" "2098" "1" "yoq4711yoq" "SETTLERSHOK" "SETTLERSHOK" Bin{ 00 2C 6B 02 02 00 00 00 73 DF DD E9 D8 2C 6B 02} "0" "8" "1" "0" "0" "SHOKPC1.05" "SHOKPC1.05" "84.115.212.253" "10.9.9.9"]]

            var roomName = msg.LobbyData[1].AsString;
            var numA = msg.LobbyData[3].AsInt;
            var numB = msg.LobbyData[4].AsInt;
            var numC = msg.LobbyData[5].AsInt;
            Console.WriteLine("CreateRoom({0}|{1}|{2})", numA, numB, numC);

            var gameInfo = msg.LobbyData[6].AsBinary;

            var room = actLobby.CreateRoom(roomName, gameInfo, account);
            Connection.Send(msg.LobbySuccessResponse(new DNodeList { room.ID, roomName, Constants.LOBBY_SERVER_ID }));
            actLobby.Broadcast(new Message(LobbyMessageCode.LB_GROUPNEW, room.RoomInfo));
        }

        [Handler(LobbyMessageCode.LB_ROOMJOIN)]
        protected void RoomJoin(Message msg)
        {
            /*
                GAM 4|2 LOBBY_MSG(209) [LB_ROOMJOIN("24"), ["-126" "" "0" "0" "SHOKPC1.05"]]
                SRV 12|4 LOBBY_MSG(209) [LB_GSSUCCESS("38"), ["24" ["-126"]]]
                SRV 12|4 LOBBY_MSG(209) [LB_MEMBERGROUPJOIN("50"), ["yoq4711yoq" "0" "-126" "84.115.212.253" "10.9.9.9" Bin{02 00 00 00 73 DF DD E9 00 00 00 00 00 00 00 00} "0"]]
            */

            var roomID = msg.LobbyData[0].AsInt;
            var room = actLobby.GetRoom(roomID);
            if (room == null)
                return;

            if (!room.Players.Contains(account))
            {
                room.Players.Add(account);
                room.RoomMessage += ForwardMessage;
            }
            actRoom = room;
            Connection.Send(msg.LobbySuccessResponse(new DNodeList { roomID }));
            room.Broadcast(new Message(LobbyMessageCode.LB_MEMBERGROUPJOIN, UserInfoBlock(roomID)));
        }

        protected DNodeList UserInfoBlock(int group)
        {
            return new DNodeList { account.Username, 0, group, account.PublicIP, account.LocalIP, account.GameIdentifier, 0 };
        }

        protected void ForwardMessage(object sender, Message msg)
        {
            Connection.Send(msg);
        }

        protected void LeaveRoom()
        {
            actRoom.Broadcast(new Message(LobbyMessageCode.LB_MEMBERGROUPLEAVE, new DNodeList { account.Username, actRoom.ID }));
            actRoom.Players.Remove(account);
            actRoom.RoomMessage -= ForwardMessage;
        }

        protected void LeaveLobby()
        {
            actLobby.Broadcast(new Message(LobbyMessageCode.LB_MEMBERGROUPLEAVE, new DNodeList { account.Username, actLobby.LobbyID }));
            actLobby.Players.Remove(account);
            actLobby.LobbyMessage -= ForwardMessage;
        }

        public override void Disconnect()
        {
            if (account == null || actLobby == null)
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
