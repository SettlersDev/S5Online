using S5GameServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            account.IP = Connection.IP;

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
                "\n  Hello " + account.Username + " [" + account.Language + "]",
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
                Connection.Send(msg.SuccessResponse(new DNodeList { acc.IrcAlias, acc.Username, acc.FirstName, acc.LastName, acc.Language, acc.Email, acc.IP }));

            // irc nick, username, firstn, lastn, lang, mail, ip of user
            //[GSSUCCESS] [PLAYERINFO["u0b1dd832" "yoq4711yoq" "Noe" "Lol" "de" "hy@there.net" "78.104.167.114" ]]
            //c.Send(msg.SuccessResponse(new DNodeList { "u0b1dd832", "yoq4711yoq", "Noe", "Lol", "de", "hy@there.net", c.IP }));
        }

        [Handler(LobbyMessageCode.LB_LOBBYLOGIN)]
        void LobbyLogin(Message msg)
        {
            var game = msg.LobbyData[0].AsString;
            Connection.Send(msg.LobbySuccessResponse());
            //LOBBY_MSG (209, 1, 4) [ "54" [ "0" "Le comptoir des Settlers" "870" "51" "0" "2068" "0" "" "SETTLERSHOK" "SETTLERSHOK" Bin{} "0" "500" "0"]]
            Connection.Send(new Message(MessageType.GSMessage, LobbyMessageCode.LB_GROUPNEW, new DNodeList { 0, "Developer's Playground 0", 870, 51, 0, 2068, 0, "", "SETTLERSHOK", "SETTLERSHOK", new byte[0], 0, 500, 0 }, 1, 4));
            Connection.Send(new Message(MessageType.GSMessage, LobbyMessageCode.LB_GROUPNEW, new DNodeList { 0, "Developer's Playground 1", 871, 52, 0, 2068, 0, "", "SETTLERSHOK", "SETTLERSHOK", new byte[0], 0, 500, 0 }, 1, 4));
            Connection.Send(new Message(MessageType.GSMessage, LobbyMessageCode.LB_GROUPNEW, new DNodeList { 0, "Developer's Playground 2", 872, 51, 0, 2068, 0, "", "SETTLERSHOK", "SETTLERSHOK", new byte[0], 0, 501, 0 }, 8, 2));
        }
    }

    static class WaitModuleServer
    {
        public static void Run()
        {
            var ms = new MessageServer<WaitModuleConnection>() { Port = 40001, TimeoutMS = 60000 };
            ms.Run();
        }
    }
}