using E_Voting.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TCP_Election_Server;

namespace E_Voting
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<Category> Categories { get; set; }
        public ObservableCollection<Candidate> Candidates { get; set; }
        public ObservableCollection<Candidate> VoteFor { get; set; }
        private TcpListener ClientServer { get; set; }

        public enum States { INIT, NONE, CONNECTED }
        private States state;
        public States State
        {
            get { return state; }
            set
            {
                state = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("State"));
            }
        }

        private Client TheClient;
        private LoginWindow loginWindow;
        private CancellationTokenSource CancelPing;

        public MainWindow(Client myClient)
        {
            TheClient = myClient;
            InitializeComponent();

            IPAddress localAddress = IPAddress.Parse(TheClient.GetLocalIPAddress());
            ClientServer = new TcpListener(localAddress, TheClient.AppPort);
            TCP.TcpServer.server = ClientServer;
            Thread serverThread = new Thread(()=>TCP.TcpServer.ServerRoutine(TheClient));
            serverThread.Start();
            this.Closing += MainWindow_Closing;
            TheClient.PropertyChanged += TheClient_PropertyChanged;
            CancelPing = new CancellationTokenSource();
            TCP.TcpServer.PingServerPeriodic(TimeSpan.FromSeconds(10), CancelPing.Token, TheClient);
            Categories = new ObservableCollection<Category>(Connection.LoadData());
            VoteFor = new ObservableCollection<Candidate>();
            Categories.Add(new Category("The King"));
            CategoryList.ItemsSource = Categories;
            CategoryList.DataContext = Categories;
            CategoryList.SelectionChanged += CategoryList_SelectionChanged;
            CategoryList.SelectedIndex = 0;
            VoteForList.DataContext = VoteFor;
            VoteForList.ItemsSource = VoteFor;
            UsernameText.Text = TheClient.Username;
            string keyString = new System.Numerics.BigInteger(TheClient.ClientPrivateKey).ToString();
            PrivateKeyText.Text = String.Format("Private Key:   {0}", keyString);
            
        }

        private void TheClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (TheClient.ActiveUsers.Count > 0)
            {
                ConnectionStatus.Text = "Anonymous Connection Established";
                ConnectionStatus.Background = (Brush)new BrushConverter().ConvertFromString("Green");
            }
            else
            {
                ConnectionStatus.Text = "Connection Not Anonymous";
                ConnectionStatus.Background = (Brush)new BrushConverter().ConvertFromString("Red");
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            ClientServer.Stop();
            CancelPing.Cancel();
        }

        

        private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Category cat = (Category)CategoryList.SelectedItem;
            Candidates = cat.Candidates;
            CandidateList.ItemsSource = Candidates;
            CandidateList.DataContext = Candidates;
        }



        



        private void SelectCandidate_Click(object sender, RoutedEventArgs e)
        {
            Button Sender = (Button)sender;
            long Id = (long)Sender.Tag;
            Category selectedCategory = (Category)CategoryList.SelectedItem;
            if(selectedCategory.CountSelected > selectedCategory.MaxVote - 1)
            {
                new Views.ErrorWindow("Maximum number of candidates selected!!!").ShowDialog();
                return;
            }
            foreach (Candidate candidate in Candidates)
            {
                if (candidate.UniqueId == Id)
                {
                    if (candidate.Selected) break;
                    VoteFor.Add(candidate);
                    candidate.Selected = true;
                    selectedCategory.CountSelected += 1;
                }
            }
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            Button Sender = (Button)sender;
            long Id = (long)Sender.Tag;
            Category selectedCategory = null;
            foreach (Candidate candidate in VoteFor)
            {
                if (candidate.UniqueId == Id)
                {
                    foreach(Category category in Categories)
                    {
                        if (category.Id == candidate.CatId)
                        {
                            selectedCategory = category;
                        }
                    }
                    VoteFor.Remove(candidate);
                    candidate.Selected = false;
                    selectedCategory.CountSelected -= 1;
                    break;
                }
            }
        }

        private void VoteSelected_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            foreach(Candidate candidate in VoteFor)
            {
                if(candidate.UniqueId == (long)button.Tag)
                {
                    VoteFor.Remove(candidate);
                    TheClient.Vote(candidate);
                    return;
                }
            }
        }
    }
}
