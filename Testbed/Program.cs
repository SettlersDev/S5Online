using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S5GameServices;
using System.IO;
using System.Collections;

namespace Testbed
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.SetWindowSize(200, 30);

            var pp = File.ReadAllBytes("d:/reproj/shok/crypto/ppInit.bin");

            XorCrypt.Decrypt(pp);

            Console.ReadKey();
        }
    }
}
