using S5GameServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServer
{
    public static class CDKeyServer
    {
        public static void Run(int port = 44000)
        {
            Task.Run(() =>
            {
                var ipep = new IPEndPoint(IPAddress.Any, port);
                var udpClient = new UdpClient(ipep);
                var remoteEndpoint = new IPEndPoint(IPAddress.Any, port);

                var getTokenResponse = new CDKeyMessage(new DNodeList { 99, 1, 1, { (int)MessageCode.GSSUCCESS, new DNodeList { new byte[5] } } });
                var authorizeResponse = new CDKeyMessage(new DNodeList { 99, 2, 2, { (int)MessageCode.GSSUCCESS, new DNodeList { new byte[16], new byte[16] } } });
                var login1Response = new CDKeyMessage(new DNodeList { 99, 3, 1, { (int)MessageCode.GSSUCCESS, new DNodeList { new byte[20] } } });
                var login2Response = new CDKeyMessage(new DNodeList { 99, 4, 2, { (int)MessageCode.GSSUCCESS, new DNodeList { 2, new byte[16] } } });
                var logoutResponse = new CDKeyMessage(new DNodeList { 99, 6, 1, { (int)MessageCode.GSSUCCESS, new DNodeList() } });

                
                while (true)
                {
                    var reqData = udpClient.Receive(ref remoteEndpoint);
                    var req = new CDKeyMessage(reqData);
                    Console.WriteLine("CDK IN :\t" + req.Data.ToString());

                    CDKeyMessage response;
                    switch (req.Data[1].AsInt)
                    {
                        case 1: response = getTokenResponse; break;
                        case 2: response = authorizeResponse; break;
                        case 3: response = login1Response; break;
                        case 4: response = login2Response; break;
                        case 6: response = logoutResponse; break;
                        case 7: continue; //heartbeat
                        default: throw new Exception("Unknown CDKey request: " + req.Data.ToString());
                    }

                    var udpConnId = req.Data[0].AsString;
                    response.Data[0].AsString = udpConnId;
                    Console.WriteLine("CDK OUT:\t" + response.Data.ToString());

                    var respData = response.Serialize();
                    udpClient.Send(respData, respData.Length, remoteEndpoint);
                }
            });

        }
    }
}
