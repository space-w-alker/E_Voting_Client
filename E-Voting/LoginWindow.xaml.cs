using E_Voting.Models;
using Newtonsoft.Json;
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
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private Client client;
        private bool result = false;


        public LoginWindow(Client client)
        {
            InitializeComponent();
            this.client = client;
            UsernameText.Text = "spacewalker";
            PasswordText.Password = "password";
            this.Closing += LoginWindow_Closing;
        }

        private void LoginWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = result;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {

            bool error = false;
            if (String.IsNullOrWhiteSpace(UsernameText.Text))
            {
                UsernameError.Content = "Username cannot be left Empty";
                error = true;
            }
            if (String.IsNullOrWhiteSpace(PasswordText.Password))
            {
                PasswordError.Content = "Password cannot be left Empty";
                error = true;
            }
            if (error) { return; }
            string response;

            try
            {
                response = await client.LoginInToServerAsync(UsernameText.Text, PasswordText.Password);
            }
            catch (Exception theError)
            {
                LoginStatus.Content = theError.Message;
                return;
            }

            var JsonResponse = JsonConvert.DeserializeObject<LoginResponse>(response);
            if (JsonResponse.Error)
            {
                LoginStatus.Content = JsonResponse.Message;
            }
            else
            {
                client.ClientKey = JsonResponse.Key;
                client.Username = UsernameText.Text;
                result = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
