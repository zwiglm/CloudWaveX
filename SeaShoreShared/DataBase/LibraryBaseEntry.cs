using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Data.Linq.Mapping;

namespace SeaShoreShared.DataBase
{
    [Table]
    [Index(Columns = "ShoreMD5Hash", IsUnique = true)]
    public class LibraryBaseEntry
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int DummyId { get; set; }

        [Column(CanBeNull = false)]
        public string ShoreMD5Hash { get; set; }

        [Column]
        public string FileName { get; set; }

        [Column]
        public string Path { get; set; }

        [Column]
        public DateTimeOffset DateModified { get; set; }

        [Column(IsVersion = true)]
        private Binary _version { get; set; }
    }
}
