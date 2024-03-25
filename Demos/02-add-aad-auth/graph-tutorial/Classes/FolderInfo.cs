using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace graph_tutorial.Classes
{
    public class FolderInfo
    {
        public string FolderName { get; set; }
        public List<string> FileNames { get; set; }

        public FolderInfo(string folderName, List<string> fileNames)
        {
            FolderName = folderName;
            FileNames = fileNames;
        }
    }
}