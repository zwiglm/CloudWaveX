﻿using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AppSeafileClient
{
    public class MainViewModel
    {
        public ObservableCollection<LibraryRootObject> Elements { get; set; }

        public MainViewModel()
        {
            Elements = new ObservableCollection<LibraryRootObject>();
        }
    }
}
