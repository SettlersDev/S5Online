using S5GameServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServer
{

    class LadderConnection : ClientHandler
    {
        PlayerAccount account;

        [Handler(MessageCode.LOGINWAITMODULE)]
        protected void Login(Message msg)
        {
            var username = msg.Data[0].AsString;
            account = PlayerAccount.Get(username);

            if (account != null)
                Connection.Send(msg.SuccessResponse(new DNodeList()));
        }

        [Handler(MessageCode.NEWQUERY)]
        protected void Ladder(Message msg)
        {
            var num = msg.Data[1].AsInt;
            Connection.Send(new Message(MessageCode.NEWQUERY, new DNodeList
            {
                1281,
                num,
                new DNodeList
                {
                    1,
                    num % 2 == 1 ? 4235 : 4050,
                    new DNodeList { "QUERY_RANK", "QUERY_RANK" },
                    new DNodeList { "GLOBAL_RANK", "GLOBAL_RANK" },
                    new DNodeList { "ALIAS", "ALIAS" },
                    new DNodeList { "COUNTRY", "COUNTRY" },
                    new DNodeList { "COUNTRY_ID", "COUNTRY_ID" },
                    new DNodeList { "SCORE", "SCORE" },
                    new DNodeList { "WINS", "WINS" },
                    new DNodeList { "LOSSES", "LOSSES" },
                    new DNodeList { "NB_GAMES", "NB_GAMES" }
                },
                new DNodeList { }
            }));
        }
    }
    static class LadderServer
    {
        public static void Run()
        {
            var msa = new MessageServer<LadderConnection>() { Port = Constants.LADDER_SERVER_PORT, TimeoutMS = 60000 };
            msa.Run();
        }
    }
}
