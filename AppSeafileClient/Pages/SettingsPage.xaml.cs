﻿using System;
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
using PlasticWonderland.Class;
using Microsoft.Phone.Scheduler;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using PlasticWonderland.Domain;

namespace PlasticWonderland.Pages
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        // block too early clicking....
        bool _ignorePhotoBackupToggleEvents;


        public SettingsPage()
        {
            InitializeComponent();

            if (!GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.TOKEN_SAVED_SET) && !GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.URL_SAVED_SET))
            {
                Debug.WriteLine("Button disabled because no session");
                ButtonLogoff.IsEnabled = false;
            }
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this._ignorePhotoBackupToggleEvents = true;

            base.OnNavigatedTo(e);

            if (TaskHelperFactory.Instance.enabledBackupPhotos())
                this.CbBackupPhotos.IsChecked = true;
            else
                this.CbBackupPhotos.IsChecked = false;

            if (TaskHelperFactory.Instance.enabledBackupPhotosWifiOnly())
                this.CbBackupPhotosWifiOnly.IsChecked = true;
            else
                this.CbBackupPhotosWifiOnly.IsChecked = false;

            this._ignorePhotoBackupToggleEvents = false;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            TaskHelperFactory.Instance.triggerScheduleAgent();
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
            // also remove from local db
            this.deleteAllCacheFileEntries();
        }

        private void ButtonReview_Click(object sender, RoutedEventArgs e)
        {
            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
            marketplaceReviewTask.Show();
        }

        /// <summary>
        /// MaZ attn: This is fixed URL in code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonPrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Tasks.WebBrowserTask wbt = new Microsoft.Phone.Tasks.WebBrowserTask();
            wbt.Uri = new Uri("http://www.zwigl.info/seashore/Windows-Phone-App/Seashore-Privacy-Policy");
            wbt.Show();
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

        private void CbGetThumbs_Click(object sender, RoutedEventArgs e)
        {
            // MaZ todo:
        }

        private void CbBackupPhotos_Click(object sender, RoutedEventArgs e)
        {
            if (_ignorePhotoBackupToggleEvents)
                return;

            if (this.CbBackupPhotos.IsChecked == true)
            {
                TaskHelperFactory.Instance.addBackupPhotoSetting();
                TaskHelperFactory.Instance.storeSettings();
            }
            else
            {
                TaskHelperFactory.Instance.removeBackupPhotoSetting();
                TaskHelperFactory.Instance.storeSettings();

                this.CbBackupPhotosWifiOnly.IsChecked = false;
                this.CbBackupPhotosOnlyOnWifi_Click(sender, e);
            }
        }

        private void CbBackupPhotosOnlyOnWifi_Click(object sender, RoutedEventArgs e)
        {
            if (_ignorePhotoBackupToggleEvents)
                return;

            if (this.CbBackupPhotosWifiOnly.IsChecked == true)
            {
                TaskHelperFactory.Instance.addBackupPhotosWifiOnlySettings();
                TaskHelperFactory.Instance.storeSettings();

                this.CbBackupPhotos.IsChecked = true;
                this.CbBackupPhotos_Click(sender, e);
            }
            else
            {
                TaskHelperFactory.Instance.removeBackupPhotosWifiOnlySettings();
                TaskHelperFactory.Instance.storeSettings();
            }
        }


        #region DB STuff

        private void deleteAllCacheFileEntries()
        {
            using (CacheFileEntryContext cfeDbContetxt = new CacheFileEntryContext(CacheFileEntryContext.DBConnectionString))
            {
                try
                {
                    IQueryable<CacheFileEntry> cfeQuery = from cfe in cfeDbContetxt.CacheFileEntries select cfe;
                    IList<CacheFileEntry> entitiesToDelete = cfeQuery.ToList();
                    cfeDbContetxt.CacheFileEntries.DeleteAllOnSubmit(entitiesToDelete);
                    cfeDbContetxt.SubmitChanges();
                }
                catch (Exception ex)
                {
                }
            }
        }

        #endregion

    }
}