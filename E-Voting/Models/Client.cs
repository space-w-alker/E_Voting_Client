
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;
using System.ComponentModel;
using E_Voting.TCP;

namespace E_Voting.Models
{
    public class Client:INotifyPropertyChanged
    {
        private HttpClient httpClient;
        public string Username { get; set; }
        public RSAParameters ClientKey { get; set; }
        public byte[] ClientPrivateKey = new byte[16];
        public string ServerLocation;
        public int ServerPort;
        public int AppPort;
        public List<String> active_users;
        public List<String> ActiveUsers { get { return active_users; } set { active_users = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ActiveUsers")); } }
        Random rnd;

        public event PropertyChangedEventHandler PropertyChanged;

        public Client()
        {
            httpClient = new HttpClient();
            active_users = new List<string>();
            new Random().NextBytes(ClientPrivateKey);
            ServerLocation = "127.0.0.1";
            ServerPort = 13002;
            AppPort = 13001;
        }

        

        

        public string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public async Task<string> ConnectToServer(string ServerAddress)
        {
            string ArrtoSend = JsonConvert.SerializeObject(new { IpAddress = this.GetLocalIPAddress() });
            
            var content = new StringContent(ArrtoSend, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(ServerAddress, content);
            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }


        public async Task<string> LoginInToServerAsync(string username, string password)
        {
            string ArrtoSend = JsonConvert.SerializeObject(new { Username = username, Password = password, IpAddress = GetLocalIPAddress(), Action = "LOGIN" });
            string responseString = await Task.Run(()=> TcpServer.TCP_SendData(ServerLocation, ArrtoSend, ServerPort));
            return responseString;
        }
        
        public async Task<PingResponse> PingServer()
        {
            UnicodeEncoding ByteConverter = new UnicodeEncoding();
            byte[] message = Security.RSAImplementation.Encrypt(ByteConverter.GetBytes("APPLICATION"), ClientKey);
            string MToSend = JsonConvert.SerializeObject(new { IpAddress = GetLocalIPAddress(), EncryptedMessage = message, Action = "PING" });
            string responseString = await Task.Run(() => TcpServer.TCP_SendData(ServerLocation, MToSend, ServerPort));
            return JsonConvert.DeserializeObject<PingResponse>(responseString);
        }

        public async Task Vote(Candidate candidate)
        {
            VoteObject vote = new VoteObject();
            vote.CandidateId = candidate.UniqueId;
            vote.BounceCount = (byte)new Random().Next(2, 7);
            vote.EncryptedVote = EncryptVote(String.Format("{0} Voted for {1}:{2}", Username, candidate.UniqueId, candidate.CandidateName));
            int tryCount = 0;
            int indexSend = 0;
            while(tryCount < 2 && ActiveUsers.Count > 0)
            {
                indexSend = new Random().Next(this.ActiveUsers.Count);
                try
                {
                    string res = await Task.Run(()=>TcpServer.TCP_SendData(ActiveUsers[indexSend], vote.ToString(), AppPort));
                    var resObj = JsonConvert.DeserializeAnonymousType(res, new { Error = false, Message = "" });
                    if (resObj.Error)
                    {
                        throw new Exception(resObj.Message);
                    }
                    return;
                }
                catch(Exception e)
                {
                    tryCount += 1;
                }
            }
            await Task.Run(()=>TcpServer.TCP_SendData(ServerLocation, vote.ToString(), ServerPort));
            
        }

        public byte[] EncryptVote(string message)
        {
            MemoryStream memoryStream = new MemoryStream();
            RijndaelManaged cryptoEngine = new RijndaelManaged();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoEngine.CreateEncryptor(ClientPrivateKey, ClientPrivateKey), CryptoStreamMode.Write);
            StreamWriter writer = new StreamWriter(cryptoStream);
            writer.Write(message);
            writer.Flush();
            return memoryStream.ToArray();
        }

        public class PingResponse
        {
            public List<string> ActiveUsers { get; set; }
            public bool Error { get; set; }
            public string Message { get; set; }
        }
        
    }
}
