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


        public string CalculateHashForString(string repoId, string filePath, string fileName)
        {
            string repoIdClean = repoId.Trim('/');
            string filePathClean = filePath.Trim('/');
            string fileNameClean = fileName.Trim('/');
            string strConc = string.Format("{0};{1};{2}", repoIdClean, filePathClean, fileNameClean);
            string hash =  this.CalculateHashForString(strConc, HttpHelperFactory.SHA_256);
            return hash;
        }


        #region Privee

        private void DeviceNetworkInformation_NetworkAvailabilityChanged(object sender, NetworkNotificationEventArgs e)
        {
            this.IsWifiEnabled = DeviceNetworkInformation.IsWiFiEnabled;
        }

        private string CalculateHashForString(string DataString, string hashType)
        {
            string dataHash = string.Empty;

            if (string.IsNullOrWhiteSpace(DataString))
                return null;

            if (string.IsNullOrWhiteSpace(hashType))
                hashType = "MD5";
            try
            {
                ///Hash Algorithm Provider is Created 
                HashAlgorithmProvider Algorithm = HashAlgorithmProvider.OpenAlgorithm(hashType);
                ///Creating a Buffer Stream using the Cryptographic Buffer class and UTF8 encoding 
                IBuffer vector = CryptographicBuffer.ConvertStringToBinary(DataString, BinaryStringEncoding.Utf8);


                IBuffer digest = Algorithm.HashData(vector);////Hashing The Data 

                if (digest.Length != Algorithm.HashLength)
                {
                    throw new System.InvalidOperationException(
                      "HashAlgorithmProvider failed to generate a hash of proper length!");
                }
                else
                {

                    dataHash = CryptographicBuffer.EncodeToHexString(digest);//Encoding it to a Hex String 
                    return dataHash;
                }
            }
            catch (Exception ex)
            {
                ///
            }

            return null;
        }

        #endregion

    }
}
