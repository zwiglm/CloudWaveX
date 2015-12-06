using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Info;
using Microsoft.Phone.Net.NetworkInformation;
using Newtonsoft.Json;
using PlasticWonderland.Resources;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Certificates;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace PlasticWonderland.Domain
{
    public class HttpHelperFactory
    {
        private static HttpHelperFactory _instance;

        private  HttpHelperFactory()
        {
            this.PhotoBackupLibraryName = this.photoBackupLibraryName();
            this.GetAgentVersion = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
        }

        public static HttpHelperFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HttpHelperFactory();
                }
                return _instance;
            }
        }


        public string PhotoBackupLibraryName { get; private set; }
        public Version GetAgentVersion { get; private set; }

        public HttpBaseProtocolFilter getHttpFilter()
        {
            var filter = new HttpBaseProtocolFilter();

            // *******************
            // IGNORING CERTIFACTE PROBLEMS for self-signed
            // *******************
            if (GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.IGNORE_SELF_SIGNED_SET) && 
                (bool)GlobalVariables.IsolatedStorageUserInformations[GlobalVariables.IGNORE_SELF_SIGNED_SET] == true)
            {
                filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.IncompleteChain);
                filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
                filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            } 
            return filter;
        }

        public LibraryRootObject photoBackupLibraryExists(List<LibraryRootObject> mainLibs)
        {
            LibraryRootObject result = null;
            List<LibraryRootObject> selected = mainLibs.Where(q => q.name.Equals(this.PhotoBackupLibraryName)).ToList();
            if (selected.Count > 0)
                result = selected.First();
            return result;
        }

        public async Task<LibraryRootObject> createPhotoBackupLibary(string token, string url)
        {
            var filter = HttpHelperFactory.Instance.getHttpFilter();
            Uri currentRequestUri = new Uri(url + "/api2/" + GlobalVariables.SF_REQ_REPOS + "/");
            var HttpClientGetLibrary = new HttpClient(filter);

            HttpClientGetLibrary.DefaultRequestHeaders.Add("Accept", "application/json;indent=4");
            HttpClientGetLibrary.DefaultRequestHeaders.Add("Authorization", "token " + token);
            HttpClientGetLibrary.DefaultRequestHeaders.Add("User-agent", GlobalVariables.WEB_CLIENT_AGENT + this.GetAgentVersion);

            HttpFormUrlEncodedContent msgParms = new HttpFormUrlEncodedContent(new[] {
               new KeyValuePair<string, string>("name", this.PhotoBackupLibraryName),
               new KeyValuePair<string, string>("desc", AppResources.Photo_Backup_Library_Description)
            });

            LibraryRootObject result = null;
            try
            {
                HttpResponseMessage libsResponse = await HttpClientGetLibrary.PostAsync(currentRequestUri, msgParms);
                result = JsonConvert.DeserializeObject<LibraryRootObject>(libsResponse.Content.ToString());
                HttpClientGetLibrary.Dispose();
            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, "Download list library Exception err" + ex);
                    App.logger.log(LogLevel.critical, "Download list library uristringRequestLibrary : " + currentRequestUri.ToString());
                    App.logger.log(LogLevel.critical, "Download list library informations address : " + url);

                }
            }

            return result;
        }

        public async Task<PhotoUploadWrapper> getUplUpdLink(string token, string url, string repoId, string sfQualifier)
        {
            var filter = HttpHelperFactory.Instance.getHttpFilter();
            Uri currentRequestUri = new Uri(url + "/api2/repos/" + repoId + "/" + sfQualifier + "/");
            var HttpClientGetLibrary = new HttpClient(filter);

            HttpClientGetLibrary.DefaultRequestHeaders.Add("Authorization", "token " + token);
            HttpClientGetLibrary.DefaultRequestHeaders.Add("User-agent", GlobalVariables.WEB_CLIENT_AGENT + this.GetAgentVersion);

            PhotoUploadWrapper result = new PhotoUploadWrapper();
            try
            {
                HttpResponseMessage libsResponse = await HttpClientGetLibrary.GetAsync(currentRequestUri);
                result.HttpResponseState = libsResponse.StatusCode;
                if (result.HttpResponseState == HttpStatusCode.Ok)
                    result.UplUpdLink = libsResponse.Content.ToString().Trim('"');

                HttpClientGetLibrary.Dispose();
            }
            catch (Exception ex)
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.critical, ex, String.Format("get {0} failed", sfQualifier));

                }
            }

            return result;
        }

        public async Task<HttpStatusCode> directoryExists(PhotoUploadWrapper wrp, string directory)
        {
            var filter = HttpHelperFactory.Instance.getHttpFilter();
            Uri currentRequestUri = new Uri(wrp.RawUrl + "/api2/" + GlobalVariables.SF_REQ_REPOS + "/" + wrp.RepoId +"/dir/?p=" + directory);
            var HttpClientGetLibrary = new HttpClient(filter);

            HttpClientGetLibrary.DefaultRequestHeaders.Add("Accept", "application/json;charset=utf-8;indent=4");
            HttpClientGetLibrary.DefaultRequestHeaders.Add("Authorization", "token " + wrp.AuthToken);
            HttpClientGetLibrary.DefaultRequestHeaders.Add("User-agent", GlobalVariables.WEB_CLIENT_AGENT + this.GetAgentVersion);

            HttpStatusCode result = HttpStatusCode.NotImplemented;
            try
            {
                HttpResponseMessage libsResponse = await HttpClientGetLibrary.GetAsync(currentRequestUri);
                result = libsResponse.StatusCode;
                HttpClientGetLibrary.Dispose();
            }
            catch (Exception ex)
            {
            }

            return result;
        }

        public async Task<HttpStatusCode> createDirectory(PhotoUploadWrapper wrp, string directory)
        {
            var filter = HttpHelperFactory.Instance.getHttpFilter();
            Uri currentRequestUri = new Uri(wrp.RawUrl + "/api2/" + GlobalVariables.SF_REQ_REPOS + "/" + wrp.RepoId + "/dir/?p=" + directory);
            var HttpClientGetLibrary = new HttpClient(filter);

            HttpClientGetLibrary.DefaultRequestHeaders.Add("Accept", "application/json;charset=utf-8;indent=4");
            HttpClientGetLibrary.DefaultRequestHeaders.Add("Authorization", "token " + wrp.AuthToken);
            HttpClientGetLibrary.DefaultRequestHeaders.Add("User-agent", GlobalVariables.WEB_CLIENT_AGENT + this.GetAgentVersion);

            HttpFormUrlEncodedContent msgParms = new HttpFormUrlEncodedContent(new[] {
               new KeyValuePair<string, string>("operation", "mkdir"),
            });

            HttpStatusCode result = HttpStatusCode.NotImplemented;
            try
            {
                HttpResponseMessage libsResponse = await HttpClientGetLibrary.PostAsync(currentRequestUri, msgParms);
                result = libsResponse.StatusCode;
                HttpClientGetLibrary.Dispose();
            }
            catch (Exception ex)
            {
            }

            return result;
        }


        #region Private

        private string photoBackupLibraryName()
        {
            //byte[] devIdBytes = (byte[])DeviceExtendedProperties.GetValue("DeviceUniqueId");
            //string deviceId = Convert.ToBase64String(devIdBytes);

            string device = DeviceStatus.DeviceName;
            string result = String.Format("{0} - {1}", AppResources.Photo_Backup_Library_Prefix, device);
            return result;
        }

        #endregion

    }
}
