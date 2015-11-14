using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaShoreShared.DataBase
{
    public class PhotoUploadContext : DataContext
    {
        public static string DBConnectionString = "Data Source=isostore:/PhotoUploadEntries.sdf";

        public PhotoUploadContext(string connectionString)
            : base(connectionString)
        {
        }

        public Table<LibraryBaseEntry> PhotoUploadEntries
        {
            get
            {
                return this.GetTable<LibraryBaseEntry>();
            }
        }
    }
}
