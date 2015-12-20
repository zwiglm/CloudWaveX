using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaShoreShared
{
    public static class SharedGlobalVars
    {
        // Task helper
        public static string CHECK_PHOTO_CHANGES_TASKNAME = "CheckPhotoChangesAgent";

        // Seafile API
        public static string UPLOAD_LINK_QUALI = "upload-link";
        public static string UPDATE_LINK_QUALI = "update-link";

        // Misc
        public static int MAX_DOWNLOAD_PAGE = 80;
        public static ulong MAX_UPLOAD_SIZE = 50000000;
    }
}
