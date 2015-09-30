using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Phone.Controls;
using PlasticWonderland.Class;
using PlasticWonderland.Domain;
using PlasticWonderland.Resources;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace PlasticWonderland.Pages
{
    public partial class ContentLibraryPage
    {

        private string _downloadUrl = "";
        private string _completePathOnISF;
        CancellationTokenSource _ctsDownload;

        private string _filePath;
        private string _repoId;

        private string _fileName = "";
        private string _timeLastUpdate = "";
        private long _fileSize;
        private string _sfUniqueId = "";


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
                    UniqueId = _sfUniqueId,
                    Filename = fileName,
                    Mtime = _timeLastUpdate,
                    FileSize = _fileSize,
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
                cfe.FileSize = _fileSize;
                cfe.Mtime = _timeLastUpdate;
                cfeDbContetxt.SubmitChanges();
            }
        }

        private CacheFileEntry findCfeById(string id)
        {
            using (CacheFileEntryContext cfeDbContetxt = new CacheFileEntryContext(CacheFileEntryContext.DBConnectionString))
            {
                IQueryable<CacheFileEntry> cfeQuery = from cfe in cfeDbContetxt.CacheFileEntries where cfe.UniqueId == id select cfe;
                return cfeQuery.FirstOrDefault();
            }
        }

        #endregion


        #region Downloading Stuff

        private async void GetURLDataAsync(
            string token, string url, string idlib, string type, string path,
            string fileName)
        {
            // other stuff that has been transported via Url-parms before
            _fileName = fileName;
            _filePath = path;
            _repoId = idlib;

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

                _downloadUrl = tmp;

                string pathInISF;
                if (_filePath.StartsWith("/"))
                {
                    pathInISF = "cache" + "/" + id + _filePath;
                }
                else
                {
                    pathInISF = "cache" + "/" + id + "/" + _filePath;
                }

                if (GlobalVariables.ISF.FileExists(pathInISF))
                {
                    _completePathOnISF = pathInISF;
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

            // MaZ attn: no cancel
            //cancelbtn.Visibility = Visibility.Visible;

            try
            {
                _ctsDownload = new CancellationTokenSource();
                var dwnldProgressHandler = new Progress<HttpProgress>(dwnldProgressCallback);
                var asyncResponse = await webClientGetURLData.GetAsync(new Uri(_downloadUrl)).AsTask(_ctsDownload.Token, dwnldProgressHandler);

                HttpResponseMessage errors = asyncResponse.EnsureSuccessStatusCode();
                StoreFileToISF(asyncResponse.Content);
            }
            catch (TaskCanceledException tcEx)
            {
                if (_ctsDownload.Token.IsCancellationRequested)
                {
                    // MaZ attn: no DownloadStatusText 
                    //DownloadStatusText.Text = AppResources.Download_Status_Text_3;
                    return;
                }
                else
                {
                    // MaZ attn: no DownloadStatusText (and if, then please from Resources...)
                    //DownloadStatusText.Text = "Operation timed out";
                    return;
                }
            }
            catch (Exception ex)
            {
                // MaZ attn: no DownloadStatusText 
                //DownloadStatusText.Text = AppResources.Download_Status_Text_4;
                //DownloadResultText.Text = ex.Message;
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, String.Format("Download error:  {0} {1}", ex.ToString(), ex.Message));
                }
                return;
            }

            // MaZ attn: no DownloadStatusText 
            //DownloadStatusText.Text = AppResources.Download_Status_Text_5;

            // MaZ attn: no DownloadStatusText 
            // MaZ todo: seems there should be ab label with the name of the file to donwload...
            //DownloadResultText.Text = e.Result;
            //DownloadResultText.Visibility = Visibility.Collapsed;


            // MaZ attn: no cancel
            //cancelbtn.Visibility = Visibility.Collapsed;
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
                _selectedItemProgress.Value = bc;
                _selectedItemProgress.Maximum = Convert.ToDouble(tb);
            });
        }

        private async void StoreFileToISF(IHttpContent downloadContent)
        {
            //DownloadStatus fileDownloaded = await StoreFileSimle(downloadContent, _fileName, id, path);
            DownloadStatus fileDownloaded = await StoreFileSimle(downloadContent, _fileName, _repoId, _filePath);
            switch (fileDownloaded)
            {
                case DownloadStatus.Ok:
                    // MaZ attn: nothing to open here now... only in old view
                    //openFileDownloaded.Visibility = Visibility.Visible;
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
                    _completePathOnISF = pathInISF + f;
                }
                else
                {
                    _completePathOnISF = pathInISF + "/" + f;
                }

                using (IsolatedStorageFileStream file = GlobalVariables.ISF.CreateFile(_completePathOnISF))
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

        private async void openFileFromISF()
        {
            string t;
            string q;

            q = _completePathOnISF.Replace("/", "\\");

            t = "\\" + q;

            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(q);

            if (file != null)
            {
                var urlOptions = new Windows.System.LauncherOptions();
                urlOptions.TreatAsUntrusted = false;
                await Windows.System.Launcher.LaunchFileAsync(file, urlOptions);
            }
        }

        #endregion

    }
}
