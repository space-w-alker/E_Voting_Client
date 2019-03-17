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
        private List<CancellationTokenSource> cancellationTokenSources;

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
            SearchCandidates.SearchBox.SelectionChanged += SearchBox_SelectionChanged;
            SearchVoteFor.SearchBox.SelectionChanged += SearchBox_SelectionChanged1;
            string keyString = new System.Numerics.BigInteger(TheClient.ClientPrivateKey).ToString();
            PrivateKeyText.Text = String.Format("Private Key:   {0}", keyString);
            cancellationTokenSources = new List<CancellationTokenSource>();
            
        }

        private void SearchBox_SelectionChanged1(object sender, RoutedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(VoteForList.DataContext);
            string search_text = "";
            if (string.IsNullOrWhiteSpace(SearchVoteFor.SearchBox.Text)) view.Filter = null;
            else
            {
                search_text = SearchVoteFor.SearchBox.Text;
                view.Filter = candidate =>
                {
                    long long_search_text;
                    bool output = ((Candidate)candidate).CandidateName.IndexOf(search_text, StringComparison.InvariantCultureIgnoreCase) > -1;
                    if (output) return output;
                    if (Int64.TryParse(search_text, out long_search_text))
                    {
                        output = ((Candidate)candidate).UniqueId == long_search_text;
                    }
                    return output;
                };
            }
        }

        private void SearchBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(CandidateList.DataContext);
            string search_text = "";
            if (string.IsNullOrWhiteSpace(SearchCandidates.SearchBox.Text)) view.Filter = null;
            else
            {
                search_text = SearchCandidates.SearchBox.Text;
                view.Filter = candidate =>
                {
                    long long_search_text;
                    bool output = ((Candidate)candidate).CandidateName.IndexOf(search_text, StringComparison.InvariantCultureIgnoreCase) > -1;
                    if (output) return output;
                    if(Int64.TryParse(search_text,out long_search_text))
                    {
                        output = ((Candidate)candidate).UniqueId == long_search_text;
                    }
                    return output;
                };
            }
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


        public async void NotifyStatus(string Message, string Color, CancellationToken cancellation)
        {
            MainWindowStatus.Text = Message;
            MainWindowStatusBorder.Visibility = Visibility.Visible;
            MainWindowStatusBorder.Background = new BrushConverter().ConvertFromString(Color) as Brush;
            await Task.Run(() => Thread.Sleep(5000));
            if (!cancellation.IsCancellationRequested)
            {
                MainWindowStatusBorder.Visibility = Visibility.Collapsed;
            }
            cancellationTokenSources.RemoveAt(0);
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
                    candidate.Voted = true;
                    CancellationTokenSource c = new CancellationTokenSource();
                    if(cancellationTokenSources.Count > 0)
                    {
                        cancellationTokenSources[cancellationTokenSources.Count - 1].Cancel();
                    }
                    cancellationTokenSources.Add(c);
                    NotifyStatus(String.Format("Vote Sent For {0}", candidate.CandidateName), "#FF8AFF8A", c.Token);
                    return;
                }
            }
        }
    }
}
