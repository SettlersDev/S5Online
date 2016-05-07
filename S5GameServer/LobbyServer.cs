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
        Lobby curLobby;
        GameRoom curRoom;

        [Handler(MessageCode.LOBBYSERVERLOGIN)]
        protected void LobbyServerLogin(Message msg)
        {
            var username = msg.Data[0].AsString;
            var unknownNum = msg.Data[1].AsInt;
            var localIP = msg.Data[2].AsString;
            var localSubnet = msg.Data[3].AsString;
            var unknownNum2 = msg.Data[4].AsInt;
            Connection.WriteDebug("lobby server login ({0} | {1})", unknownNum, unknownNum2);

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
                curLobby.LobbyID, 448, curLobby.LobbyInfo,
                curLobby.Rooms.Values.Select((r) => r.RoomInfo),
                curLobby.Players.Select((p) => p.PlayerInfo)
            }));
            curLobby.Broadcast(new Message(LobbyMessageCode.LB_MEMBERGROUPJOIN, UserInfoBlock(curLobby.LobbyID)));
            //lobby.Broadcast(new Message((LobbyMessageCode)66, new DNodeList { account.GameIdentifier }));
            Connection.Send(msg.LobbySuccessResponse());
        }

        [Handler(LobbyMessageCode.LB_LOBBYJOIN)]
        protected void LBLobbyLogin(Message msg)
        {
            var lobbyID = msg.LobbyData[0].AsInt;
            var unknown = msg.LobbyData[1].AsString;
            var unknown448 = msg.LobbyData[2].AsInt;

            curLobby = Lobby.Get(lobbyID);
            if (curLobby != null)
            {
                if (!curLobby.Players.Contains(account))
                {
                    curLobby.Players.Add(account);
                    curLobby.LobbyMessage += ForwardMessage;
                }
                Connection.Send(msg.LobbySuccessResponse(new DNodeList { lobbyID }));
            }
        }

        [Handler(LobbyMessageCode.LB_GROUPLEAVE)]
        protected void GroupLeave(Message msg)
        {
            var id = msg.LobbyData[0].AsInt;

            if (id > 0) //lobby
            {
                LeaveLobby();
                Connection.Send(msg.LobbySuccessResponse(new DNodeList { id }));
            }
            else if (id < 0) //game room
            {
                LeaveRoom();
                Connection.Send(msg.LobbySuccessResponse(new DNodeList { id }));
                CheckRoomEmpty();
                curRoom = null;
            }
            else
                throw new NotImplementedException();

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
            Connection.WriteDebug("CreateRoom({0}|{1}|{2})", numA, numB, numC);

            var gameInfo = msg.LobbyData[6].AsBinary;

            var room = curLobby.CreateRoom(roomName, gameInfo, account);
            Connection.Send(msg.LobbySuccessResponse(new DNodeList { room.ID, roomName, Constants.LOBBY_SERVER_ID }));
            curLobby.Broadcast(new Message(LobbyMessageCode.LB_GROUPNEW, room.RoomInfo));
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
            var room = curLobby.GetRoom(roomID);
            if (room == null)
                return;

            if (!room.Players.Contains(account))
            {
                room.Players.Add(account);
                room.RoomMessage += ForwardMessage;
            }
            curRoom = room;
            Connection.Send(msg.LobbySuccessResponse(new DNodeList { roomID }));
            curLobby.Broadcast(new Message(LobbyMessageCode.LB_MEMBERGROUPJOIN, UserInfoBlock(roomID)));
        }

        [Handler(LobbyMessageCode.LB_UPDATEGROUPSETTINGS)]
        protected void UpdateRoomSetting(Message msg)
        {
            /*
                [LB_UPDATEGROUPSETTINGS("31"), ["-408" "32" "__CLOSED_ROOM__"]]
            */

            var roomID = msg.LobbyData[0].AsInt;
            var room = curLobby.GetRoom(roomID);
            if (room == null)
                return;

            var cmd = msg.LobbyData[2].AsString;
            if (cmd == "__CLOSED_ROOM__")
            {
                // WAT DO?!
                //SRV 12|4 LOBBY_MSG(209) [LB_GSSUCCESS("38"), ["31" ["-408"]]]
                // maybe SRV 12|4 LOBBY_MSG(209) [LB_GROUPCONFIGUPDATE("57"), ["-408" "2099"]]

                Connection.Send(msg.LobbySuccessResponse(new DNodeList { roomID }));
                room.Broadcast(new Message(LobbyMessageCode.LB_GROUPCONFIGUPDATE, new DNodeList { roomID, 2099 }));
            }
            else
                throw new NotImplementedException();
        }

        [Handler(LobbyMessageCode.LB_GAMESTART)]
        protected void GameStart(Message msg)
        {
            Connection.Send(msg.LobbySuccessResponse(new DNodeList { curRoom.ID }));
        }

        [Handler(LobbyMessageCode.LB_GAMEREADY)]
        protected void GameReady(Message msg)
        {
            Connection.Send(msg.LobbySuccessResponse(new DNodeList { curRoom.ID }));
            curRoom.Broadcast(new Message(LobbyMessageCode.LB_GROUPCONFIGUPDATE, new DNodeList { curRoom.ID, 2106 }));
            //[LB_GAMESTARTED("56"), ["-110" Bin{} "0" "84.115.212.253" "10.9.9.9"]]
            curRoom.Broadcast(new Message(LobbyMessageCode.LB_GAMESTARTED, new DNodeList { curRoom.ID, new byte[0], 0, curRoom.Host.PublicIP, curRoom.Host.LocalIP }));
            curRoom.Broadcast(new Message(LobbyMessageCode.LB_GROUPCONFIGUPDATE, new DNodeList { curRoom.ID, 2106 }));
        }

        [Handler(LobbyMessageCode.LB_GAMECONNECTED)]
        protected void GameConnected(Message msg)
        {
            curRoom.Broadcast(new Message(LobbyMessageCode.LB_PLAYERUPDATESTATUS, new DNodeList { account.Username, 2 }));
        }

        [Handler(LobbyMessageCode.LB_GAMEFINISHED)]
        protected void GameFinished(Message msg)
        {
            Connection.Send(new Message(LobbyMessageCode.LB_WAKEUP, new DNodeList()));
            curRoom.Broadcast(new Message(LobbyMessageCode.LB_PLAYERUPDATESTATUS, new DNodeList { account.Username, 0 }));
            Connection.Send(new Message(LobbyMessageCode.LB_GROUPCONFIGUPDATE, new DNodeList { curRoom.ID, 2098 }));
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
            curLobby.Broadcast(new Message(LobbyMessageCode.LB_MEMBERGROUPLEAVE, new DNodeList { account.Username, curRoom.ID }));
            curRoom.Players.Remove(account);
            curRoom.RoomMessage -= ForwardMessage;
        }

        protected void LeaveLobby()
        {
            curLobby.Broadcast(new Message(LobbyMessageCode.LB_MEMBERGROUPLEAVE, new DNodeList { account.Username, curLobby.LobbyID }));
            curLobby.Players.Remove(account);
            curLobby.LobbyMessage -= ForwardMessage;
        }

        protected void CheckRoomEmpty()
        {
            if (curRoom.Players.Count == 0)
            {
                curLobby.Broadcast(new Message(LobbyMessageCode.LB_GROUPREMOVE, new DNodeList { curRoom.ID }));
                curLobby.Rooms.Remove(curRoom.ID);
            }
        }

        public override void Disconnect()
        {
            if (account == null || curLobby == null)
                return;

            if (curRoom != null)
            {
                LeaveRoom();
                CheckRoomEmpty();
            }

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
