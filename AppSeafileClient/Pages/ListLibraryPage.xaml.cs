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

using PlasticWonderland.Resources;
using System.IO.IsolatedStorage;
using System.Globalization;
using System.Threading;
using Coding4Fun.Toolkit.Controls.Common;
using System.Reflection;
using PlasticWonderland.Domain;
using System.Threading.Tasks;
using System.Collections;
using SeaShoreShared;
using SeaShoreShared.DataBase;
using Windows.Networking.BackgroundTransfer;
using Windows.Web;

namespace PlasticWonderland.Pages
{
    public partial class ListLibraryPage : PhoneApplicationPage
    {
        private string authorizationToken = "";
        private string address = "";
        private Uri currentRequestUri = null;
        private Uri currentDecryptUri = null;

        PasswordInputPrompt passwordInputLibrary;

        private static readonly Guid _cacheInvalidator = Guid.NewGuid();

        private CancellationTokenSource _bgPhotoUploadCTS;

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

            // SET decrypt Uri
            currentDecryptUri = new Uri(address + "/api2/" + GlobalVariables.SF_REQ_REPOS + "/");

            // Get Main-Libraries
            var foundLibs = await requestLibrary(authorizationToken, address, GlobalVariables.SF_REQ_REPOS);
            List<LibraryRootObject> mainLibsSource = this.filterMainLibs(foundLibs.Content.ToString());
            // Get Be-Shared-Libraries
            //var beSharedLibs = await requestLibrary(authorizationToken, address, GlobalVariables.SF_REQ_BE_SHARED_REPOS);
            //List<LibraryRootObject> beSharedLibsSourece = this.filterBeSharedLibs(beSharedLibs.Content.ToString());
            List<LibraryRootObject> beSharedLibsSourece = this.filterBeSharedLibs(foundLibs.Content.ToString());
            // put to list
            displayListMyLibs(mainLibsSource, beSharedLibsSourece);

            // Get accounts infos
            requestAccountInfos(authorizationToken, address, "account/info");

            // handle pseudo-auto-backup....
            this.handlePhotoBackup(mainLibsSource, authorizationToken, address);
        }


        #region Take care about Photo-Upload
        // -------------------------------------------------------------------------------

        private async void handlePhotoBackup(List<LibraryRootObject> mainLibsSource, string authToken, string url)
        {
            // anyway: only when upload is enabled....
            if (TaskHelperFactory.Instance.isAgentEnabled())
            {
                LibraryRootObject photoBackupLibrary = HttpHelperFactory.Instance.photoBackupLibraryExists(mainLibsSource);
                string repoId = null;
                if (photoBackupLibrary == null)
                {
                    photoBackupLibrary = await HttpHelperFactory.Instance.createPhotoBackupLibary(authToken, url);
                    if (photoBackupLibrary != null)
                    {
                        repoId = photoBackupLibrary.repo_id;
                    }
                }
                else
                {
                    repoId = photoBackupLibrary.id;
                }

                if (photoBackupLibrary != null)
                {
                    // get Upload-Link
                    PhotoUploadWrapper uploadLink = 
                        await HttpHelperFactory.Instance.getUplUpdLink(authToken, url, repoId, SharedGlobalVars.UPLOAD_LINK_QUALI);

                    // get Update-Link
                    PhotoUploadWrapper updateLink =
                        await HttpHelperFactory.Instance.getUplUpdLink(authToken, url, repoId, SharedGlobalVars.UPDATE_LINK_QUALI);

                    if (updateLink.HttpResponseState == HttpStatusCode.InternalServerError || updateLink.HttpResponseState == HttpStatusCode.InternalServerError)
                        SharedHelperFactory.Instance.showToast(
                            AppResources.Background_Upload_Task,
                            AppResources.Background_Upload_QuotaError,
                            "");
                    else
                        this.putPhotosToQueue(uploadLink, updateLink, authToken);
                }
            }
        }

