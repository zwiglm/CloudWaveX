using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using AppSeafileClient.Resources;
using System.Text;
using Windows.Web.Http.Filters;
using Windows.Security.Cryptography.Certificates;

namespace AppSeafileClient.Pages
{
    public partial class DownloadFile : PhoneApplicationPage
    {

        string authorization = "";
        string address = "";
        string id = "";
        string path = "";
        string file = "";
        string downloadUrl = "";
        string timeLastUpdate = "";
        WebClient webClientDownloadFileURLData;
        public enum DownloadStatus { Ok, SameName, Error };
        bool backFromView = false;

        static string completePatahOnISF;

        public DownloadFile()
        {
            InitializeComponent();
        }

       

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string token = "";
            string url = "";
            string idlibrary = "";
            string pathFolder = "";
            string fileName = "";
            string timestamp = "";

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

            if (backFromView == false)
            {
                //GetURLData(authorization, address, id, "file", path); 
                GetURLDataAsync(authorization, address, id, "file", path);
            }
            else
            {
                openFileDownloaded.Visibility = Visibility.Collapsed;
                backFromView = false;
            }

        }


        //private void GetURLData(string token, string url, string idlib, string type, string path)
        //{
        //    Uri uristring = null;
        //    WebClient webClientGetURLData = new WebClient();

        //    if (!string.IsNullOrEmpty(path))
        //    {
        //        uristring = new Uri(url + "/api2/" + "repos/" + idlib + "/" + type + "/?p=/" + System.Net.HttpUtility.UrlEncode(path));
        //    }


        //    webClientGetURLData.Headers["Accept"] = "application/json; charset=utf-8; indent=4";
        //    webClientGetURLData.Headers["Authorization"] = "Token " + token;

        //    webClientGetURLData.AllowReadStreamBuffering = true;
        //    webClientGetURLData.Encoding = Encoding.UTF8;

        //    webClientGetURLData.DownloadStringCompleted += webClientGetURLData_DownloadStringCompleted;
        //    webClientGetURLData.DownloadStringAsync(uristring);
        //}
        private async void GetURLDataAsync(string token, string url, string idlib, string type, string path)
        {
            Uri uristring = null;

            var filter = new HttpBaseProtocolFilter();
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.IncompleteChain);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
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

