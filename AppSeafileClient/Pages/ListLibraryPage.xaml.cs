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
using AppSeafileClient.Domain;
using System.Threading.Tasks;

namespace AppSeafileClient.Pages
{
    public partial class ListLibraryPage : PhoneApplicationPage
    {
        string authorizationToken = "";
        string address = "";
        Uri uristringRequestLibrary = null;

        PasswordInputPrompt passwordInputLibrary;

        private static readonly Guid _cacheInvalidator = Guid.NewGuid();


        public ListLibraryPage()
        {
            InitializeComponent();
        }
     

        #region Pie-Chart
        public class PData 
        { 
            public string title { get; set; } 
            public double value { get; set; } 
        } 
        #endregion 
    

        protected override async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
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
            var foundLibs = await requestLibrary(authorizationToken, address, GlobalVariables.SF_REQ_REPOS);
            List<LibraryRootObject> mainLibsSource = this.filterMainLibs(foundLibs.Content.ToString());
            displayListLibrary(mainLibsSource);

            //Get accounts infos
            requestAccountInfos(authorizationToken, address, "account/info");
        }


        private async void requestAccountInfos(string token, string url, string type)
        {
            var filter = HttpHelperFactory.Instance.getHttpFilter();

            Uri uristringGetAccountInfos = new Uri(url + "/api2/" + type + "/");
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

                AccountInfosUsage.Text = AppResources.ListLibrary_Account_1 + CloudHelper.bytesToString(resultAccountInfos.usage) + AppResources.ListLibrary_Account_2 + GlobalVariables.AccountInfosUsage + AppResources.ListLibrary_Account_3;
                AccountInfosQuotaStatus.Text = AppResources.ListLibrary_Quota_Status_Enabled;
            }
            else
            {
                ObservableCollection<PData> dataPieChart = new ObservableCollection<PData>()
                                                            { 
                                                                new PData() { title = AppResources.ListLibrary_Pie_Title_3, value = 100  }, 
                                                            };
                AccountInfosPieChart.DataSource = dataPieChart;

                AccountInfosUsage.Text = AppResources.ListLibrary_Account_1 + CloudHelper.bytesToString(resultAccountInfos.usage) + AppResources.ListLibrary_Account_4;
                AccountInfosQuotaStatus.Text = AppResources.ListLibrary_Quota_Status_Disabled;
            }
        }


        private async Task<HttpResponseMessage> requestLibrary(string token, string url, string type)
        {
            var filter = HttpHelperFactory.Instance.getHttpFilter();
            uristringRequestLibrary = new Uri(url + "/api2/" + type + "/");
            var HttpClientGetLibrary = new HttpClient(filter);
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);   

            HttpClientGetLibrary.DefaultRequestHeaders.Add("Accept", "application/json;indent=4");
            HttpClientGetLibrary.DefaultRequestHeaders.Add("Authorization", "token " + token);
            HttpClientGetLibrary.DefaultRequestHeaders.Add("User-agent", "CloudWave for Seafile/" + nameHelper.Version);

            HttpResponseMessage libsResponse = null;
            try
            {
                SetProgressIndicator(true);
                libsResponse = await HttpClientGetLibrary.GetAsync(uristringRequestLibrary);
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
            return libsResponse;
        }

        private List<LibraryRootObject> filterMainLibs(string httpResponse)
        {
            List<LibraryRootObject> resultLibrary = JsonConvert.DeserializeObject<List<LibraryRootObject>>(httpResponse);
            resultLibrary.RemoveAll(q => q.@virtual);
            resultLibrary.RemoveAll(q => (!string.IsNullOrEmpty(q.type) && q.type.Equals(GlobalVariables.SF_RESP_GROUP_REPOS)));
            return resultLibrary;
        }

        private void displayListLibrary(List<LibraryRootObject> mainLibsSource)
        {
            if (mainLibsSource.Count == 0)
            {
                SetProgressIndicator(false);
            }
            else
            {
                SetProgressIndicator(false);

                listBoxAllLibraries.ItemsSource = mainLibsSource;
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "List library OK");
                }
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
            var filter = HttpHelperFactory.Instance.getHttpFilter();
            string t = uristringRequestLibrary + GlobalVariables.currentLibrary + "/";
            Uri a = new Uri(t);

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
            if (GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.TOKEN_SAVED_SET) && GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.URL_SAVED_SET))
            {
                e.Cancel = true;
                Application.Current.Terminate();
            }
    
        }
    }
}