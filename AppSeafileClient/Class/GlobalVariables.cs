using System.IO;
using System.IO.IsolatedStorage;
using Windows.Storage;

public static class GlobalVariables
{
    public static string WEB_CLIENT_AGENT = "Seashore for Seafile/";

    public static string FILE_AS_FILE = "file";
    public static string TYPEFILE_AS_TYPEFILE = "type_file";

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


    // setting tokens
    public static string TOKEN_SAVED_SET = "tokensaved";
    public static string URL_SAVED_SET = "urlsaved";
    public static string IGNORE_SELF_SIGNED_SET = "ignoreSelfSignedSet";
    public static string SETTINGS_BACKUP_PHOTOS = "settingsBackupPhotos";
    public static string SETTINGS_BACKUP_PHOTOS_WIFI_ONLY = "settingsBackupPhotosWifiOnly";
    public static string DOWNLOAD_THUMBNAILS = "downloadThumbs";

    // Seafile API request tokens
    public static string SF_REQ_REPOS = "repos";
    public static string SF_REQ_SHARED_REPOS = "shared-repos";          // those that I have shared to someone else
    public static string SF_REQ_BE_SHARED_REPOS = "beshared-repos";     // those shared to me

    // Seafile API json respons
    public static string SF_RESP_REPOS = "repo";
    public static string SF_RESP_SHARED_REPOS = "srepo";                // personally shared repo
    public static string SF_RESP_GROUP_REPOS = "grepo";                 // group shared rep

    // Seafile API helper
    public static string SHARED_REPO_HELPER = "h_sRepo";
    public static string GROUP_SPLITTER = "group_splitter";

    // Task helper
    public static string CHECK_PHOTO_CHANGES_TASKNAME = "CheckPhotoChangesAgent";
}