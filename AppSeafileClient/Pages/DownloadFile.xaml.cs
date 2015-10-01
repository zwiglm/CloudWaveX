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
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
using Windows.Web.Http;
using System.Windows.Media.Imaging;
using Windows.Storage;
using System.IO;
using System.IO.IsolatedStorage;
using PlasticWonderland.Resources;
using System.Text;
using Windows.Web.Http.Filters;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage.Streams;
using PlasticWonderland.Domain;
using PlasticWonderland.Class;
using System.Collections.ObjectModel;

namespace PlasticWonderland.Pages
{
    public partial class DownloadFile : PhoneApplicationPage
    {

        public enum DownloadStatus
        {
            Ok,
            SameName,
            Error
        };

        //private ObservableCollection<CacheFileEntry> _cacheFileEntries;

        string authorization = "";
        string address = "";
        string id = "";
        string path = "";
        string file = "";
        string downloadUrl = "";
        string timeLastUpdate = "";
        long fileSize;
        string sfUniqueId = "";
        bool backFromView = false;

        CancellationTokenSource ctsDownload;

        static string completePathOnISF;


        public DownloadFile()
        {
            InitializeComponent();
        }


        #region DB Stuff

        //public ObservableCollection<CacheFileEntry> CacheFileEntries
        //{
        //    get
        //    {
        //        return _cacheFileEntries;
        //    }
        //    set
        //    {
        //        if (_cacheFileEntries != value)
        //        {
        //            _cacheFileEntries = value;
        //            //NotifyPropertyChanged("ToDoItems");
        //        }
        //    }
        //}

        private void loadCacheFileEntries()
        {
            // Define the query to gather all of the to-do items.
            //var cacheFileEnties = 
            //    from CacheFileEntry cfe in _cacheFileEntryDB.CacheFileEntries
            //    select cfe;

            // Execute the query and place the results into a collection.
            //CacheFileEntries = new ObservableCollection<CacheFileEntry>(cacheFileEnties);
        }

        private void saveNewCacheFileEntry(string fileName)
        {
            using (CacheFileEntryContext cfeDbContetxt = new CacheFileEntryContext(CacheFileEntryContext.DBConnectionString))
            {
                CacheFileEntry newCacheFileEntry = new CacheFileEntry()
                {
                    ZwstHashValue = sfUniqueId,
                    Mtime = timeLastUpdate,
                    FileSize = fileSize,
                };

                // Add a to-do item to the observable collection.
                //CacheFileEntries.Add(newCacheFileEntry);

                // Add a to-do item to the local database.
                cfeDbContetxt.CacheFileEntries.InsertOnSubmit(newCacheFileEntry);

                // Save changes to the database.
                cfeDbContetxt.SubmitChanges();

            }
        }

        private void updateCachFileEntry(CacheFileEntry cfe)
        {
            using (CacheFileEntryContext cfeDbContetxt = new CacheFileEntryContext(CacheFileEntryContext.DBConnectionString))
            {
                cfe.FileSize = fileSize;
                cfe.Mtime = timeLastUpdate;
                cfeDbContetxt.SubmitChanges();
            }
        }

        private CacheFileEntry findCfeByHash(string hashValue)
        {
            using (CacheFileEntryContext cfeDbContetxt = new CacheFileEntryContext(CacheFileEntryContext.DBConnectionString))
            {
                IQueryable<CacheFileEntry> cfeQuery = from cfe in cfeDbContetxt.CacheFileEntries where cfe.ZwstHashValue == hashValue select cfe;
                return cfeQuery.FirstOrDefault();
            }
        }

        #endregion


        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string token = "";
            string url = "";
            string idlibrary = "";
            string pathFolder = "";
            string fileName = "";
            string timestamp = "";
            string strSize;
            string uniqueId = "";

