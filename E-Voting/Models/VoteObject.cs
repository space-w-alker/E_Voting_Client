using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Voting.Models
{
    public class VoteObject
    {
        public long CandidateId { get; set; }
        public byte[] EncryptedVote { get; set; }
        public byte BounceCount { get; set; }
        public string Action = "VOTE";

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    
}
