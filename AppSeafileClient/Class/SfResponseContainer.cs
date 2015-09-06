public class LibraryRootObject
{
    /// <summary>
    /// Common props
    /// </summary>
    public string permission { get; set; }
    public bool encrypted { get; set; }

    /// <summary>
    /// Response from List Libraries
    /// </summary>
    public int mtime { get; set; }
    public string owner { get; set; }
    public string id { get; set; }
    public long size { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public bool @virtual { get; set; }
    public string desc { get; set; }
    public string root { get; set; }

    /// <summary>
    /// Response from Shared-Libs
    /// </summary>
    public string repo_id { get; set; }
    public string share_type { get; set; }
    public string user { get; set; }
    public int last_modified { get; set; }
    public string repo_desc { get; set; }
    public int group_id { get; set; }
    public string repo_name { get; set; }

    /// <summary>
    /// Be-Shared-Libs
    /// </summary>
    public bool enc_version { get; set; }
    public bool is_virtual { get; set; }


    /// <summary>
    /// 
    /// </summary>
    public string token { get; set; }
    public string email { get; set; }
    public long usage { get; set; }
    public long total { get; set;}
}