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
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Phone.Tasks;

namespace AppSeafileClient.Pages
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            if (!GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.TOKEN_SAVED_SET) && !GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.URL_SAVED_SET))
            {
                Debug.WriteLine("Button disabled because no session");
                ButtonLogoff.IsEnabled = false;
            }
        }

        /// <summary>
        /// Occurs when user click on the logoff button. Destroy settings and cache
        /// </summary>
        private void ButtonLogoff_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.TOKEN_SAVED_SET) && GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.URL_SAVED_SET))
            {
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "Delete isolatedstorage informations");
                }
                GlobalVariables.IsolatedStorageUserInformations.Clear();
                ButtonLogoff.IsEnabled = false;
                ButtonDeleteCache.IsEnabled = false;
                CleanAndDeleteDirectoryRecursive("cache");

            }
        }

        /// <summary>
        /// Occurs when user click on the delete cache button. Destroy cache
        /// </summary>
        private void ButtonDeleteCache_Click(object sender, RoutedEventArgs e)
        {
            CleanAndDeleteDirectoryRecursive("cache");
        }

        private void ButtonReview_Click(object sender, RoutedEventArgs e)
        {
            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
            marketplaceReviewTask.Show();
        }


        private static void CleanAndDeleteDirectoryRecursive(string directory)
        {
            IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication(); if (iso.DirectoryExists(directory))
            {
                string[] files = iso.GetFileNames(directory + @"/*"); 
                foreach (string file in files) 
                { 
                    iso.DeleteFile(directory + @"/" + file);
                    if (GlobalVariables.IsDebugMode == true)
                    {
                        App.logger.log(LogLevel.debug, "Deleted file: " + directory + @"/" + file);
                    }
                }
                string[] subDirectories = iso.GetDirectoryNames(directory + @"/*");
                foreach (string subDirectory in subDirectories)
                {
                    CleanAndDeleteDirectoryRecursive(directory + @"/" + subDirectory);
                }

                iso.DeleteDirectory(directory);
                if (GlobalVariables.IsDebugMode == true)
                {
                    App.logger.log(LogLevel.debug, "Deleted directory: " + directory);
                }

            }
        }

        /// <summary>
        /// Occurs when back key is pressed
        /// </summary>
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (!GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.TOKEN_SAVED_SET))
            {
                e.Cancel = true;
                Application.Current.Terminate();
            } 
            else
            {
                base.OnBackKeyPress(e);
            }
        }

    }
}