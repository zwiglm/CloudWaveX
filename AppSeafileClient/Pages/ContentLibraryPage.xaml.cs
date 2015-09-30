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
using Windows.Storage;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Windows.Web.Http.Filters;
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO.IsolatedStorage;
using PlasticWonderland.Resources;
using Windows.Storage.Streams;
using Windows.Web.Http.Headers;
using System.Reflection;
using System.Windows.Threading;
using PlasticWonderland.Domain;
using PlasticWonderland.Class;

namespace PlasticWonderland.Pages
{


    public partial class ContentLibraryPage : PhoneApplicationPage
    {
        string authorization = "";
        string address = "";
        string id = "";
        string path = "";
        string completePath = "";

        int HttpErrorCode_GetUploadLink;

        public enum DownloadStatus 
        { 
            Ok, 
            SameName, 
            Error 
        };

        StorageFile fileChoosenFromFilePicker;
        CancellationTokenSource cts;
        ProgressBar _selectedItemProgress;


        public ContentLibraryPage()
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
            SystemTray.ProgressIndicator = new ProgressIndicator();

            if (GlobalVariables.IsDebugMode == true)
            {
                myPivoteItemHelp.toogle_debug_state = true;
            }
            else
            {
                myPivoteItemHelp.toogle_debug_state = false;
            }

            var app = App.Current as App;
            if (app.FilePickerContinuationArgs != null)
            {
                if (app.FilePickerContinuationArgs.Files != null && app.FilePickerContinuationArgs.Files.Count > 0)
                {
                    fileChoosenFromFilePicker = app.FilePickerContinuationArgs.Files[0];
                    // Process file here
                    UploadFileFromFilePicker();
                    app.FilePickerContinuationArgs = null;
                }
            }

            //Get variable
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
                path = System.Net.HttpUtility.UrlEncode(pathFolder);
            }

            if (GlobalVariables.IsDebugMode == true)
            {
                App.logger.log(LogLevel.debug, "Path : " + path);
            }

            if (path.StartsWith("/") == true)
            {
                path = path.Substring(1, path.Length - 1);
            }

            completePath = path;

            App.logger.log(LogLevel.debug, "ON NAVIGUATEDTO");

