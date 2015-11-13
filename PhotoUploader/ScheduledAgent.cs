//#define DEBUG_AGENT

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Windows.Storage;
using System.Threading.Tasks;
using System;
using System.Linq;
using SeaShoreShared;
using SeaShoreShared.DataBase;


namespace PhotoUploader
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        static ScheduledAgent()
        {
            // Subscribe to the managed exception handler
            Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                Application.Current.UnhandledException += UnhandledException;
            });
        }

        /// Code to execute on Unhandled Exceptions
        private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            //TODO: Add code to perform your task in background
            string toastMessage = "";

            // If your application uses both PeriodicTask and ResourceIntensiveTask
            // you can branch your application code here. Otherwise, you don't need to.
            if (task is PeriodicTask)
            {
                // Execute periodic task actions here.
                toastMessage = string.Format("Periodic Task: {0}", task.Description);

                if (task.Name.Equals(SharedGlobalVars.CHECK_PHOTO_CHANGES_TASKNAME))
                {
                    // MaZ todo: which known libraries - get from ApplicationSettings...
                    this.iteratePictureLibary();
                }
            }
            else
            {
                // Execute resource-intensive task actions here.
                //toastMessage = "Resource-intensive task running.";
                toastMessage = string.Format("Task: {0}", task.Description);
            }

            // Launch a toast to show that the agent is running.
            // The toast will not be shown if the foreground application is running.
            ShellToast toast = new ShellToast();
            toast.Title = "Seashore Background Agent";
            toast.Content = toastMessage;
            toast.Show();

            // If debugging is enabled, launch the agent again in one minute.
#if DEBUG_AGENT
            ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(30));
#endif

            // Call NotifyComplete to let the system know the agent is done working.
            NotifyComplete();
        }

        protected override void OnCancel()
        {
            base.OnCancel();
        }


        #region Private own methods

        private async void iteratePictureLibary()
        {
            List<StorageFile> pics = await this.getFilesFromPictureLib();
        }

        private async Task retriveFilesInFolder(List<StorageFile> list, StorageFolder parent)
        {
            IReadOnlyList<StorageFile> allFiles = await parent.GetFilesAsync();
            foreach (var item in allFiles)
            {
                // MaZ todo: calculate MD5
                DateTimeOffset fileModified = (await item.GetBasicPropertiesAsync()).DateModified;
                string md5ForFile = SharedHelperFactory.Instance.CalculateMD5ForLibraryFile(item, fileModified);
                LibraryBaseEntry dbEntry = this.createDbEntry(md5ForFile, item, fileModified);

                list.Add(item);
            }

            IReadOnlyList<StorageFolder> allFolders = await parent.GetFoldersAsync();
            foreach (var item in allFolders)
            {
                await retriveFilesInFolder(list, item);
            }
        }

        private async Task<List<StorageFile>> getFilesFromPictureLib()
        {
            StorageFolder folder = KnownFolders.PicturesLibrary;
            List<StorageFile> listOfFiles = new List<StorageFile>();

            await retriveFilesInFolder(listOfFiles, folder);

            return listOfFiles;
        }

        private LibraryBaseEntry createDbEntry(string md5, StorageFile file, DateTimeOffset fileModified)
        {
            LibraryBaseEntry result = new LibraryBaseEntry()
            {
                ShoreMD5Hash = md5,
                FileName = file.Name,
                Path = file.Path,
                DateModified = fileModified,
            };
            return result;
        }

        #endregion

    }
}