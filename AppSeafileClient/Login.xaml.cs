using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;
using Microsoft.Phone.Net.NetworkInformation;
using AppSeafileClient.Resources;
using Coding4Fun.Toolkit.Controls;
using Windows.Web.Http.Filters;
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Networking;
using System.Reflection;

namespace AppSeafileClient
{
    public partial class MainPage : PhoneApplicationPage
    {
        bool CertificateValidateByUser = false;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            if (GlobalVariables.IsDebugMode == true)
            {
                myPivoteItemHelp.toogle_debug_state = true;
            }
            else
            {
                myPivoteItemHelp.toogle_debug_state = false;
            }
            (App.Current.Resources["PhoneRadioCheckBoxCheckBrush"] as SolidColorBrush).Color = Colors.Black;

          
        }


        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            SystemTray.ProgressIndicator = new ProgressIndicator();

            if (GlobalVariables.IsolatedStorageUserInformations.Contains("tokensaved") && GlobalVariables.IsolatedStorageUserInformations.Contains("urlsaved"))
            {
            string t = GlobalVariables.IsolatedStorageUserInformations["tokensaved"] as string;
            string u = GlobalVariables.IsolatedStorageUserInformations["urlsaved"] as string;

            NavigationService.Navigate(new Uri("/Pages/ListLibraryPage.xaml?token=" + t + "&url=" + u, UriKind.Relative));
            }
        }

        /// <summary>
        /// Set ProgressIndicator status
        /// </summary>
        /// <param name="value">Value to display</param>
        public void SetProgressIndicator(bool value)
        {
            SystemTray.ProgressIndicator.IsIndeterminate = true;
            SystemTray.ProgressIndicator.IsVisible = value;
        }

        /// <summary>
        /// Validate user informations and launch login
        /// </summary>
        private void Button_Login(object sender, RoutedEventArgs e)
        {
            if (TextBox_url.Text.StartsWith("http://") || TextBox_url.Text.StartsWith("https://"))
            {         
                if ((TextBox_login.Text != "") && (PasswordBox_Password.Password != "") && (TextBox_url.Text != ""))
                {
                    SetProgressIndicator(true);
                    if (NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.None)
                    {
                        //No Internet
                        MessageBox.Show(AppResources.Login_Error_NoInternet_Content, AppResources.Login_Error_NoInternet_Title, MessageBoxButton.OK);
                    }
                    else
                    {
                        CertificateValidateByUser = false;
                        if (TextBox_url.Text.StartsWith("https://"))
                        {
                            validateCertificate();
                        }
                        else
                        {
                            loginRequestUser(TextBox_login.Text, PasswordBox_Password.Password, TextBox_url.Text, "auth-token");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show(AppResources.Login_Error_URL_NotValid_Content, AppResources.Login_Error_URL_NotValid_Title, MessageBoxButton.OK);
            }

        }

        public async void validateCertificate()
        {
            // Define some variables and set values
            StreamSocket clientSocket = new StreamSocket();

            System.Uri uri = new System.Uri(TextBox_url.Text);

            // get the port
            int port = uri.Port;

            // get the host name (my.domain.com)
            string host = uri.Host;

            // get the protocol
            string protocol = uri.Scheme;

            // get everything before the query:
            string cleanURL = uri.GetComponents(UriComponents.Host, UriFormat.UriEscaped);

            HostName serverHost = new HostName(cleanURL);
            string serverServiceName = "https";

            // Try to connect to contoso using HTTPS (port 443)
            try
            {

                // Call ConnectAsync method with SSL
                await clientSocket.ConnectAsync(serverHost, serverServiceName, SocketProtectionLevel.Tls12);

                Debug.WriteLine("Connected");

                loginRequestUser(TextBox_login.Text, PasswordBox_Password.Password, TextBox_url.Text, "auth-token");
        
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    if (GlobalVariables.IsDebugMode == true)
                    {
                        App.logger.log(LogLevel.critical, "Connect failed with error: " + exception.Message);
                    }
                }

                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.CertificateExpired)
                {
                   if (GlobalVariables.IsDebugMode == true)
                   {
                       App.logger.log(LogLevel.critical, "Connect failed with error: " + exception.Message);
                   }
                   if (MessageBox.Show(AppResources.Certificate_Error_Expired, AppResources.Certificate_Error_Title, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                   {
                       CertificateValidateByUser = true;
                   }
                }
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.CertificateRevoked)
                {
                   if (GlobalVariables.IsDebugMode == true)
                   {
                       App.logger.log(LogLevel.critical, "Connect failed with error: " + exception.Message);
                   }
                   if (MessageBox.Show(AppResources.Certificate_Error_Revoked, AppResources.Certificate_Error_Title, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                   {
                      CertificateValidateByUser = true;                     
                   }
                }
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.CertificateIsInvalid)
                {
                   if (GlobalVariables.IsDebugMode == true)
                   {
                        App.logger.log(LogLevel.critical, "Connect failed with error: " + exception.Message);
                   }
                   if (MessageBox.Show(AppResources.Certificate_Error_Invalid, AppResources.Certificate_Error_Title, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                   {
                       CertificateValidateByUser = true;   
                   }
                }
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.CertificateUntrustedRoot)
                {
                   if (GlobalVariables.IsDebugMode == true)
                   {
                       App.logger.log(LogLevel.critical, "Connect failed with error: " + exception.Message);
                   }
                   if (MessageBox.Show(AppResources.Certificate_Error_UntrustedRoot, AppResources.Certificate_Error_Title, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                   {
                       CertificateValidateByUser = true;   
                   }
                }
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.CertificateCommonNameIsIncorrect)
                {
                   if (GlobalVariables.IsDebugMode == true)
                   {
                       App.logger.log(LogLevel.critical, "Connect failed with error: " + exception.Message);
                   }
                   if( MessageBox.Show(AppResources.Certificate_Error_CNMissmatch, AppResources.Certificate_Error_Title, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                   {
                       CertificateValidateByUser = true;   
                   }
                }

                clientSocket.Dispose();
                clientSocket = null;

                if (CertificateValidateByUser == true)
                {
                    loginRequestUser(TextBox_login.Text, PasswordBox_Password.Password, TextBox_url.Text, "auth-token");
                }
                else
                {
                    SetProgressIndicator(false);
                    MessageBox.Show(AppResources.Login_Error_SSL_NotValidate_Content, AppResources.Login_Error_SSL_NotValidate_Title, MessageBoxButton.OK);
                }
            }
        }

     
        private async void loginRequestUser(string username, string password, string url, string type)
        {
            var filter = new HttpBaseProtocolFilter();
            Uri uristringLogin = new Uri(url + "/api2/" + type + "/");     

            // *******************
            // IGNORING CERTIFACTE PROBLEMS
            // *******************
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.IncompleteChain);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

            var HttpClientLogin = new HttpClient(filter);
            HttpMultipartFormDataContent requestContentLogin = new HttpMultipartFormDataContent();
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);

            HttpClientLogin.DefaultRequestHeaders.Add("Accept", "application/json;indent=4");
            HttpClientLogin.DefaultRequestHeaders.Add("User-agent", "CloudWaveX for Seafile/" + nameHelper.Version);

            var values = new[]
                    {
                        new KeyValuePair<string, string>("username", username),
                        new KeyValuePair<string, string>("password", password)
                    };

            foreach (var keyValuePair in values)
            {
                requestContentLogin.Add(new HttpStringContent(keyValuePair.Value), keyValuePair.Key);
            }

            try
            {
                HttpResponseMessage responseLogin = await HttpClientLogin.PostAsync(uristringLogin, requestContentLogin);
                responseLogin.EnsureSuccessStatusCode();

                loginDoLogin(responseLogin.Content.ToString());

                HttpClientLogin.Dispose();
            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "Error login : " + ex.ToString());
                    App.logger.log(LogLevel.critical, "URL login : " + uristringLogin);

                }

                MessageBox.Show(AppResources.Login_Error_WrongUser_Content, AppResources.Login_Error_WrongUser_Title, MessageBoxButton.OK);
              //  TextBlockErrorLogin.Text = "Please verify connection information.";

                SetProgressIndicator(false);
            }
        }

        private void loginDoLogin(string content)
        {
            var resultLogin = JsonConvert.DeserializeObject<LibraryRootObject>(content);

                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "Token : " + resultLogin.token);
                    App.logger.log(LogLevel.debug, "Login ok, token ok");
                }

                SetProgressIndicator(false);

                if (CheckBox_RememberMe.IsChecked == true)
                {
                    //User want remember me
                    if (!GlobalVariables.IsolatedStorageUserInformations.Contains("tokensaved") && !GlobalVariables.IsolatedStorageUserInformations.Contains("urlsaved"))
                    {
                        GlobalVariables.IsolatedStorageUserInformations.Add("tokensaved", resultLogin.token);
                        GlobalVariables.IsolatedStorageUserInformations.Add("urlsaved", TextBox_url.Text);
                    }
                    else
                    {
                        GlobalVariables.IsolatedStorageUserInformations["tokensaved"] = resultLogin.token;
                        GlobalVariables.IsolatedStorageUserInformations["urlsaved"] = TextBox_url.Text;
                    }
                    GlobalVariables.IsolatedStorageUserInformations.Save();
                    NavigationService.Navigate(new Uri("/Pages/ListLibraryPage.xaml?token=" + GlobalVariables.IsolatedStorageUserInformations["tokensaved"] as string + "&url=" + GlobalVariables.IsolatedStorageUserInformations["urlsaved"] as string, UriKind.Relative));
                }
                else
                {
                    NavigationService.Navigate(new Uri("/Pages/ListLibraryPage.xaml?token=" + resultLogin.token + "&url=" + TextBox_url.Text, UriKind.Relative));
                }
        }

    

        /// <summary>
        /// Checkbox for saving token information
        /// </summary>
        private void CheckBox_RememberMe_Click(object sender, RoutedEventArgs e)
        {
            //Login info already set, user want to delete login
            if (GlobalVariables.IsolatedStorageUserInformations.Contains("tokensaved") && GlobalVariables.IsolatedStorageUserInformations.Contains("urlsaved"))
            {
                GlobalVariables.IsolatedStorageUserInformations.Remove("tokensaved");
                GlobalVariables.IsolatedStorageUserInformations.Remove("urlsaved");
                TextBox_login.Text = "";
                TextBox_url.Text = "";
                PasswordBox_Password.Password = "";
                TextBox_login.IsEnabled = true;
                TextBox_url.IsEnabled = true;
                PasswordBox_Password.IsEnabled = true;
            }
        } 
    }
}