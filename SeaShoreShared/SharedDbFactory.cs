using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SeaShoreShared.DataBase;
using Windows.Storage;

namespace SeaShoreShared
{
    public enum QualifiesAdding
    {
        No,
        ForInsert,
        ForUpdate,
    }

    public class SharedDbFactory
    {
        private static SharedDbFactory _instance;
        private IDictionary<string, LibraryBaseEntry> _allUploadEntries;

        private SharedDbFactory()
        {
            this.loadDatabase();
        }

        public static SharedDbFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SharedDbFactory();
                }
                return _instance;
            }
        }


        #region Convenience to find out if have to add, or alread in Dict

        public QualifiesAdding entryQualifiesAdding(string md5Hash, StorageFile file, ulong size, string dateOffset)
        {
            // if not there qualifies anyway
            if (!this.inDictionary(md5Hash))
                return QualifiesAdding.ForInsert;


            if (this.inDictionary(md5Hash))
            {
                LibraryBaseEntry dbEntry = this._allUploadEntries[md5Hash];
                if (dbEntry.Size != size || !dbEntry.DateModified.Equals(dateOffset))
                    return QualifiesAdding.ForUpdate;
            }

            return QualifiesAdding.No;
        }

        #endregion


        #region Private Methods

        private void loadDatabase()
        {
            using (PhotoUploadContext uploadContext = new PhotoUploadContext(PhotoUploadContext.DBConnectionString))
            {
                // Define the query to gather all of the to-do items.
                var uploadEntries =
                    from uplEnts in uploadContext.PhotoUploadEntries select uplEnts;

                // Execute the query and place the results into a collection.
                List<LibraryBaseEntry> tmpList = uploadEntries.ToList();
                IDictionary<string, LibraryBaseEntry> result = new Dictionary<string, LibraryBaseEntry>();
                foreach (var entry in tmpList)
                {
                    result.Add(entry.ShoreMD5Hash, entry);
                }
                this._allUploadEntries = result;
            }
        }

        private void insertLibraryBase(List<LibraryBaseEntry> foundEntries)
        {
            using (PhotoUploadContext uploadContext = new PhotoUploadContext(PhotoUploadContext.DBConnectionString))
            {
                uploadContext.PhotoUploadEntries.InsertAllOnSubmit(foundEntries);
                uploadContext.SubmitChanges();
            }
        }

        private void updateCacheFileEntry(string hashValue, bool uploaded)
        {
            using (PhotoUploadContext uploadContext = new PhotoUploadContext(PhotoUploadContext.DBConnectionString))
            {
                IQueryable<LibraryBaseEntry> query = from uplEnt in uploadContext.PhotoUploadEntries where uplEnt.ShoreMD5Hash == hashValue select uplEnt;
                LibraryBaseEntry found = query.FirstOrDefault();

                found.AlreadyUploaded = uploaded;

                uploadContext.SubmitChanges();
            }

            LibraryBaseEntry fromOC = this._allUploadEntries[hashValue];
            fromOC.AlreadyUploaded = uploaded;
        }

        #endregion


        public ForUploadWrapper getForUpload()
        {
            Dictionary<string, LibraryBaseEntry> dummy = 
                this._allUploadEntries.Where(q => !q.Value.AlreadyUploaded).ToDictionary(q2 => q2.Key, q2 => q2.Value);
            return new ForUploadWrapper(dummy.Count, dummy);
        }

        public ForUploadWrapper getForUpdate()
        {
            Dictionary<string, LibraryBaseEntry> dummy =
                this._allUploadEntries.Where(q => q.Value.AlreadyUploaded && q.Value.NeedsUpdate).ToDictionary(q2 => q2.Key, q2 => q2.Value);
            return new ForUploadWrapper(dummy.Count, dummy);
        }

        public bool inDictionary(string md5Hash)
        {
            return this._allUploadEntries.ContainsKey(md5Hash);
        }

        public void convenienceInsert(List<LibraryBaseEntry> foundEntries)
        {
            this.insertLibraryBase(foundEntries.Where(q => !q.AlreadyUploaded).ToList());

            foreach (var entry in foundEntries.Where(q => q.AlreadyUploaded))
            {
                this.updateCacheFileEntry(entry.ShoreMD5Hash, false);
            }
        }

        public void resetToUploaded(string hashValue)
        {
            using (PhotoUploadContext uploadContext = new PhotoUploadContext(PhotoUploadContext.DBConnectionString))
            {
                IQueryable<LibraryBaseEntry> query = from uplEnt in uploadContext.PhotoUploadEntries where uplEnt.ShoreMD5Hash == hashValue select uplEnt;
                LibraryBaseEntry found = query.FirstOrDefault();

                found.AlreadyUploaded = true;
                found.NeedsUpdate = false;

                uploadContext.SubmitChanges();
            }

            LibraryBaseEntry fromOC = this._allUploadEntries[hashValue];
            fromOC.AlreadyUploaded = true;
            fromOC.NeedsUpdate = false;
        }

    }


    public class ForUploadWrapper
    {
        public ForUploadWrapper(int count, Dictionary<string, LibraryBaseEntry> entries)
        {
            this.Count = count;
            this.Entries = entries;

            this.ValuesList = entries.Values.ToList();
        }

        public int Count { get; private set; }
        public Dictionary<string, LibraryBaseEntry> Entries { get; private set; }

        public List<LibraryBaseEntry> ValuesList { get; private set; }
    }
}
