using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace S5GameServices
{
    public class RsaKeyExchange
    {
        public byte[] Key { get; protected set; }       //128Bit Blowfish key
        protected RSACryptoServiceProvider rsa;
        
        public void ReadMessage(Message msg)
        {
            var isPublicKey = msg.Data[0].AsInt == 1;

            if(isPublicKey)
            {
                var data = msg.Data[1][2].AsBinary;
                var pubkey = new byte[64];
                Array.Copy(data, 0x44, pubkey, 0, 64);

                rsa = new RSACryptoServiceProvider();
                rsa.ImportParameters(new RSAParameters()
                {
                    Exponent = new byte[] { 3 },
                    Modulus = pubkey
                });
            }
            else
            {
                var encryptedKey = msg.Data[1][2].AsBinary;
                Key = rsa.Decrypt(encryptedKey, false);
            }
        }

        public Message GetPublicKeyRequest() //sends the pubkey, requests a encrypted BF key
        {
            rsa = new RSACryptoServiceProvider(512);
            var rsaParams = rsa.ExportParameters(false);

            var pkData = new byte[260];
            pkData[1] = 2; //0x200 = 512Bit
            pkData[259] = 3; //exp = 3
            Array.Copy(rsaParams.Modulus, 0, pkData, 0x44, 64);

            var data = new DNodeList { 1, { 1, pkData.Length, pkData } };

            return new Message(MessageType.GSMessage, MessageCode.RSAEXCHANGE, data, 8, 2);
        }

        public Message GetBfKeyResponse()
        {
            var rng = new RNGCryptoServiceProvider();
            Key = new byte[16];
            rng.GetBytes(Key);
            var encryptedBF = rsa.Encrypt(Key, false);

            var data = new DNodeList { 2, { 1, encryptedBF.Length, encryptedBF } };

            return new Message(MessageType.GSMessage, MessageCode.RSAEXCHANGE, data, 8, 2);
        }
    }
}
