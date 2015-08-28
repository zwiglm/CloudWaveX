using System.IO;
using System.IO.IsolatedStorage;
using Windows.Storage;

public static class GlobalVariables
{
    public static string currentLibrary = "";
    public static string currentPath = "";
    public static string currentFile = "";
    public static string currentPWD = "";
    public static string currentLibraryPassword = "";
    public static IsolatedStorageSettings IsolatedStorageUserInformations = IsolatedStorageSettings.ApplicationSettings;
    public static IsolatedStorageFile ISF = IsolatedStorageFile.GetUserStoreForApplication();
    public static StorageFolder localFolder = ApplicationData.Current.LocalFolder;
    public static bool IsDebugMode = false;
    public static string DebugModeComment = "";
    public static string FolderNamePivotItem = "";
    public static double AccountInfosUsage = 0;
    public static double AccountInfosTotalSpace = 0;
}