        private string makeSeashoreParentDir(LibraryBaseEntry item)
        {
            return item.CutPath.Replace(item.FileName, "").Replace(@"\\", @"\").Replace(@"\", "/");
        }
        /// <summary>
        /// 440 Invalid filename --> cant upload
        /// 
        /// 441 File already exists --> try again
        /// 500 Internal server error --> try again
        /// 
        /// </summary>
        /// <param name="uploadUrl"></param>
        private void putPhotosToQueue(PhotoUploadWrapper uploadUrl, PhotoUploadWrapper updateUrl, string authToken)
        {
            IList<LibraryBaseEntry> libBaseForUpload = SharedDbFactory.Instance.getForUpload().Values.ToList();
            //this.putUploadsToQueue(uploadUrl, libBaseForUpload, authToken);
            IList<LibraryBaseEntry> libBaseForUpdate = SharedDbFactory.Instance.getForUpdate().Values.ToList();
            //this.putUpdatesToQueue(updateUrl, libBaseForUpdate, authToken);
        }
        private async void putUploadsToQueue(PhotoUploadWrapper upload, IList<LibraryBaseEntry> entsForUpload, string authToken)
        {
            int testRunner = 0;
            foreach (LibraryBaseEntry item in entsForUpload)
            {
                testRunner++;
                StorageFile sFile = await StorageFile.GetFileFromPathAsync(item.FullPath);


                List<BackgroundTransferContentPart> parts = new List<BackgroundTransferContentPart>();
                BackgroundTransferContentPart partFile = new BackgroundTransferContentPart(item.FileName, item.FullPath);
                partFile.SetFile(sFile);
                parts.Add(partFile);
                BackgroundTransferContentPart partFileName = new BackgroundTransferContentPart(item.FileName, item.FullPath);
                partFileName.SetText(String.Format("filename={0}", item.FileName));
                parts.Add(partFileName);
                BackgroundTransferContentPart partParentDir = new BackgroundTransferContentPart(item.FileName, item.FullPath);
                string parent_dir = this.makeSeashoreParentDir(item);
                partParentDir.SetText(String.Format("parent_dir={0}", parent_dir));
                parts.Add(partParentDir);

                BackgroundUploader uploader = new BackgroundUploader();
                uploader.SetRequestHeader("Authorization", String.Format("Token {0}", authToken));
                uploader.Method = "POST";
                Uri upldUri = new Uri(upload.UplUpdLink);
                UploadOperation uploadOp = await uploader.CreateUploadAsync(upldUri, parts);

                // Attach progress and completion handlers.
                await handleBgPhotoUploadAsync(uploadOp, true);


                if (testRunner == 4)
                    break;
            }
        }
        private async void putUpdatesToQueue(PhotoUploadWrapper update, IList<LibraryBaseEntry> entsForUpload, string authToken)
        {
            foreach (var item in entsForUpload)
            {
                StorageFile sFile = await StorageFile.GetFileFromPathAsync(item.FullPath);
            }
        }


        private async Task handleBgPhotoUploadAsync(UploadOperation upload, bool start)
        {
            try
            {
                Progress<UploadOperation> progressCallback = new Progress<UploadOperation>(photoUploadAsyncProgress);
                if (start)
                {
                    // Start the upload and attach a progress handler.
                    await upload.StartAsync().AsTask(_bgPhotoUploadCTS.Token, progressCallback);
                }
                else
                {
                    // The upload was already running when the application started, re-attach the progress handler.
                    await upload.AttachAsync().AsTask(_bgPhotoUploadCTS.Token, progressCallback);
                }

                ResponseInformation response = upload.GetResponseInformation();
                string dummy = "here";
                switch (response.StatusCode)
                {
                    case 400:
                        dummy = "here";
                        break;
                    case 440:
                        dummy = "here";
                        break;
                    case 441:
                        dummy = "here";
                        break;
                    case 500:
                        dummy = "here";
                        break;
                    default:
                        break;
                }
            }
            catch (TaskCanceledException cncEx)
            {
            }
            catch (Exception ex)
            {
            }
        }

        private void photoUploadAsyncProgress(UploadOperation upload)
        {
            BackgroundUploadProgress progress = upload.Progress;

            if (progress.Status == BackgroundTransferStatus.Completed)
            {
                // MaZ todo: write DB...
                string dummy = "here";
            }
            if (progress.Status == BackgroundTransferStatus.Error)
            {
                // MaZ todo: write DB...
                string dummy = "here";
            }

            double percentSent = 100;
            if (progress.TotalBytesToSend > 0)
            {
                percentSent = progress.BytesSent * 100 / progress.TotalBytesToSend;
            }

            if (progress.HasRestarted)
            {
            }

            if (progress.HasResponseChanged)
            {
                // We've received new response headers from the server.

                // If you want to stream the response data this is a good time to start.
                // upload.GetResultStreamAt(0);
            }
        }

        private async Task DiscoverActiveUploadsAsync()
        {
            IReadOnlyList<UploadOperation> uploads = null;
            try
            {
                uploads = await BackgroundUploader.GetCurrentUploadsAsync();
            }
            catch (Exception ex)
            {
                if (!isBgPhotoUploadExceptionHandled("Discovery error", ex))
                {
                    throw;
                }
                return;
            }

            if (uploads.Count > 0)
            {
                List<Task> tasks = new List<Task>();
                foreach (UploadOperation upload in uploads)
                {
                    //Log(String.Format(CultureInfo.CurrentCulture, "Discovered background upload: {0}, Status: {1}",
                    //    upload.Guid, upload.Progress.Status));

                    // Attach progress and completion handlers.
                    tasks.Add(handleBgPhotoUploadAsync(upload, false));
                }

                // Don't await HandleUploadAsync() in the foreach loop since we would attach to the second
                // upload only when the first one completed; attach to the third upload when the second one
                // completes etc. We want to attach to all uploads immediately.
                // If there are actions that need to be taken once uploads complete, await tasks here, outside
                // the loop.
                await Task.WhenAll(tasks);
            }
        }

        private bool isBgPhotoUploadExceptionHandled(string title, Exception ex, UploadOperation upload = null)
        {
            WebErrorStatus error = BackgroundTransferError.GetStatus(ex.HResult);
            if (error == WebErrorStatus.Unknown)
            {
                return false;
            }

            if (upload == null)
            {
                //LogStatus(String.Format(CultureInfo.CurrentCulture, "Error: {0}: {1}", title, error),
                //    NotifyType.ErrorMessage);
            }
            else
            {
                //LogStatus(String.Format(CultureInfo.CurrentCulture, "Error: {0} - {1}: {2}", upload.Guid, title,
                //    error), NotifyType.ErrorMessage);
            }

            return true;
        }

        // -------------------------------------------------------------------------------
        #endregion


        private async void requestAccountInfos(string token, string url, string type)
        {
            var filter = HttpHelperFactory.Instance.getHttpFilter();

            Uri uristringGetAccountInfos = new Uri(url + "/api2/" + type + "/");
            var HttpClientAccountInfos = new HttpClient(filter);
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            HttpMultipartFormDataContent requestContentLogin = new HttpMultipartFormDataContent();

            HttpClientAccountInfos.DefaultRequestHeaders.Add("Accept", "application/json;indent=4");
            HttpClientAccountInfos.DefaultRequestHeaders.Add("Authorization", "token " + token);
            HttpClientAccountInfos.DefaultRequestHeaders.Add("User-agent", GlobalVariables.WEB_CLIENT_AGENT + nameHelper.Version);

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
            currentRequestUri = new Uri(url + "/api2/" + type + "/");
            var HttpClientGetLibrary = new HttpClient(filter);
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);   

            HttpClientGetLibrary.DefaultRequestHeaders.Add("Accept", "application/json;indent=4");
            HttpClientGetLibrary.DefaultRequestHeaders.Add("Authorization", "token " + token);
            HttpClientGetLibrary.DefaultRequestHeaders.Add("User-agent", GlobalVariables.WEB_CLIENT_AGENT + nameHelper.Version);

            HttpResponseMessage libsResponse = null;
            try
            {
                SetProgressIndicator(true);
                libsResponse = await HttpClientGetLibrary.GetAsync(currentRequestUri);
                HttpClientGetLibrary.Dispose();
            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "Download list library Exception err" + ex);
                    App.logger.log(LogLevel.critical, "Download list library uristringRequestLibrary : " + currentRequestUri.ToString());
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
            resultLibrary.RemoveAll(q => (!string.IsNullOrEmpty(q.type) && q.type.Equals(GlobalVariables.SF_RESP_SHARED_REPOS)));
            resultLibrary.RemoveAll(q => (!string.IsNullOrEmpty(q.type) && q.type.Equals(GlobalVariables.SF_RESP_GROUP_REPOS)));
            return resultLibrary;
        }
        //private List<LibraryRootObject> filterBeSharedLibs(string httpResponse)
        //{
        //    List<LibraryRootObject> resultLibrary = JsonConvert.DeserializeObject<List<LibraryRootObject>>(httpResponse);
        //    foreach (var item in resultLibrary)
        //        item.type = GlobalVariables.SHARED_REPO_HELPER;
        //    return resultLibrary;
        //}
        private List<LibraryRootObject> filterBeSharedLibs(string httpResponse)
        {
            List<LibraryRootObject> resultLibrary = JsonConvert.DeserializeObject<List<LibraryRootObject>>(httpResponse);
            resultLibrary.RemoveAll(q => (!string.IsNullOrEmpty(q.type) && q.type.Equals(GlobalVariables.SF_RESP_REPOS)));
            foreach (var item in resultLibrary)
                item.type = GlobalVariables.SHARED_REPO_HELPER;

            IEnumerable<IGrouping<string, LibraryRootObject>> bsLibGroups = resultLibrary.GroupBy(q => q.owner);
            List<LibraryRootObject> grpdResult = new List<LibraryRootObject>();
            foreach (var bsLibGrp in bsLibGroups)
            {
                LibraryRootObject grpSplitter = new LibraryRootObject()
                {
                    type = GlobalVariables.GROUP_SPLITTER,
                    owner = bsLibGrp.Key,
                };
                grpdResult.Add(grpSplitter);
                grpdResult.AddRange(bsLibGrp.ToList());
            }
            return grpdResult;
        }

        private void displayListMyLibs(List<LibraryRootObject> mainLibsSource, List<LibraryRootObject> beSharedLibsSource)
        {
            if (mainLibsSource.Count == 0)
            {
                SetProgressIndicator(false);
            }
            else
            {
                SetProgressIndicator(false);

                listBoxAllLibraries.ItemsSource = mainLibsSource;
                listBoxBeSharedLibs.ItemsSource = beSharedLibsSource;
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
            if (SystemTray.ProgressIndicator == null)
                SystemTray.ProgressIndicator = new ProgressIndicator();

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
                    NavigationService.Navigate(
                        new Uri("/Pages/ContentLibraryPage.xaml?token=" + authorizationToken + "&url=" + address + "&idlibrary=" + GlobalVariables.currentLibrary, UriKind.Relative));
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
            string t = currentDecryptUri + GlobalVariables.currentLibrary + "/";
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