            if (completePath != "")
            {
                requestContentLibrary(authorization, address, id, "dir", System.Net.HttpUtility.UrlEncode(completePath));
            }
            else
            {
                requestContentLibrary(authorization, address, id, "dir", completePath);
            }
        }

        /// <summary>
        /// Get content of the library from the server
        /// </summary>
        /// <param name="token">Auth token from the user</param>
        /// <param name="type">Type of API call</param>
        /// <param name="url">URL of the server</param>
        /// <param name="idlib">ID of the library</param>
        /// <param name="path">Path in the library</param>
        private async void requestContentLibrary(string token, string url, string idlib, string type, string path)
        {

            var filter = HttpHelperFactory.Instance.getHttpFilter();
            Uri uristringContentLibrary = null;
            var HttpClientContentLibrary = new HttpClient(filter);
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);           

            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith("/") == true)
                {
                    path = path.Substring(1, path.Length - 1);
                }
                uristringContentLibrary = new Uri(url + "/api2/" + "repos/" + idlib + "/" + type + "/?p=" + path);
            }
            else
            {
                uristringContentLibrary = new Uri(url + "/api2/" + "repos/" + idlib + "/" + type + "/");
            }

            HttpClientContentLibrary.DefaultRequestHeaders.Add("Accept", "application/json;indent=4");
            HttpClientContentLibrary.DefaultRequestHeaders.Add("Authorization", "token " + token);
            HttpClientContentLibrary.DefaultRequestHeaders.Add("User-agent", "CloudWave for Seafile/" + nameHelper.Version);

            if (GlobalVariables.IsDebugMode == true)
            {
                App.logger.log(LogLevel.debug, "uristringContentLibrary load ContentLibraryPage " + uristringContentLibrary.ToString());
            }
            
            SetProgressIndicator(true);

            try
            {
                HttpResponseMessage responseContentLibrary = await HttpClientContentLibrary.GetAsync(uristringContentLibrary);
                responseContentLibrary.EnsureSuccessStatusCode();

                displayContentLibrary(responseContentLibrary.Content.ToString());

                HttpClientContentLibrary.Dispose();
            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "Download content library Exception err" + ex);
                    App.logger.log(LogLevel.critical, "Download content library informations address : " + address);

                }
                MessageBox.Show(AppResources.ContentLibrary_Error_Content_Content, AppResources.ContentLibrary_Error_Content_Title, MessageBoxButton.OK);
            } 

        }

        private void displayContentLibrary(string p)
        {
            if (SystemTray.ProgressIndicator.IsVisible)
            {
                SetProgressIndicator(false);
            }

            if (listContentLibrary.Items.Count > 0)
            {
                listContentLibrary.ItemsSource = null;
            }

            var resultContentLibrary = JsonConvert.DeserializeObject<List<LibraryRootObject>>(p);
            if (resultContentLibrary.Count == 0)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "Load content library OK but empty");
                    App.logger.log(LogLevel.critical, "Download content library empty informations address : " + address);
                }
            }
            else
            {
                listContentLibrary.ItemsSource = resultContentLibrary;
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "Load content library OK");
                }
            }            
        }

        #region | Upload file |
        
        /// <summary>
        /// Create an upload request for uploading a file
        /// </summary>
        private async void UploadFileFromFilePicker()
        {

            if (!SystemTray.ProgressIndicator.IsVisible)
            {
                SetProgressIndicator(true);
            }

            var filter = HttpHelperFactory.Instance.getHttpFilter();
            Uri uristringGetUploadLink = new Uri(address + "/api2/" + "repos/" + id + "/" + "upload-link/");
            var HttpClientGetUploadLink = new HttpClient(filter);
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);    
          
            HttpClientGetUploadLink.DefaultRequestHeaders.Add("Authorization", "token " + authorization);
            HttpClientGetUploadLink.DefaultRequestHeaders.Add("User-agent", "CloudWave for Seafile/" + nameHelper.Version);

            try
            {
                HttpResponseMessage responseGetUploadLink = await HttpClientGetUploadLink.GetAsync(uristringGetUploadLink);

                if (responseGetUploadLink.StatusCode == HttpStatusCode.Forbidden)
	            {
                    HttpErrorCode_GetUploadLink = 403;
	            }

                responseGetUploadLink.EnsureSuccessStatusCode();

                UploadFileToUploadLink(responseGetUploadLink.Content.ToString());

                HttpClientGetUploadLink.Dispose();
            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "Error get upload link : " + ex.ToString());
                    App.logger.log(LogLevel.critical, "URL uristringGetUploadLink : " + uristringGetUploadLink);

                }

                if (HttpErrorCode_GetUploadLink == 403)
                {
                    MessageBox.Show(AppResources.ContentLibrary_Error_UploadForbidden_Content, AppResources.ContentLibrary_Error_UploadForbidden_Title, MessageBoxButton.OK);
                }
                else
                { 
                    MessageBox.Show(AppResources.ContentLibrary_Error_Upload_Content, AppResources.ContentLibrary_Error_Upload_Title, MessageBoxButton.OK);
                }
            }
        }

   
        private async void UploadFileToUploadLink(string url)
        {

            WaitIndicator.IsVisible = true;

            var filter = HttpHelperFactory.Instance.getHttpFilter();
            Uri uristringForUpload = new Uri(url.Substring(1, url.Length - 2));
            string boundary = "s---------" + DateTime.Today.Ticks.ToString("x");
            var HttpClientUpload = new HttpClient(filter);

            HttpMultipartFormDataContent requestUploadContent = new HttpMultipartFormDataContent(boundary);

            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);

            HttpClientUpload.DefaultRequestHeaders.Add("Authorization", "token " + authorization);
            HttpClientUpload.DefaultRequestHeaders.Add("User-agent", "CloudWave for Seafile/"+nameHelper.Version);
            HttpClientUpload.DefaultRequestHeaders.Add("Accept", "*/*");

            FileInfo fi = new FileInfo(fileChoosenFromFilePicker.Path);
            string fileName = fi.Name;
       

            var fileContent = await fileChoosenFromFilePicker.OpenAsync(FileAccessMode.Read);

            string pathForUpload;
            if (GlobalVariables.currentPath == "")
            {
                pathForUpload = "/";
            }
            else
            {
                pathForUpload = GlobalVariables.currentPath;
            }

            IHttpContent content4 = new HttpStringContent(pathForUpload);
            requestUploadContent.Add(content4, "parent_dir");

            IHttpContent content5 = new HttpStreamContent(fileContent);
            requestUploadContent.Add(content5,"file",fileName);

            string mimetype;
            try
            {
                mimetype = fileChoosenFromFilePicker.ContentType;
                if (mimetype == "")
                {
                    mimetype = "application/octet-stream";
                }
            }
            catch (Exception ex)
            {
                mimetype = "application/octet-stream";
            }
            content5.Headers.Add("Content-Type", mimetype);


      
            try
            {
                cts = new CancellationTokenSource();
                HttpResponseMessage responseUpload = await HttpClientUpload.PostAsync(uristringForUpload, requestUploadContent).AsTask(cts.Token);
                responseUpload.EnsureSuccessStatusCode();
                if (SystemTray.ProgressIndicator.IsVisible)
                {
                    SetProgressIndicator(false);
                }

                HttpClientUpload.Dispose();

                WaitIndicator.IsVisible = false;
                Refresh(AppResources.ContentLibrary_Upload_UploadOK);
            }
            catch (TaskCanceledException)
            {
                App.logger.log(LogLevel.debug, "Request canceled.");
                WaitIndicator.IsVisible = false;
                Refresh(AppResources.ContentLibrary_Upload_UploadCancel);
            } 
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "Error upload : " + ex.ToString());
                    App.logger.log(LogLevel.critical, "URL upload : " + uristringForUpload);

                }
                WaitIndicator.IsVisible = false;
                MessageBox.Show(AppResources.ContentLibrary_Error_Upload_Content, AppResources.ContentLibrary_Error_Upload_Title, MessageBoxButton.OK);
            }
            
        }

             
        #endregion


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
        private void listContentLibrary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lst = sender as ListBox;

            if (lst.SelectedIndex != -1)
            {
                LibraryRootObject lib = lst.SelectedItem as LibraryRootObject;

                lst.SelectedIndex = -1;

                if (lib.type == "dir")
                {
                    GlobalVariables.currentPath = GlobalVariables.currentPath + "/" + lib.name;
                    GlobalVariables.FolderNamePivotItem = lib.name;

                    if (GlobalVariables.IsDebugMode == true)
                    {
                        App.logger.log(LogLevel.debug, " GlobalVariables.currentPath " + GlobalVariables.currentPath);
                    }

                    if (!string.IsNullOrEmpty(GlobalVariables.FolderNamePivotItem))
                    {
                        var t = new TextBlock();
                        t.Text = GlobalVariables.FolderNamePivotItem;
                        t.Foreground = new SolidColorBrush((App.Current.Resources["PhoneAccentBrush"] as SolidColorBrush).Color);

                        PivotItemLibContent.Header = t;
                    }

                    if (GlobalVariables.currentPath.StartsWith("/") == true)
                    {
                        path = GlobalVariables.currentPath.Substring(1, GlobalVariables.currentPath.Length - 1);
                    }

                    completePath = path;

                    requestContentLibrary(authorization, address, GlobalVariables.currentLibrary, "dir", System.Net.HttpUtility.UrlEncode(GlobalVariables.currentPath));
                }
                else if (lib.type == "file")
                {
                    DependencyObject dummyCt = this.listContentLibrary.ItemContainerGenerator.ContainerFromItem(lib);
                    //var dummy = UIChildFinder.FindChild(dummyCt, "DwnldProgrStackP", typeof(StackPanel));
                    _selectedItemProgress = (ProgressBar) UIChildFinder.FindChild(dummyCt, "DownloadProgress", typeof(ProgressBar));

                    if (GlobalVariables.currentPath.StartsWith("/") == true)
                    {
                        GlobalVariables.currentPath = GlobalVariables.currentPath.Substring(1, GlobalVariables.currentPath.Length - 1);
                    }
                    GlobalVariables.currentPWD = GlobalVariables.currentPath + "/" + lib.name;

                    if (GlobalVariables.IsDebugMode == true)
                    {
                        App.logger.log(LogLevel.debug, " GlobalVariables.currentPWD " + GlobalVariables.currentPWD);
                    }

                    //NavigationService.Navigate(
                    //    new Uri("/Pages/DownloadFile.xaml?token=" + authorization + 
                    //            "&url=" + address + 
                    //            "&idlibrary=" + GlobalVariables.currentLibrary + 
                    //            "&pathFolder=" + System.Net.HttpUtility.UrlEncode(GlobalVariables.currentPWD) + 
                    //            "&fileName=" + lib.name + 
                    //            "&timestamp=" + lib.mtime +
                    //            "&fileSize=" + lib.size +
                    //            "&sfUniqueId=" + lib.id, 
                    //            UriKind.Relative));

                    // MaZ todo: instead download and add progress-bar.....
                    this.GetURLDataAsync(authorization, address, GlobalVariables.currentLibrary, "file", GlobalVariables.currentPWD, lib.name);
                    // MaZ continue....
                }
            }
        }

        /// <summary>
        /// Occurs when back key is pressed, and change some variable
        /// </summary>
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch (Exception ex)
            {                
            }

            
            if (GlobalVariables.IsDebugMode == true)
            {
                App.logger.log(LogLevel.debug, "Before back " + GlobalVariables.currentPath);
            }

            string mypath = "";
            string str = null;
            int l = 0;
            int t = 0;
            string[] strArr = null;
            int count = 0;
            str = GlobalVariables.currentPath;
            char[] splitchar = { '/' };
            strArr = str.Split(splitchar);

            if (str.StartsWith("/") == true)
            {
                l = (strArr.Length - 2);
           
                for (count = 1; count <= l; count++)
                {
                    if (GlobalVariables.IsDebugMode == true)
                    {
                        App.logger.log(LogLevel.debug, "loop" + count + strArr[count]);
                    }
                    mypath = mypath + "/" + strArr[count];
                }
            }
            else
            {
                l = (strArr.Length - 2);
               
                for (count = 0; count <= l; count++)
                {
                    if (GlobalVariables.IsDebugMode == true)
                    {
                        App.logger.log(LogLevel.debug, "loop" + count + strArr[count]);
                    }
                    mypath = mypath + "/" + strArr[count];
                }
            }


            GlobalVariables.currentPath = mypath;

            if (GlobalVariables.IsDebugMode == true)
            {
                App.logger.log(LogLevel.debug, "After back " + GlobalVariables.currentPath);
            }

            if (str.StartsWith("/"))
            {
                t = strArr.Length - 2;
                if (t >= 0)
                {
                    if (t <= 0)
                    {
                        GlobalVariables.FolderNamePivotItem = "Top";
                    }
                    else
                    {
                        GlobalVariables.FolderNamePivotItem = strArr[t];
                    }

                    e.Cancel = true;
                    Refresh("");
                }
                else
                {
                    e.Cancel = false;
                }
            }
            else
            {
                if (strArr.Length == 1 && strArr[0] == "")
                {
                    t = -1;
                }
                else
                { 
                    t = strArr.Length - 1;
                }
                if (t >= 0)
                {
                    if (t <= 0)
                    {
                        GlobalVariables.FolderNamePivotItem = "Top";
                    }
                    else
                    {
                        if (t >= 1)
                        {
                            t = t - 1;
                        }
                        GlobalVariables.FolderNamePivotItem = strArr[t];
                    }

                    e.Cancel = true;
                    Refresh("");
                }
                else
                {
                    e.Cancel = false;
                }
            }

            if (!string.IsNullOrEmpty(GlobalVariables.FolderNamePivotItem))
            {
                var q = new TextBlock();
                q.Text = GlobalVariables.FolderNamePivotItem;
                q.Foreground = new SolidColorBrush((App.Current.Resources["PhoneAccentBrush"] as SolidColorBrush).Color);

                PivotItemLibContent.Header = q;
            }
            else
            {
                var q = new TextBlock();
                q.Text = AppResources.ContentLibrary_PivotTitle_1;
                q.Foreground = new SolidColorBrush((App.Current.Resources["PhoneAccentBrush"] as SolidColorBrush).Color);

                PivotItemLibContent.Header = q;
            }
                        
         //   base.OnBackKeyPress(e);
        }

        /// <summary>
        /// Occurs when choose "detail" from the tap and hold event on an item in a ListBox
        /// </summary>
        private void MenuItem_ContentLibrary_Detail(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedListBoxItem = this.listContentLibrary.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            LibraryRootObject t = selectedListBoxItem.Content as LibraryRootObject;

            SetProgressIndicator(true);
            GetFileDetail(t.name);
            SetProgressIndicator(false);
        }

        /// <summary>
        /// Occurs when choose "delete" from the tap and hold event on an item in a ListBox
        /// </summary>
        private void MenuItem_ContentLibrary_Delete(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedListBoxItem = this.listContentLibrary.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            LibraryRootObject t = selectedListBoxItem.Content as LibraryRootObject;

            SetProgressIndicator(true);
            DeleteFile(t.name);
            SetProgressIndicator(false);
       }

        /// <summary>
        /// Get file detail
        /// </summary>
        /// <param name="fileToDetail">File to detail</param>
        private async void GetFileDetail(string fileToDetail)
        {
            var filter = HttpHelperFactory.Instance.getHttpFilter();
            Uri uristringDetail = null;
            var HttpClientGetFileDetail = new HttpClient(filter);
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);   

            if (completePath == "")
            {
                uristringDetail = new Uri(address + "/api2/" + "repos/" + id + "/" + "file/detail" + "/?p=" + System.Net.HttpUtility.UrlEncode(fileToDetail));
            }
            else
            {
                uristringDetail = new Uri(address + "/api2/" + "repos/" + id + "/" + "file/detail" + "/?p=" + System.Net.HttpUtility.UrlEncode(completePath) + "/" + System.Net.HttpUtility.UrlEncode(fileToDetail));
            }

            HttpClientGetFileDetail.DefaultRequestHeaders.Add("Accept", "application/json;charset=utf-8;indent=4");
            HttpClientGetFileDetail.DefaultRequestHeaders.Add("Authorization", "token " + authorization);
            HttpClientGetFileDetail.DefaultRequestHeaders.Add("User-agent", "CloudWave for Seafile/" + nameHelper.Version);

            try
            {
                HttpResponseMessage responseFileDetail = await HttpClientGetFileDetail.GetAsync(uristringDetail);
                responseFileDetail.EnsureSuccessStatusCode();

                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "Detail OK : " + responseFileDetail.StatusCode + " " + responseFileDetail.ReasonPhrase);
                }

                var responseString = responseFileDetail.Content.ToString();
                var resultDetail = JsonConvert.DeserializeObject<LibraryRootObject>(responseString);

                // Unix timestamp is seconds past epoch
                System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds(resultDetail.mtime).ToLocalTime();

                string sizeReadable = "0";

                sizeReadable = CloudHelper.bytesToString(resultDetail.size);


                var messagePrompt = new MessagePrompt
                {
                    Title = AppResources.ContentLibrary_MessagePrompt_Detail_Title,
                    Message = AppResources.ContentLibrary_MessagePrompt_Detail_1 + resultDetail.name + "\r\n" + AppResources.ContentLibrary_MessagePrompt_Detail_2 + sizeReadable + "\r\n" + AppResources.ContentLibrary_MessagePrompt_Detail_3 + dtDateTime + "\r\n"
                };
                messagePrompt.Show();

                HttpClientGetFileDetail.Dispose();
            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "Get file detail error :  " + ex);
                }
                MessageBox.Show(AppResources.ContentLibrary_Error_Detail_Content, AppResources.ContentLibrary_Error_Detail_Title, MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="fileToDelete">File to delete</param>
        public async void DeleteFile(string fileToDelete)
        {
            var filter = HttpHelperFactory.Instance.getHttpFilter();
            Uri uristringDelete = null;
            var HttpClientDeleteFile = new HttpClient(filter);
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);   

            if (completePath == "")
            {
                uristringDelete = new Uri(address + "/api2/" + "repos/" + id + "/" + "file" + "/?p=" + System.Net.HttpUtility.UrlEncode(fileToDelete));
            }
            else
            {
                uristringDelete = new Uri(address + "/api2/" + "repos/" + id + "/" + "file" + "/?p=" + System.Net.HttpUtility.UrlEncode(completePath) + "/" + System.Net.HttpUtility.UrlEncode(fileToDelete));
            }

            HttpClientDeleteFile.DefaultRequestHeaders.Add("Accept", "application/json;charset=utf-8;indent=4");
            HttpClientDeleteFile.DefaultRequestHeaders.Add("Authorization", "token " + authorization);
            HttpClientDeleteFile.DefaultRequestHeaders.Add("User-agent", "CloudWave for Seafile/" + nameHelper.Version);

            try
            {
                HttpResponseMessage responseFileDetail = await HttpClientDeleteFile.DeleteAsync(uristringDelete);
                responseFileDetail.EnsureSuccessStatusCode();

                var responseStringDelete = responseFileDetail.Content.ToString();
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "Delete OK : " + responseFileDetail.StatusCode + " " + responseFileDetail.ReasonPhrase + " " + responseStringDelete);
                }
                Refresh(AppResources.ContentLibrary_Refresh_1);

                HttpClientDeleteFile.Dispose();
            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "Delete file error :  " + ex);
                }
                MessageBox.Show(AppResources.ContentLibrary_Error_Delete_Content, AppResources.ContentLibrary_Error_Delete_Title, MessageBoxButton.OK);
            }
          
        }

        /// <summary>
        /// Refresh the ListBox
        /// </summary>
        private void Refresh(string toastMessage)
        {
            //GetContentLibrary(authorization, address, id, "dir", HttpUtility.UrlEncode(GlobalVariables.currentPath));
            requestContentLibrary(authorization, address, GlobalVariables.currentLibrary, "dir", System.Net.HttpUtility.UrlEncode(GlobalVariables.currentPath));
            if (GlobalVariables.IsDebugMode == true)
            {
                App.logger.log(LogLevel.debug, "Refresh OK");
            }

            if (!string.IsNullOrEmpty(toastMessage))
            {
                var toast = new ToastPrompt
                {
                    Message = toastMessage,
                };
                toast.Show(); 
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DwnldProgrStackP_Loaded(object sender, RoutedEventArgs e)
        {
        }

    }
}