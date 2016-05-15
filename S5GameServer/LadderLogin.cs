using S5GameServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServer
{

    class LadderWaitModuleConnection : ClientHandler
    {
        PlayerAccount account;

        [Handler(MessageCode.LOGIN)]
        protected void LadderLogin(Message msg)
        {
            var username = msg.Data[0].AsString;
            var game = msg.Data[1].AsString;

            account = PlayerAccount.Get(username);
            if (account != null)
                Connection.Send(msg.SuccessResponse(new DNodeList()));
        }

        [Handler(MessageCode.JOINWAITMODULE)]
        protected void LadderJoinServer(Message msg)
        {
            if (account != null)
                Connection.Send(msg.SuccessResponse(new DNodeList { account.Username, ServerConfig.Instance.HostName, Constants.LADDER_SERVER_PORT }));
        }
    }
}
