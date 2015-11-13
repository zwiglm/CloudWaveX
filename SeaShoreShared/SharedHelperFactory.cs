using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Net.NetworkInformation;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http.Filters;

namespace SeaShoreShared
{
    public class SharedHelperFactory
    {
        private static string SHA_256 = "SHA256";
        private static string MD5 = "MD5";
        private static SharedHelperFactory _instance;


        private SharedHelperFactory()
        {
        }

        public static SharedHelperFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SharedHelperFactory();
                }
                return _instance;
            }
        }


        public string CalculateHashForString(string repoId, string filePath, string fileName)
        {
            string repoIdClean = repoId.Trim('/');
            string filePathClean = filePath.Trim('/');
            string fileNameClean = fileName.Trim('/');
            string strConc = string.Format("{0};{1};{2}", repoIdClean, filePathClean, fileNameClean);
            string hash = this.CalculateHashForString(strConc, SharedHelperFactory.SHA_256);
            return hash;
        }

        public string CalculateMD5ForLibraryFile(StorageFile file, string modifiedOffset)
        {
            string forEncoding = String.Format("{0};{1};{2};{3}", file.DisplayName, file.Name, file.Path, modifiedOffset);
            string md5Hash = this.CalculateHashForString(forEncoding, SharedHelperFactory.MD5);
            return md5Hash;
        }


        #region Privat

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
