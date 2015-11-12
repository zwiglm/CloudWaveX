using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Phone.Scheduler;
using SeaShoreShared;

namespace PlasticWonderland.Domain
{
    public class TaskHelperFactory
    {
        private static TaskHelperFactory _instance;


        private TaskHelperFactory()
        {
        }

        public static TaskHelperFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TaskHelperFactory();
                }
                return _instance;
            }
        }


        #region Settings in regard to tasks

        public bool enabledBackupPhotos()
        {
            bool result = GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.SETTINGS_BACKUP_PHOTOS);
            return result;
        }
        public bool enabledBackupPhotosWifiOnly()
        {
            bool result = GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.SETTINGS_BACKUP_PHOTOS_WIFI_ONLY);
            return result;
        }

        public void storeSettings()
        {
            GlobalVariables.IsolatedStorageUserInformations.Save();
        }
        public void addBackupPhotoSetting()
        {
            if (!GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.SETTINGS_BACKUP_PHOTOS))
                GlobalVariables.IsolatedStorageUserInformations.Add(GlobalVariables.SETTINGS_BACKUP_PHOTOS, true);
            else
                GlobalVariables.IsolatedStorageUserInformations[GlobalVariables.SETTINGS_BACKUP_PHOTOS] = true;
        }
        public void removeBackupPhotoSetting()
        {
            if (GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.SETTINGS_BACKUP_PHOTOS))
                GlobalVariables.IsolatedStorageUserInformations.Remove(GlobalVariables.SETTINGS_BACKUP_PHOTOS);
        }
        public void addBackupPhotosWifiOnlySettings()
        {
            if (!GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.SETTINGS_BACKUP_PHOTOS_WIFI_ONLY))
                GlobalVariables.IsolatedStorageUserInformations.Add(GlobalVariables.SETTINGS_BACKUP_PHOTOS_WIFI_ONLY, true);
            else
                GlobalVariables.IsolatedStorageUserInformations[GlobalVariables.SETTINGS_BACKUP_PHOTOS_WIFI_ONLY] = true;
        }
        public void removeBackupPhotosWifiOnlySettings()
        {
            if (GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.SETTINGS_BACKUP_PHOTOS_WIFI_ONLY))
                GlobalVariables.IsolatedStorageUserInformations.Remove(GlobalVariables.SETTINGS_BACKUP_PHOTOS_WIFI_ONLY);
        }

        #endregion 


        public void startIteratingPicturesAgent()
        {
            // Variable for tracking enabled status of background agents for this app.
            //_agentsEnabled = true;

            // Obtain a reference to the period task, if one exists
            PeriodicTask checkPhotoChangesTask = ScheduledActionService.Find(SharedGlobalVars.CHECK_PHOTO_CHANGES_TASKNAME) as PeriodicTask;

            // If the task already exists and background agents are enabled for the
            // application, you must remove the task and then add it again to update 
            // the schedule
            if (checkPhotoChangesTask != null)
            {
                RemoveTaskAgent(SharedGlobalVars.CHECK_PHOTO_CHANGES_TASKNAME);
            }

            checkPhotoChangesTask = new PeriodicTask(SharedGlobalVars.CHECK_PHOTO_CHANGES_TASKNAME);

            // The description is required for periodic agents. This is the string that the user
            // will see in the background services Settings page on the device.
            checkPhotoChangesTask.Description = "Iterating photos for changes";

            // Place the call to Add in a try block in case the user has disabled agents.
            try
            {
                ScheduledActionService.Add(checkPhotoChangesTask);

                // If debugging is enabled, use LaunchForTest to launch the agent in one minute.
                //#if(DEBUG_AGENT)
                //    ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(60));
                //#endif
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    MessageBox.Show("Background agents for this application have been disabled by the user.");
                    //_agentsEnabled = false;

                    TaskHelperFactory.Instance.removeBackupPhotoSetting();
                    TaskHelperFactory.Instance.storeSettings();
                }

                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.

                }
                TaskHelperFactory.Instance.removeBackupPhotoSetting();
                TaskHelperFactory.Instance.storeSettings();
            }
            catch (SchedulerServiceException)
            {
                // No user action required.
                TaskHelperFactory.Instance.removeBackupPhotoSetting();
                TaskHelperFactory.Instance.storeSettings();
            }
        }

        public void RemoveTaskAgent(string name)
        {
            try
            {
                ScheduledActionService.Remove(name);
            }
            catch (Exception)
            {
            }
        }


    }
}
