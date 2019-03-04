using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace E_Voting.Models
{
    public class LoginResponse
    {
        public bool Error { get; set; }
        public RSAParameters Key { get; set; }
        public string Message { get; set; }
        public List<string> ActiveUsers { get; set; }
    }
}
