using E_Voting.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace E_Voting.TCP
{
    class TcpServer
    {
        public static TcpListener server;

        public static void ServerRoutine(Client AppClient)
        {

            IPAddress localAddress = IPAddress.Parse(AppClient.GetLocalIPAddress());
            //server = new TcpListener(localAddress, AppClient.AppPort);
            server.Start();
            TcpClient client = null;
            while (true)
            {

                Console.WriteLine("Waiting for connection...");
                try
                {
                    client = server.AcceptTcpClient();
                }
                catch(SocketException e)
                {
                    if((e.ErrorCode == (int)SocketError.Interrupted))
                    {
                        return;
                    }
                }
                
                Thread thread = new Thread(() => ClientHandler(client, AppClient));
                thread.Start();
                Thread.Sleep(0);
            }
        }

        public static void ClientHandler(TcpClient client, Client AppClient)
        {
            string requestString = "";
            StreamReader reader = new StreamReader(new MemoryStream());
            try
            {
                NetworkStream networkStream = client.GetStream();
                reader = new StreamReader(networkStream);
                requestString = reader.ReadLine();
                using (StreamWriter writer = new StreamWriter(client.GetStream()))
                {
                    writer.WriteLine(JsonConvert.SerializeObject(new { Error = false, Message = "" }));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally { reader.Close();client.Close(); }

            VoteObject vote = JsonConvert.DeserializeObject<VoteObject>(requestString);
            vote.BounceCount -= 1;
            CycleVote(vote, AppClient);
            return;
        }

        public async static void CycleVote(VoteObject vote, Client AppClient)
        {
            if (vote.BounceCount > 0)
            {
                int tryCount = 0;
                while (tryCount < 5 && AppClient.ActiveUsers.Count>0)
                {
                    int indexSend = new Random().Next(AppClient.ActiveUsers.Count);
                    try
                    {
                        string res = await Task.Run(() => TcpServer.TCP_SendData(AppClient.ActiveUsers[indexSend], vote.ToString(), AppClient.AppPort));
                        var resObj = JsonConvert.DeserializeAnonymousType(res, new { Error = false, Message = "" });
                        if (resObj.Error)
                        {
                            throw new Exception(resObj.Message);
                        }
                        return;
                    }
                    catch (Exception e)
                    {
                        tryCount += 1;
                        Thread.Sleep(100 * tryCount);
                    }
                }
                await Task.Run(() => TCP_SendData(AppClient.ServerLocation, vote.ToString(), AppClient.ServerPort));
                return;
            }
            else
            {
                await Task.Run(() => TCP_SendData(AppClient.ServerLocation, vote.ToString(), AppClient.ServerPort));
            }
                    
        }

        public static async void PingServerPeriodic(TimeSpan interval, CancellationToken cancellationToken, Client TheClient)
        {
            while (true)
            {
                try
                {
                    await Task.Delay(interval, cancellationToken);
                }
                catch (TaskCanceledException e)
                {

                    return;
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                //ConnectionStatus.Text = "Establishing Anonymous Connection...";
                Client.PingResponse res = await TheClient.PingServer();
                TheClient.ActiveUsers = res.ActiveUsers;
            }
        }

        public static string TCP_SendData(string IpAddress, string content, Int32 port)
        {
            TcpClient tcpClient = null;
            try
            {
                tcpClient = new TcpClient(IpAddress, port);
            }
            catch(Exception e)
            {
                return JsonConvert.SerializeObject(new { Error = true, e.Message });
            }
            NetworkStream networkStream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            StreamReader reader = new StreamReader(tcpClient.GetStream());
            writer.WriteLine(content);
            writer.Flush();
            string response = reader.ReadLine();
            writer.Close();
            reader.Close();
            tcpClient.Close();
            return response;
        }
    }
}
