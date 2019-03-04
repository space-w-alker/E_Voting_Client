using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace E_Voting.Security
{
    public class RSAImplementation
    {
        public static byte[] Encrypt (byte[] message, RSAParameters param)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(param);
            return rsa.Encrypt(message, false);
        }
    }
}
