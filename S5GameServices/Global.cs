using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5GameServices
{
    public static class Global
    {
        public static Blowfish CDKeyCrypt = new Blowfish(Encoding.ASCII.GetBytes("SKJDHF$0maoijfn4i8$aJdnv1jaldifar93-AS_dfo;hjhC4jhflasnF3fnd"));
        public static Blowfish IRCCrypt = new Blowfish(new byte[] { 0x06, 0xE2, 0xC8, 0x46, 0x01, 0x90, 0x55, 0x7C, 0x3C, 0xA1, 0xCD, 0xA3, 0xE3, 0xA1, 0x10, 0x6C });
        public static Encoding ServerEncoding = Encoding.GetEncoding("iso-8859-15");
    }
}