        //private void webClientGetURLData_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        //{
        //    try
        //    {
        //        string json = e.Result;
        //        this.saveDataFromUriString(json);
        //    }
        //    catch
        //    {
        //        if (GlobalVariables.IsDebugMode == true)
        //        {
        //            App.logger.log(LogLevel.critical, "Get url data error :  " + e.Error);
        //        }
        //        MessageBox.Show(AppResources.Download_Error_Download_Content, AppResources.Download_Error_Download_Title, MessageBoxButton.OK);
        //    }
        //}
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
                    completePatahOnISF = pathInISF;
                    openFileFromISF();
                }
                else
                {
                    //DownloadFileWithURLData();
                    await DownloadFileWithURLDataAsync();
                }

            }
        }


       // private void DownloadFileWithURLData()
       // {
       //     webClientDownloadFileURLData = new WebClient();
       //     webClientDownloadFileURLData.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webClientDownloadFileURLData_DownloadStringCompleted);
       //     webClientDownloadFileURLData.DownloadProgressChanged += new DownloadProgressChangedEventHandler(webClientDownloadFileURLData_DownloadProgressChanged);
       //     webClientDownloadFileURLData.DownloadStringAsync(new Uri(downloadUrl));

       //     cancelbtn.Visibility = Visibility.Visible;

       ////     DownloadStatusText.Text = "Downloading source from " + downloadUrl;
       //     DownloadResultText.Text = string.Empty;
       // }
        private async Task DownloadFileWithURLDataAsync()
        {
            var filter = new HttpBaseProtocolFilter();
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.IncompleteChain);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            HttpClient webClientGetURLData = new HttpClient(filter);

            cancelbtn.Visibility = Visibility.Visible;

            var asyncResponse = webClientGetURLData.GetAsync(new Uri(downloadUrl));
            asyncResponse.Progress = (res, progress) =>
            {
                var bc = progress.BytesReceived;
                var tb = progress.TotalBytesToReceive;
                var p = 0;
                try
                {
                    p = Convert.ToInt32(tb / bc); 
                }
                catch {}

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

            };
            await asyncResponse;

            //String result = asyncResponse.GetResults();
            HttpResponseMessage respMessge = asyncResponse.GetResults();
            respMessge.EnsureSuccessStatusCode();
            StoreFileToISF();
        }
        //private void DwnldCompleted(IAsyncOperationWithProgress<string, HttpProgress> asyncInfo, AsyncStatus asyncStatus)
        //{
        //    string dummy = "";
        //}

        //void webClientDownloadFileURLData_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        //{
        //    var bc = e.BytesReceived;
        //    var tb = e.TotalBytesToReceive;
        //    var p = e.ProgressPercentage;

        //    ProgressBarStatus.Value = bc;
        //    ProgressBarStatus.Maximum = tb;

        //    string t = AppResources.Download_Status_Text_1 + file + AppResources.Download_Status_Text_2;
        //    DownloadStatusText.Text = t;
        //    DownloadResultText.Text = AppResources.Download_Result_Text_1 + (bc/1024) + AppResources.Download_Result_Text_2 + (tb/1024) + AppResources.Download_Result_Text_3 + p + AppResources.Download_Result_Text_4;

        //    if (GlobalVariables.IsDebugMode == true)
        //    {
        //        App.logger.log(LogLevel.debug, "Download : " + (string)e.UserState + " downloaded " + e.BytesReceived + " of " + e.TotalBytesToReceive + " bytes. " + e.ProgressPercentage +  "% complete...");
        //    }
        //}

        void webClientDownloadFileURLData_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                cancelbtn.Visibility = Visibility.Collapsed;
                if (e.Cancelled == true)
                {
                    DownloadStatusText.Text = AppResources.Download_Status_Text_3;
                    return;
                }

                if (e.Error != null)
                {
                    DownloadStatusText.Text = AppResources.Download_Status_Text_4;
                    DownloadResultText.Text = e.Error.ToString();
                    if (GlobalVariables.IsDebugMode == true)
                    {
                        App.logger.log(LogLevel.critical, "Download error :  " + e.Error.ToString());
                    }
                    return;
                }
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "File download OK");
                }
                DownloadStatusText.Text = AppResources.Download_Status_Text_5;
                DownloadResultText.Text = e.Result;

                DownloadResultText.Visibility = Visibility.Collapsed;
                cancelbtn.Visibility = Visibility.Collapsed;

                StoreFileToISF();
            }
            catch
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "Download file error :  " + e.Error.ToString());
                }
            }
        }

        private void CancelButton_Click_1(object sender, RoutedEventArgs e)
        {
            webClientDownloadFileURLData.CancelAsync();
        }

   
        private async void StoreFileToISF()
        {
            DownloadStatus fileDownloaded = await DownloadFileSimle(new Uri(downloadUrl, UriKind.Absolute), file, id, path);
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

        public static Task<Stream> DownloadStream(Uri url)
        {
            TaskCompletionSource<Stream> tcs = new TaskCompletionSource<Stream>();
            WebClient wbc = new WebClient();

            wbc.OpenReadCompleted += (s, e) =>
            {
                if (e.Error != null) tcs.TrySetException(e.Error);
                else if (e.Cancelled) tcs.TrySetCanceled();
                else tcs.TrySetResult(e.Result);
            };
            wbc.OpenReadAsync(url);
            return tcs.Task;
        }

        public static async Task<DownloadStatus> DownloadFileSimle(Uri fileAdress, string f, string idlib, string path)
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

                Stream response = await DownloadStream(fileAdress);

                if (GlobalVariables.ISF.FileExists(pathInISF + "/" + f)) return DownloadStatus.SameName;
                if (pathInISF.EndsWith("/"))
                {
                    completePatahOnISF = pathInISF + f; 
                }
                else
                {
                    completePatahOnISF = pathInISF + "/" + f;
                }

                using (IsolatedStorageFileStream file = GlobalVariables.ISF.CreateFile(completePatahOnISF))
                    response.CopyTo(file);


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

        public async void openFileFromISF()
        {
            backFromView = true;
            string t;
            string q;

            q = completePatahOnISF.Replace("/", "\\");

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
            try
            {
                if (webClientDownloadFileURLData.IsBusy)
                {
                    webClientDownloadFileURLData.CancelAsync();
                }
            }
            catch (Exception)
            {

                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "webClient is not busy");
                }
            }

         
            base.OnBackKeyPress(e);
        }
                  
    }
}
