using E_Voting.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace E_Voting
{
    /// <summary>
    /// Interaction logic for LandinWindow.xaml
    /// </summary>
    public partial class LandinWindow : Window
    {
        private Client TheClient = new Client();
        public LandinWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            bool? hey = new LoginWindow(TheClient).ShowDialog();
            if ((bool)!hey)
            {
                Close();
                return;
            }
            hey = new MainWindow(TheClient).ShowDialog();
        }
    }
}
