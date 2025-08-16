
namespace TrionControlPanel.Desktop.Extensions.Modules.Lists
{
    public class ExpansionInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public Func<bool> IsInstalled { get; set; }
        public Action<bool> SetInstalled { get; set; }
        public string InstallLocation { get; set; }
        public string WorkingDirectory { get; set; }
        public Action<bool> SetLaunchCore { get; set; }
    }
}
