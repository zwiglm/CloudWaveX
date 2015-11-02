using System;
using System.Diagnostics;
using System.Resources;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PlasticWonderland.Resources;
using System.Threading;
using System.IO.IsolatedStorage;
using Windows.ApplicationModel.Activation;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Xml;
using Coding4Fun.Toolkit.Controls;
using PlasticWonderland.Class;

namespace PlasticWonderland
{
    public partial class App : Application
    {
        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

        //Logger
        public static WPClogger logger = new WPClogger(LogLevel.debug);


        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions.
            UnhandledException += Application_UnhandledException;

            // Standard XAML initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Language display initialization
            InitializeLanguage();

            // Show graphics profiling information while debugging.
            if (Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode,
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Prevent the screen from turning off while under the debugger by disabling
                // the application's idle detection.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }


            // Create the database if it does not exist.
            this.createLocalDatabase();
        }


        #region DB Stuff

        private void createLocalDatabase()
        {
            using (CacheFileEntryContext db = new CacheFileEntryContext(CacheFileEntryContext.DBConnectionString))
            {
                bool DEBUG = false;
                if (!db.DatabaseExists() || DEBUG)
                {
                    //Create the database
                    try
                    {
                        if (DEBUG)
                            db.DeleteDatabase();

                        db.CreateDatabase();
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }

        #endregion


        // Code to execute when a contract activation such as a file open or save picker returns 
        // with the picked file or other return values
        private void Application_ContractActivated(object sender, Windows.ApplicationModel.Activation.IActivatedEventArgs e)
        {
            var filePickerContinuationArgs = e as FileOpenPickerContinuationEventArgs;
	        if (filePickerContinuationArgs != null)
	        {
		        this.FilePickerContinuationArgs = filePickerContinuationArgs;
	        }
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            Microsoft.Phone.Shell.PhoneApplicationService.Current.ContractActivated += Application_ContractActivated;
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {

        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {

        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {

        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (e != null)
            {
                Exception exception = e.ExceptionObject;
                if ((exception is XmlException || exception is NullReferenceException) && exception.ToString().ToUpper().Contains("INNERACTIVE"))
                {
                    Debug.WriteLine("Handled Inneractive exception {0}", exception);
                    e.Handled = true;
                    return;
                }
                else if (exception is NullReferenceException && exception.ToString().ToUpper().Contains("SOMA"))
                {
                    Debug.WriteLine("Handled Smaato null reference exception {0}", exception);
                    e.Handled = true;
                    return;
                }
                else if ((exception is System.IO.IOException || exception is NullReferenceException) && exception.ToString().ToUpper().Contains("GOOGLE"))
                {
                    Debug.WriteLine("Handled Google exception {0}", exception);
                    e.Handled = true;
                    return;
                }
                else if (exception is ObjectDisposedException && exception.ToString().ToUpper().Contains("MOBFOX"))
                {
                    Debug.WriteLine("Handled Mobfox exception {0}", exception);
                    e.Handled = true;
                    return;
                }
                else if ((exception is NullReferenceException) && exception.ToString().ToUpper().Contains("MICROSOFT.ADVERTISING"))
                {
                    Debug.WriteLine("Handled Microsoft.Advertising exception {0}", exception);
                    e.Handled = true;
                    return;
                }

            }

            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

 
        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Handle reset requests for clearing the backstack
            RootFrame.Navigated += CheckForResetNavigation;

            // Handle contract activation such as a file open or save picker
            PhoneApplicationService.Current.ContractActivated += Application_ContractActivated;


            // Assign the custom URI mapper class to the application frame.
            RootFrame.UriMapper = new CustomMapping.CustomUriMapper();

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;

            PhoneApplicationService.Current.ContractActivated += Application_ContractActivated;

            var appBar = App.Current.Resources["GlobalAppBar"] as ApplicationBar;
            ((ApplicationBarMenuItem)appBar.MenuItems[0]).Text = AppResources.AppBarHome;
            ((ApplicationBarMenuItem)appBar.MenuItems[1]).Text = AppResources.AppBarUpload;
            ((ApplicationBarMenuItem)appBar.MenuItems[2]).Text = AppResources.AppBarSettings;
            ((ApplicationBarMenuItem)appBar.MenuItems[3]).Text = AppResources.AppBarAbout;
           

            if (IsolatedStorageSettings.ApplicationSettings.Contains(GlobalVariables.TOKEN_SAVED_SET))
            {
                if (GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.TOKEN_SAVED_SET) && GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.URL_SAVED_SET))
                {
                    string t = GlobalVariables.IsolatedStorageUserInformations[GlobalVariables.TOKEN_SAVED_SET] as string;
                    string u = GlobalVariables.IsolatedStorageUserInformations[GlobalVariables.URL_SAVED_SET] as string;

                    RootFrame.Navigate(new Uri("/Pages/ListLibraryPage.xaml?token=" + t + "&url=" + u, UriKind.Relative));
                }
            }
            else
            {
                RootFrame.Navigate(new Uri("/Login.xaml", UriKind.RelativeOrAbsolute));
            }          


        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            // If the app has received a 'reset' navigation, then we need to check
            // on the next navigation to see if the page stack should be reset
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Unregister the event so it doesn't get called again
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Only clear the stack for 'new' (forward) and 'refresh' navigations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // For UI consistency, clear the entire page stack
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // do nothing
            }
        }

        #endregion

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }

        private void ApplicationBarIconButtonAbout_Click(object sender, EventArgs e)
        {
            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void ApplicationBarIconButtonBack_Click_1(object sender, EventArgs e)
        {
            if (GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.TOKEN_SAVED_SET) && GlobalVariables.IsolatedStorageUserInformations.Contains(GlobalVariables.URL_SAVED_SET))
            {
                GlobalVariables.IsolatedStorageUserInformations.Remove(GlobalVariables.TOKEN_SAVED_SET);
                GlobalVariables.IsolatedStorageUserInformations.Remove(GlobalVariables.URL_SAVED_SET);
            }
        }

        private void ApplicationBarIconButtonAdd_Click_1(object sender, EventArgs e)
        {
            Uri currentUri =  (Application.Current.RootVisual as PhoneApplicationFrame).CurrentSource;
            if (currentUri.ToString().StartsWith("/Pages/ContentLibraryPage.xaml") == true)
            {
                ChooseFile();
            }
            else
            {
                var messagePrompt = new MessagePrompt
                {
                    Title = AppResources.ListLibrary_Error_NoUpload_Title,
                    Message = AppResources.ListLibrary_Error_NoUpload_Content
                };
                messagePrompt.Show();
            }

            
        }

        private void ChooseFile()
        {
            FileOpenPicker singleFilePicker = new FileOpenPicker();
            singleFilePicker.ViewMode = PickerViewMode.Thumbnail;
            singleFilePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            singleFilePicker.FileTypeFilter.Add("*");
            singleFilePicker.PickSingleFileAndContinue();
        }

        public FileOpenPickerContinuationEventArgs FilePickerContinuationArgs { get; set; }

        private void ApplicationBarIconButtonSettings_Click(object sender, EventArgs e)
        {
            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Pages/SettingsPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void ApplicationBarIconButtonHome_Click(object sender, EventArgs e)
        {
            GlobalVariables.currentPath = "";
            GlobalVariables.FolderNamePivotItem = AppResources.ContentLibrary_PivotTitle_1;
            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Pages/ListLibraryPage.xaml?token=" + GlobalVariables.IsolatedStorageUserInformations[GlobalVariables.TOKEN_SAVED_SET] as string + "&url=" + GlobalVariables.IsolatedStorageUserInformations[GlobalVariables.URL_SAVED_SET] as string, UriKind.Relative));
                   
        }


    }
}