using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Data.Linq.Mapping;

namespace PlasticWonderland.Class
{
 
    [Table]
    [Index(Columns = "UniqueId", IsUnique = true)]
    public class CacheFileEntry : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private int _dummyId;

        private string _uniqueId;
        private string _fileName;
        private long _size;
        private string _mtime;


        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int DummyId
        {
            get { return _dummyId; }
            set
            {
                if (_dummyId != value)
                {
                    NotifyPropertyChanging("DummyId");
                    _dummyId = value;
                    NotifyPropertyChanged("DummyId");
                }
            }
        }

        [Column(CanBeNull = false)]
        public string UniqueId
        {
            get
            {
                return _uniqueId;
            }
            set
            {
                if (_uniqueId != value)
                {
                    NotifyPropertyChanging("UniqueId");
                    _uniqueId = value;
                    NotifyPropertyChanged("UniqueId");
                }
            }
        }

        [Column]
        public string Filename
        {
            get
            {
                return _fileName;
            }
            set
            {
                if (_fileName != value)
                {
                    NotifyPropertyChanging("Filename");
                    _fileName = value;
                    NotifyPropertyChanged("Filename");
                }
            }
        }

        [Column]
        public string Mtime
        {
            get
            {
                return _mtime;
            }
            set
            {
                if (_mtime != value)
                {
                    NotifyPropertyChanging("Mtime");
                    _mtime = value;
                    NotifyPropertyChanged("Mtime");
                }
            }
        }

        [Column]
        public long FileSize
        {
            get
            {
                return _size;
            }
            set
            {
                if (_size != value)
                {
                    NotifyPropertyChanging("FileSize");
                    _size = value;
                    NotifyPropertyChanged("FileSize");
                }
            }
        }

        // Version column aids update performance.
        [Column(IsVersion = true)]
        private Binary _version;

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify the page that a data context property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;

        // Used to notify the data context that a data context property is about to change
        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        #endregion

    }
}
