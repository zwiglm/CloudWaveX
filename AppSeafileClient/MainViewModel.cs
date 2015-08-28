using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AppSeafileClient
{
    public class MainViewModel
    {
        public ObservableCollection<LibraryRootObject> Elements { get; set; }

        public MainViewModel()
        {
            Elements = new ObservableCollection<LibraryRootObject>();

            if (DesignerProperties.IsInDesignTool)
            {
                // Fill Datacontext with fictional data
                LoadFakeData();
            }
        }

        public void LoadFakeData()
        {
            Elements.Add(new LibraryRootObject() { name = "file1", desc = "Images/Tomato.png", type = "file" , size = 100});
            Elements.Add(new LibraryRootObject() { name = "folder1", desc = "Images/Tomato.png", type = "dir", size = 100 });
            Elements.Add(new LibraryRootObject() { name = "folder2", desc = "Images/Tomato.png", type = "dir", size = 100 });
            Elements.Add(new LibraryRootObject() { name = "folder3", desc = "Images/Tomato.png", type = "dir", size = 100 });
            Elements.Add(new LibraryRootObject() { name = "file3", desc = "Images/Tomato.png", type = "file", size = 100 });
            Elements.Add(new LibraryRootObject() { name = "file4", desc = "Images/Tomato.png", type = "file", size = 100 });

            Elements.Add(new LibraryRootObject() { name = "Lib1", desc = "lib 1 private", type = "repo", encrypted = true, size = 100 });
            Elements.Add(new LibraryRootObject() { name = "Lib2", desc = "lib 2 public jsdhfdskjhfsdkjf hsdkjf hsd", type = "repo", encrypted = false, size = 100 });

            Elements.Add(new LibraryRootObject() { name = "Lib1 share", desc = "lib 1 private", type = "srepo", encrypted = true, size = 100 });
            Elements.Add(new LibraryRootObject() { name = "Lib2 share", desc = "lib 2 public jsdhfdskjhfsdkjf hsdkjf hsd", type = "srepo", encrypted = false, size = 100 });
        }
    }
}
