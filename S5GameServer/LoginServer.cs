using S5GameServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServer
{
    class LoginClientHandler : ClientHandler
    {

        [Handler(MessageCode.LOGIN)]
        protected void LoginCmd(Message msg)
        {
            var username = msg.Data[0].AsString;
            var password = msg.Data[1].AsString;
            var game = msg.Data[2].AsString; // "SHOKPC1.05"

            var acc = PlayerAccount.Get(username);
            if (acc != null)
            {
                if (acc.CheckPassword(password))
                    Connection.Send(msg.SuccessResponse());
                else
                    Connection.Send(msg.FailResponse(new DNodeList { new DNodeBinary(2) }));
            }
            else
            {
                Connection.Send(msg.FailResponse(new DNodeList { new DNodeBinary(4) }));

                /* 1    logging in failed
                 * 2    wrong password
                 * 3    already logged in
                 * 4    invalid username
                 * */
            }
        }

        [Handler(MessageCode.JOINWAITMODULE)]
        protected void JoinWaitModule(Message msg)
        {
            Connection.Send(msg.SuccessResponse(new DNodeList { ServerConfig.Instance.HostName, new DNodeBinary(Constants.WAITMODULE_SERVER_PORT) }));
        }


        [Handler(MessageCode.NEWUSERREQUEST)]
        protected void NewUserCmd(Message msg)
        {
            var game = msg.Data[0].AsString; //SHOKPC1.05
            var username = msg.Data[1].AsString;
            var password = msg.Data[2].AsString;
            var firstName = msg.Data[3].AsString;
            var lastName = msg.Data[4].AsString;
            var email = msg.Data[5].AsString;
            var language = msg.Data[6].AsString;

            if (!PlayerAccount.IsNameAvailable(username))
                Connection.Send(msg.FailResponse(new DNodeList { new DNodeBinary(1) }));
            else
            {
                var pa = new PlayerAccount
                {
                    Username = username,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Language = language
                };
                pa.SetPassword(password);
                pa.UpdateStore();

                Connection.Send(msg.SuccessResponse());
            }

            /* 1    username taken
             * 2    username not according to rules
             * 3    username contains illegal words
             * */
        }
    }

    static class LoginServer
    {
        public static void Run()
        {
            var ms = new MessageServer<LoginClientHandler>
            {
                Port = ServerConfig.Instance.RouterPort,
                TimeoutMS = 10000
            };
            ms.Run();
        }
    }
}