            //Get variables
            if (NavigationContext.QueryString.TryGetValue("token", out token))
            {
                authorization = token;
            }
            if (NavigationContext.QueryString.TryGetValue("url", out url))
            {
                address = url;
            }
            if (NavigationContext.QueryString.TryGetValue("idlibrary", out idlibrary))
            {
                id = idlibrary;
            }
            if (NavigationContext.QueryString.TryGetValue("pathFolder", out pathFolder))
            {
                path = pathFolder;
            }
            if (NavigationContext.QueryString.TryGetValue("fileName", out fileName))
            {
                file = fileName;
            }
            if (NavigationContext.QueryString.TryGetValue("timestamp", out timestamp))
            {
                timeLastUpdate = timestamp;
            }
            if (NavigationContext.QueryString.TryGetValue("fileSize", out strSize))
            {
                bool done = long.TryParse(strSize, out fileSize);
            }
            if (NavigationContext.QueryString.TryGetValue("sfUniqueId", out uniqueId))
            {
                sfUniqueId = uniqueId;
            }

            if (backFromView == false)
            {
                GetURLDataAsync(authorization, address, id, "file", path);
            }
            else
            {
                openFileDownloaded.Visibility = Visibility.Collapsed;
                backFromView = false;
            }

        }


        private async void GetURLDataAsync(string token, string url, string idlib, string type, string path)
        {
            var filter = HttpHelperFactory.Instance.getHttpFilter();
            Uri uristring = null;
            HttpClient webClientGetURLData = new HttpClient(filter);

            if (!string.IsNullOrEmpty(path))
            {
                uristring = new Uri(url + "/api2/" + "repos/" + idlib + "/" + type + "/?p=/" + System.Net.HttpUtility.UrlEncode(path));
            }
            webClientGetURLData.DefaultRequestHeaders.Add("Accept", "application/json; charset=utf-8; indent=4");
            webClientGetURLData.DefaultRequestHeaders.Add("Authorization", "Token " + token);

            try
            {
                String dummy = await webClientGetURLData.GetStringAsync(uristring);
                this.saveDataFromUriString(dummy);
            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "Get url data error :  " + ex.Message);
                }
                MessageBox.Show(AppResources.Download_Error_Download_Content, AppResources.Download_Error_Download_Title, MessageBoxButton.OK);
            }
        }

        private async void saveDataFromUriString(string uriString)
        {
            if (!string.IsNullOrEmpty(uriString))
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "JSON before : " + uriString);
                }
                string tmp = uriString;
                int l = tmp.Length - 2;
                tmp = tmp.Substring(1, l);

                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "JSON after : " + tmp);
                }

                downloadUrl = tmp;

                string pathInISF;
                if (path.StartsWith("/"))
                {
                    pathInISF = "cache" + "/" + id + path;
                }
                else
                {
                    pathInISF = "cache" + "/" + id + "/" + path;
                }

                if (GlobalVariables.ISF.FileExists(pathInISF))
                {
                    completePathOnISF = pathInISF;
                    openFileFromISF();
                }
                else
                {
                    await DownloadFileWithURLDataAsync();
                }

            }
        }


        private async Task DownloadFileWithURLDataAsync()
        {
            var filter = HttpHelperFactory.Instance.getHttpFilter();
            HttpClient webClientGetURLData = new HttpClient(filter);

            cancelbtn.Visibility = Visibility.Visible;

            try
            {
                ctsDownload = new CancellationTokenSource();
                var dwnldProgressHandler = new Progress<HttpProgress>(dwnldProgressCallback);
                var asyncResponse = await webClientGetURLData.GetAsync(new Uri(downloadUrl)).AsTask(ctsDownload.Token, dwnldProgressHandler);

                HttpResponseMessage errors = asyncResponse.EnsureSuccessStatusCode();
                StoreFileToISF(asyncResponse.Content);
            }
            catch (TaskCanceledException tcEx)
            {
                if (ctsDownload.Token.IsCancellationRequested)
                {
                    DownloadStatusText.Text = AppResources.Download_Status_Text_3;
                    return;
                }
                else
                {
                    // MaZ todo: from Resources....
                    DownloadStatusText.Text = "Operation timed out";
                    return;
                }
            }
            catch (Exception ex)
            {
                DownloadStatusText.Text = AppResources.Download_Status_Text_4;
                DownloadResultText.Text = ex.Message;
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, String.Format("Download error:  {0} {1}", ex.ToString(), ex.Message));
                }
                return;
            }

            DownloadStatusText.Text = AppResources.Download_Status_Text_5;
            // MaZ todo: seems there should be ab label with the name of the file to donwload...
            //DownloadResultText.Text = e.Result;
            DownloadResultText.Visibility = Visibility.Collapsed;
            cancelbtn.Visibility = Visibility.Collapsed;
        }

        private void dwnldProgressCallback(HttpProgress progressInfo)
        {
            var bc = progressInfo.BytesReceived;
            var tb = progressInfo.TotalBytesToReceive;
            var p = 0;
            try
            {
                p = Convert.ToInt32(tb / bc);
            }
            catch { }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ProgressBarStatus.Value = bc;
                ProgressBarStatus.Maximum = Convert.ToDouble(tb);

                string t = AppResources.Download_Status_Text_1 + file + AppResources.Download_Status_Text_2;
                DownloadStatusText.Text = t;
                DownloadResultText.Text =
                    //AppResources.Download_Result_Text_1 + (bc / 1024) + AppResources.Download_Result_Text_2 + (tb / 1024) + AppResources.Download_Result_Text_3 + p + AppResources.Download_Result_Text_4;
                    AppResources.Download_Result_Text_1 + (bc / 1024) + AppResources.Download_Result_Text_2 + (tb / 1024) + AppResources.Download_Result_Text_3;
            });
        }

        private void CancelButton_Click_1(object sender, RoutedEventArgs e)
        {
            ctsDownload.Cancel();
        }

   
        private async void StoreFileToISF(IHttpContent downloadContent)
        {
            DownloadStatus fileDownloaded = await StoreFileSimle(downloadContent, file, id, path);
            switch (fileDownloaded)
            {
                case DownloadStatus.Ok:
                    openFileDownloaded.Visibility = Visibility.Visible;
                    break;
                case DownloadStatus.SameName:
                    MessageBox.Show(AppResources.Download_Error_StoreFile_Content_1);
                    break;
                case DownloadStatus.Error:
                default:
                    MessageBox.Show(AppResources.Download_Error_StoreFile_Content_2);
                    break;
            }
        }
        private async Task<DownloadStatus> StoreFileSimle(IHttpContent downloadContent, string f, string idlib, string path)
        {
            string pathInISF;

            path = path.Remove(path.LastIndexOf('/'));

            if (path.StartsWith("/"))
            {
                pathInISF = "cache" + "/" + idlib + path;
            }
            else
            {
                pathInISF = "cache" + "/" + idlib + "/" + path;
            }

            try
            {
                if (GlobalVariables.ISF.DirectoryExists(pathInISF) == false)
                {
                    GlobalVariables.ISF.CreateDirectory(pathInISF);
                }

                IInputStream dwnldStream = await downloadContent.ReadAsInputStreamAsync();
                Stream ioStream = dwnldStream.AsStreamForRead();


                if (GlobalVariables.ISF.FileExists(pathInISF + "/" + f)) 
                    return DownloadStatus.SameName;

                if (pathInISF.EndsWith("/"))
                {
                    completePathOnISF = pathInISF + f;
                }
                else
                {
                    completePathOnISF = pathInISF + "/" + f;
                }

                using (IsolatedStorageFileStream file = GlobalVariables.ISF.CreateFile(completePathOnISF))
                {
                    ioStream.CopyTo(file);
                    // MaZ attn: also store version or timestamp to be able to identify if newer or not.....
                    //           either insert or update
                    // MaZ todo: add found flag to differ
                    this.saveNewCacheFileEntry(f);
                }


                return DownloadStatus.Ok;

            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "DownloadFileSimle error :  " + DownloadStatus.Error);
                }
                return DownloadStatus.Error;
            }

        }

        public void openFileDownloaded_Click(object sender, RoutedEventArgs e)
        {
            openFileFromISF();
        }

        private async void openFileFromISF()
        {
            backFromView = true;
            string t;
            string q;

            q = completePathOnISF.Replace("/", "\\");

            t = "\\" + q;

            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(q);

            if (file != null)
            {
                var urlOptions = new Windows.System.LauncherOptions();
                urlOptions.TreatAsUntrusted = false;
                await Windows.System.Launcher.LaunchFileAsync(file, urlOptions);
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
        }

    }
}
