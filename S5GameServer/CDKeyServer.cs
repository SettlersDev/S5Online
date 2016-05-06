using S5GameServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace S5GameServer
{
    public static class CDKeyServer
    {
        static Random rng = new Random();
        static UdpClient udpClient;

        static CDKeyMessage getTokenResponse = new CDKeyMessage(new DNodeList { 99, 1, 1, { (int)MessageCode.GSSUCCESS, new DNodeList { new byte[5] } } });
        static CDKeyMessage authorizeResponse = new CDKeyMessage(new DNodeList { 99, 2, 2, { (int)MessageCode.GSSUCCESS, new DNodeList { new byte[16], new byte[16] } } });
        static CDKeyMessage login1Response = new CDKeyMessage(new DNodeList { 99, 3, 1, { (int)MessageCode.GSSUCCESS, new DNodeList { new byte[20] } } });
        static CDKeyMessage login2Response = new CDKeyMessage(new DNodeList { 99, 4, 2, { (int)MessageCode.GSSUCCESS, new DNodeList { 2, new byte[16] } } });
        static CDKeyMessage logoutResponse = new CDKeyMessage(new DNodeList { 99, 6, 1, { (int)MessageCode.GSSUCCESS, new DNodeList() } });

        static int ToID(byte[] token) //yea, this cuts the hashes to 32bit, but 1 in 4B should be good enough to protect against collisions
        {
            return BitConverter.ToInt32(token, 0);
        }

        static byte[] ToToken(int id, int length)
        {
            var token = BitConverter.GetBytes(id);
            Array.Resize(ref token, length);
            return token;
        }

        public static void Run(int port = 44000)
        {
            var ipep = new IPEndPoint(IPAddress.Any, port);
            udpClient = new UdpClient(ipep);
            udpClient.BeginReceive(HandlePacket, udpClient);
        }

        async static void HandlePacket(IAsyncResult ar)
        {
            var udpClient = ar.AsyncState as UdpClient;
            var remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            var reqData = udpClient.EndReceive(ar, ref remoteEndpoint);
            udpClient.BeginReceive(HandlePacket, udpClient);

            var req = new CDKeyMessage(reqData);
            //Console.WriteLine("CDK IN :\t" + req.Data.ToString());

            CDKeyMessage response;
            int clientID;
            byte[] token;
            switch (req.Data[1].AsInt)
            {
                case 1: response = getTokenResponse; break;
                case 2:
                    response = authorizeResponse;
                    clientID = rng.Next(int.MinValue, int.MaxValue);
                    token = ToToken(clientID, 16);
                    response.Data[3][1][0].AsBinary = token;
                    response.Data[3][1][1].AsBinary = token;
                    break;
                case 3:
                    response = login1Response;
                    clientID = ToID(req.Data[3][0].AsBinary);
                    response.Data[3][1][0].AsBinary = ToToken(clientID, 20);
                    await Task.Delay(250); //crazy racecondition in S5: the host needs to have set up his XNetwork before the clients send their AuthTokens to verify, 
                    break;
                case 4:
                    response = login2Response;
                    clientID = ToID(req.Data[3][0].AsBinary);
                    response.Data[3][1][1].AsBinary = ToToken(clientID, 16);
                    break;
                case 6: response = logoutResponse; break;
                case 7: return; //heartbeat
                default: throw new Exception("Unknown CDKey request: " + req.Data.ToString());
            }

            var udpConnId = req.Data[0].AsString;
            response.Data[0].AsString = udpConnId;
            //Console.WriteLine("CDK OUT:\t" + response.Data.ToString());

            var respData = response.Serialize();
            await udpClient.SendAsync(respData, respData.Length, remoteEndpoint);
        }
    }
}
