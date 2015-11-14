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
using Windows.Storage.FileProperties;
using SeaShoreShared.Resources;


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

            // If your application uses both PeriodicTask and ResourceIntensiveTask
            // you can branch your application code here. Otherwise, you don't need to.
            if (task is PeriodicTask)
            {
                // Execute periodic task actions here.
                if (task.Name.Equals(SharedGlobalVars.CHECK_PHOTO_CHANGES_TASKNAME))
                {
                    List<LibraryBaseEntry> notUploaded = this.iteratePictureLibary();

                    if (notUploaded.Count > 0)
                    {
                        this.showToast(
                            PhotoUploadResource.Background_Agent_Title,
                            String.Format(PhotoUploadResource.Backgroun_Agent_FilesForUpload, notUploaded.Count),
                            "");
                    }
                }
            }
            else
            {
                // Execute resource-intensive task actions here.
            }

            // If debugging is enabled, launch the agent again in one minute.
#if DEBUG_AGENT
            ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(30));
#endif

            // Call NotifyComplete to let the system know the agent is done working.
            NotifyComplete();
        }

        #region Private own methods

        private List<LibraryBaseEntry> iteratePictureLibary()
        {
            StorageFolder folder = KnownFolders.PicturesLibrary;
            Task<List<LibraryBaseEntry>> picturesTask = retriveFilesInFolder(folder);
            return picturesTask.Result;
        }


        private async Task<List<LibraryBaseEntry>> retriveFilesInFolder(StorageFolder parent)
        {
            List<LibraryBaseEntry> result = new List<LibraryBaseEntry>();

            IReadOnlyList<StorageFile> allFiles = await this.getFileList(parent);
            foreach (var item in allFiles)
            {
                BasicProperties basicProps = await this.getBasicProps(item);
                string fileModified = basicProps.DateModified.ToString();
                string md5ForFile = SharedHelperFactory.Instance.CalculateMD5ForLibraryFile(item, fileModified);
                
                // check if already in DB
                if (SharedDbFactory.Instance.entryQualifiesAdding(md5ForFile, item, basicProps.Size, fileModified))
                {
                    LibraryBaseEntry dbEntry = this.createDbEntry(md5ForFile, item, fileModified, basicProps.Size);
                    result.Add(dbEntry);
                }
            }

            IReadOnlyList<StorageFolder> allFolders = await this.getFolderList(parent);
            foreach (var item in allFolders)
	        {
                List<LibraryBaseEntry> inSubFolder = await this.retriveFilesInFolder(item);
                result.AddRange(inSubFolder);
	        }

            return result;
        }


        private async Task<IReadOnlyList<StorageFile>> getFileList(StorageFolder parent)
        {
            return await parent.GetFilesAsync();
        }
        private async Task<IReadOnlyList<StorageFolder>> getFolderList(StorageFolder parent)
        {
            return await parent.GetFoldersAsync();
        }
        private async Task<BasicProperties> getBasicProps(StorageFile item)
        {
            return await item.GetBasicPropertiesAsync();
        }


        private LibraryBaseEntry createDbEntry(string md5, StorageFile file, string fileModified, ulong size)
        {
            LibraryBaseEntry result = new LibraryBaseEntry()
            {
                ShoreMD5Hash = md5,
                FileName = file.Name,
                Path = file.Path,
                DateModified = fileModified,
                Size = size,
            };
            return result;
        }

        #endregion


        #region Private UI

        private void showToast(string title, string message, string sound)
        {
            ShellToast toast = new ShellToast();
            toast.Title = title;
            toast.Content = message;
            toast.Sound = new Uri(sound, UriKind.RelativeOrAbsolute);
            toast.Show();
        }

        #endregion

    }
}