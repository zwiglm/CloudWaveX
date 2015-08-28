using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Diagnostics;
using Newtonsoft.Json;
using Coding4Fun.Toolkit.Controls;
using System.Windows.Media;
using Windows.Storage;
using System.Collections.ObjectModel;
using Windows.Web.Http.Filters;
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http;

using AppSeafileClient.Resources;
using System.IO.IsolatedStorage;
using System.Globalization;
using System.Threading;
using Coding4Fun.Toolkit.Controls.Common;
using System.Reflection;

namespace AppSeafileClient.Pages
{
    public partial class ListLibraryPage : PhoneApplicationPage
    {
        string authorizationToken = "";
        string address = "";
        Uri uristringGetLibrary = null;
        Uri uristringGetInfos = null;
        Uri uristringRequestLibrary = null;

        PasswordInputPrompt passwordInputLibrary;

        private const string MSG_VERSION_URL = "http://wp.sgir.ch/messages/{1}/message.{0}.txt";
        private const string MSG_URL = "http://wp.sgir.ch/messages/message.{0}.txt";

        private static readonly Guid _cacheInvalidator = Guid.NewGuid();
        private bool _isVersionedMessageChecked = false;



        public ListLibraryPage()
        {
            InitializeComponent();

        }

      


        #region |pie chart|
        public class PData 
        { 
            public string title { get; set; } 
            public double value { get; set; } 
        } 
        #endregion 
    

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string token = "";
            string url = "";
            SystemTray.ProgressIndicator = new ProgressIndicator();

        
            if (GlobalVariables.IsDebugMode == true)
            {
                myPivoteItemHelp.toogle_debug_state = true;
            }
            else
            {
                myPivoteItemHelp.toogle_debug_state = false;
            }


            if (NavigationContext.QueryString.TryGetValue("token", out token))
            {
                authorizationToken = token;
            }

            if (NavigationContext.QueryString.TryGetValue("url", out url))
            {
                address = url;
            }


            //Get Library
            requestLibrary(authorizationToken, address, "repos");

            //Get accounts infos
            requestAccountInfos(authorizationToken, address, "account/info");
        }

        public async void requestAccountInfos(string token, string url, string type)
        {
            var filter = new HttpBaseProtocolFilter();
            Uri uristringGetAccountInfos = new Uri(url + "/api2/" + type + "/");

            // *******************
            // IGNORING CERTIFACTE PROBLEMS
            // *******************
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.IncompleteChain);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

            var HttpClientAccountInfos = new HttpClient(filter);
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            HttpMultipartFormDataContent requestContentLogin = new HttpMultipartFormDataContent();

            HttpClientAccountInfos.DefaultRequestHeaders.Add("Accept", "application/json;indent=4");
            HttpClientAccountInfos.DefaultRequestHeaders.Add("Authorization", "token " + token);
            HttpClientAccountInfos.DefaultRequestHeaders.Add("User-agent", "CloudWave for Seafile/" + nameHelper.Version);

