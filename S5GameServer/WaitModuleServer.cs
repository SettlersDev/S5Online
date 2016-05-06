using S5GameServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace S5GameServer
{

    class WaitModuleConnection : ClientHandler
    {
        PlayerAccount account;

        [Handler(MessageCode.LOGINWAITMODULE)]
        protected void LoginWaitModuleCmd(Message msg)
        {
            var username = msg.Data[0].AsString;
            account = PlayerAccount.Get(username);

            if (account != null)
                Connection.Send(msg.SuccessResponse());
            else
                Connection.Send(msg.FailResponse(new DNodeList { new DNodeBinary(2) }));

            account.PublicIP = Connection.IP;

        }

        [Handler(MessageCode.MODIFYUSER)]
        protected void ModifyUserCmd(Message msg)
        {
            var emptyStr = msg.Data[0].AsString;
            var newPassword = msg.Data[1].AsString;
            var firstName = msg.Data[2].AsString;
            var lastName = msg.Data[3].AsString;
            var email = msg.Data[4].AsString;
            var language = msg.Data[5].AsString;

            account.FirstName = firstName;
            account.LastName = lastName;
            account.Email = email;
            account.Language = language;
            account.SetPassword(newPassword);
            account.UpdateStore();

            Connection.Send(msg.SuccessResponse());
            //c.Send(msg.FailResponse(new DNodeList { new DNodeBinary(1) }));
        }

        [Handler(MessageCode.GETMOTD)]
        protected void GetMOTD(Message msg)
        {
            //ubisoftmessage, gamemessage
            var language = msg.Data[0].AsString;
            Connection.Send(msg.SuccessResponse(new DNodeList
            {
                "", //do not use!
                "\n  Hello " + account.Username + " [" + account.Language + "]\n" + 
                "\n  Welcome to yoq's Testserver!\n  Currently working:\n    - CDKey\n    - IRC\n    - first level proxy connection"
            }));
        }

        [Handler(MessageCode.PLAYERINFO)]
        void GetPlayerInfo(Message msg)
        {
            var username = msg.Data[0].AsString;

            var acc = PlayerAccount.Get(username);
            if (acc == null)
                Connection.Send(msg.FailResponse(new DNodeList { new DNodeBinary(1) }));
            else
                Connection.Send(msg.SuccessResponse(new DNodeList { acc.IrcAlias, acc.Username, acc.FirstName, acc.LastName, acc.Language, acc.Email, acc.PublicIP }));
        }

        [Handler(LobbyMessageCode.LB_LOBBYLOGIN)]
        void LobbyLogin(Message msg)
        {
            var game = msg.LobbyData[0].AsString;
            Connection.Send(msg.LobbySuccessResponse());

            foreach (var lobby in Lobby.AllLobbies)
                Connection.Send(new Message(LobbyMessageCode.LB_GROUPNEW, lobby.LobbyInfo));
        }

        [Handler(LobbyMessageCode.LB_LOBBYSERVERJOIN)]
        protected void LobbyServerJoin(Message msg)
        {
            if (msg.LobbyData[0].AsInt == Constants.LOBBY_SERVER_ID)
                Connection.Send(msg.LobbySuccessResponse(new DNodeList { Constants.LOBBY_SERVER_ID, ServerConfig.Instance.HostName, Constants.LOBBY_SERVER_PORT }));
            else
                Connection.Send(msg.LobbyFailResponse());
        }

        [Handler(MessageCode.NEWQUERY)]
        protected void NewQuery(Message msg)
        {
            /*
                GAM 4|1 UNKNOWN(204) ["1" ["ladderquery" "0" "0"]]
                SRV 1|4 UNKNOWN(204) ["38" ["1" ["ladderquery" "0" "0" [["48" "216.98.52.177" "42000"]]]]]
                GAM 4|1 UNKNOWN(204) ["2" ["48"]]
                SRV 1|4 UNKNOWN(204) ["38" ["2" ["48"]]]
                GAM 4|1 UNKNOWN(204) [["3" "48"]] 
            */
            if (!(msg.Data[0] is DNodeString))
                return;

            var num = msg.Data[0].AsInt;
            switch (num)
            {
                case 1:
                    if (msg.Data[1][0].AsString == "ladderquery")
                    {
                        Connection.Send(new Message(MessageCode.NEWQUERY, new DNodeList { (int)MessageCode.GSSUCCESS, { 1, new DNodeList {
                        "ladderquery", 0, 0, new DNodeList { new DNodeList { 48, ServerConfig.Instance.HostName, Constants.LADDER_LOGIN_SERVER_PORT } } } } }));
                    }
                    break;

                case 2:
                    Connection.Send(new Message(MessageCode.NEWQUERY, new DNodeList { (int)MessageCode.GSSUCCESS, msg.Data }));
                    break;
            }

        }
    }

    static class WaitModuleServer
    {
        public static void Run()
        {
            var ms = new MessageServer<WaitModuleConnection>() { Port = Constants.WAITMODULE_SERVER_PORT, TimeoutMS = 60000 };
            ms.Run();
        }
    }
}