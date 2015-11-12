using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Net.NetworkInformation;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Certificates;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.Web.Http.Filters;

namespace PlasticWonderland.Domain
{
    public class HttpHelperFactory
    {
        private static string SHA_256 = "SHA256";
        private static HttpHelperFactory _instance;


        private  HttpHelperFactory()
        {
            //NetworkInterfaces = new ObservableCollection<string>();
            DeviceNetworkInformation.NetworkAvailabilityChanged += DeviceNetworkInformation_NetworkAvailabilityChanged;
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

        //public ObservableCollection<string> NetworkInterfaces { get; private set; }
        public bool IsWifiEnabled { get; private set; }

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


        #region Privee

        private void DeviceNetworkInformation_NetworkAvailabilityChanged(object sender, NetworkNotificationEventArgs e)
        {
            this.IsWifiEnabled = DeviceNetworkInformation.IsWiFiEnabled;
        }

        #endregion

    }
}