            try
            {
                HttpResponseMessage responseAccountInfos = await HttpClientAccountInfos.GetAsync(uristringGetAccountInfos);
                responseAccountInfos.EnsureSuccessStatusCode();

                displayAccountInformations(responseAccountInfos.Content.ToString());

                HttpClientAccountInfos.Dispose();
            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "Download account info Exception err" + ex);
                    App.logger.log(LogLevel.critical, "Download account info uristringGetAccountInfos : " + uristringGetAccountInfos.ToString());
                    App.logger.log(LogLevel.critical, "Download account info informations address : " + address);

                }

                AccountInfosUsage.Text = AppResources.ListLibrary_Error_AccountInfos;
                AccountInfosQuotaStatus.Text = "";
            }

        }

        private void displayAccountInformations(string p)
        {
            var resultAccountInfos = JsonConvert.DeserializeObject<LibraryRootObject>(p);

            if (GlobalVariables.IsDebugMode == true)
            {
                App.logger.log(LogLevel.debug, "Get info account OK");
                App.logger.log(LogLevel.debug, "usage : " + resultAccountInfos.usage);
                App.logger.log(LogLevel.debug, "total : " + resultAccountInfos.total);
                App.logger.log(LogLevel.debug, "email : " + resultAccountInfos.email);

            }

            if (resultAccountInfos.total > 0)
            {
                double c = (resultAccountInfos.usage * 100) / resultAccountInfos.total;

                GlobalVariables.AccountInfosUsage = c;
                GlobalVariables.AccountInfosTotalSpace = 100 - c;

                ObservableCollection<PData> dataPieChart = new ObservableCollection<PData>()
                                                            { 
                                                                new PData() { title = AppResources.ListLibrary_Pie_Title_1, value = GlobalVariables.AccountInfosUsage  }, 
                                                                new PData() { title = AppResources.ListLibrary_Pie_Title_2, value = GlobalVariables.AccountInfosTotalSpace}
                                                            };
                AccountInfosPieChart.DataSource = dataPieChart;

                AccountInfosUsage.Text = AppResources.ListLibrary_Account_1 + BytesToString(resultAccountInfos.usage) + AppResources.ListLibrary_Account_2 + GlobalVariables.AccountInfosUsage + AppResources.ListLibrary_Account_3;
                AccountInfosQuotaStatus.Text = AppResources.ListLibrary_Quota_Status_Enabled;
            }
            else
            {
                ObservableCollection<PData> dataPieChart = new ObservableCollection<PData>()
                                                            { 
                                                                new PData() { title = AppResources.ListLibrary_Pie_Title_3, value = 100  }, 
                                                            };
                AccountInfosPieChart.DataSource = dataPieChart;

                AccountInfosUsage.Text = AppResources.ListLibrary_Account_1 + BytesToString(resultAccountInfos.usage) + AppResources.ListLibrary_Account_4;
                AccountInfosQuotaStatus.Text = AppResources.ListLibrary_Quota_Status_Disabled;
            }
        }

        public async void requestLibrary(string token, string url, string type)
        {
            var filter = new HttpBaseProtocolFilter();
            uristringRequestLibrary = new Uri(url + "/api2/" + type + "/");

            // *******************
            // IGNORING CERTIFACTE PROBLEMS
            // *******************
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.IncompleteChain);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

            var HttpClientGetLibrary = new HttpClient(filter);
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);   
            HttpMultipartFormDataContent requestContentLogin = new HttpMultipartFormDataContent();

            HttpClientGetLibrary.DefaultRequestHeaders.Add("Accept", "application/json;indent=4");
            HttpClientGetLibrary.DefaultRequestHeaders.Add("Authorization", "token " + token);
            HttpClientGetLibrary.DefaultRequestHeaders.Add("User-agent", "CloudWave for Seafile/" + nameHelper.Version);

            try
            {
                SetProgressIndicator(true);
                HttpResponseMessage responseRequestLibrary = await HttpClientGetLibrary.GetAsync(uristringRequestLibrary);

                displayListLibrary(responseRequestLibrary.Content.ToString());

                HttpClientGetLibrary.Dispose();
            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "Download list library Exception err" + ex);

                    App.logger.log(LogLevel.critical, "Download list library uristringRequestLibrary : " + uristringRequestLibrary.ToString());
                    App.logger.log(LogLevel.critical, "Download list library informations address : " + address);

                }
                MessageBox.Show(AppResources.ListLibrary_Error_ListLibrary_Content, AppResources.ListLibrary_Error_ListLibrary_Title, MessageBoxButton.OK);
            }
        }

        private void displayListLibrary(string p)
        {
            var resultLibrary = JsonConvert.DeserializeObject<List<LibraryRootObject>>(p);
            if (resultLibrary.Count == 0)
            {
                //listBoxAllLibraries.Items.Add(new LibraryRootObject() { name = "Lib2", desc = "lib 2 public", type = "repo", encrypted = false, size = 100 });
                SetProgressIndicator(false);
            }
            else
            {
                SetProgressIndicator(false);

                listBoxAllLibraries.ItemsSource = resultLibrary;
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "List library OK");
                }
            }

        }

        /// <summary>
        /// Convert byte to string
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        static String BytesToString(long i)
        {
            string sign = (i < 0 ? "-" : "");
            double readable = (i < 0 ? -i : i);
            string suffix;
            if (i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (double)(i >> 50);
            }
            else if (i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (double)(i >> 40);
            }
            else if (i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (double)(i >> 30);
            }
            else if (i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (double)(i >> 20);
            }
            else if (i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (double)(i >> 10);
            }
            else if (i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = (double)i;
            }
            else
            {
                return i.ToString(sign + "0 B"); // Byte
            }
            readable /= 1024;

            return sign + readable.ToString("0.### ") + suffix;
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
        /// Occurs when click on an item in a ListBox
        /// </summary>
        private void listAllLibraries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //On récupère la listbox qui a levé l'évènement
            ListBox lst = sender as ListBox;

            if (lst.SelectedIndex != -1)
            {
                //On récupère notre objet métier
                LibraryRootObject lib = lst.SelectedItem as LibraryRootObject;

                //On annule la sélection
                lst.SelectedIndex = -1;

                GlobalVariables.currentLibrary = lib.id;

                if (lib.encrypted == true)
                {
                    SetProgressIndicator(true);

                     //Go to ContentLibraryPage with library password
                    passwordInputLibrary = new PasswordInputPrompt
                    {
                        Title = AppResources.ListLibrary_LibCrypt_PasswordInput_Title,
                        Message = AppResources.ListLibrary_LibCrypt_PasswordInput_Message,
                        IsCancelVisible = true,
                        Overlay = new SolidColorBrush(Color.FromArgb(150,160,160,160))
                    };

                    passwordInputLibrary.Completed += input_Completed;
                    passwordInputLibrary.Show();

                }
                else
                {
                    //Go to ContentLibraryPage
                    NavigationService.Navigate(new Uri("/Pages/ContentLibraryPage.xaml?token=" + authorizationToken + "&url=" + address + "&idlibrary=" + GlobalVariables.currentLibrary, UriKind.Relative));
                }
            
            }
        }

        /// <summary>
        /// Occurs when user click OK on the password library popup
        /// </summary>
        private void input_Completed(object sender, PopUpEventArgs<string, PopUpResult> e)
        {
            if (!String.IsNullOrEmpty(e.Result))
            {
                GlobalVariables.currentLibraryPassword = e.Result;
                decryptLibrary();
                SetProgressIndicator(false);
             }
        }

        /// <summary>
        /// Send password to decrypt the crypted library
        /// </summary>
        public async void decryptLibrary()
        {
            var filter = new HttpBaseProtocolFilter();
            string t = uristringRequestLibrary + GlobalVariables.currentLibrary + "/";

            Uri a = new Uri(t);

            // *******************
            // IGNORING CERTIFACTE PROBLEMS
            // *******************
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.IncompleteChain);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

            var HttpClientDecryptLibrary = new HttpClient(filter);
            HttpMultipartFormDataContent requestDecryptLibrary = new HttpMultipartFormDataContent();

            HttpClientDecryptLibrary.DefaultRequestHeaders.Add("Accept", "application/json;indent=4");
            HttpClientDecryptLibrary.DefaultRequestHeaders.Add("Authorization", "token " + authorizationToken);


            var values = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("password", GlobalVariables.currentLibraryPassword)
                    };

            foreach (var keyValuePair in values)
            {
                requestDecryptLibrary.Add(new HttpStringContent(keyValuePair.Value), keyValuePair.Key);
            }

            try
            {
                HttpResponseMessage responseDecryptLibrary = await HttpClientDecryptLibrary.PostAsync(a, requestDecryptLibrary);
                responseDecryptLibrary.EnsureSuccessStatusCode();

                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "Decrypt library OK");
                }
                NavigationService.Navigate(new Uri("/Pages/ContentLibraryPage.xaml?token=" + authorizationToken + "&url=" + address + "&idlibrary=" + GlobalVariables.currentLibrary, UriKind.Relative));
         
            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "The library password is wrong");
                    App.logger.log(LogLevel.critical, "Exception : " + ex);
                }
                MessageBox.Show(AppResources.ListLibrary_LibCrypt_Error_Content, AppResources.ListLibrary_LibCrypt_Error_Title, MessageBoxButton.OK);
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (GlobalVariables.IsolatedStorageUserInformations.Contains("tokensaved") && GlobalVariables.IsolatedStorageUserInformations.Contains("urlsaved"))
            {
                e.Cancel = true;
                Application.Current.Terminate();
            }
    
        }
    }
}