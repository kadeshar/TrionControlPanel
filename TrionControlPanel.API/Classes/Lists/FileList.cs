using System.Numerics;

namespace TrionControlPanel.API.Classes.Lists
{
    public class FileList
    {
        public string Name { get; set; }
        public double Size { get; set; }
        public string Hash { get; set; }
        public string Path { get; set; }
    }


    // Define a DTO class
    public class FileRequest
    {
        public string FilePath { get; set; }
    }
    public class SupporterKey
    {
        public int ID { get; set; }
        public string Key { get; set; }
        public long UID { get; set; }
    }
}
