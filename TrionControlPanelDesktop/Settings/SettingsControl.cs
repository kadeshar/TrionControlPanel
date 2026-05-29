using System.Diagnostics;
using static TrionLibrary.Models.Enums;
using TrionLibrary.Setting;
using TrionControlPanelDesktop.Data;
using TrionControlPanelDesktop.Download;
using TrionLibrary.Sys;
using TrionLibrary.Database;
using TrionLibrary.Network;
using TrionControlPanelDesktop.Settings;
namespace TrionControlPanelDesktop.Controls
{
    public partial class SettingsControl : UserControl
    {
        public static bool RefreshData = false;
        static System.Threading.Timer TextTimer;
        private SynchronizationContext _syncContext;
        public SettingsControl()
        {
            Dock = DockStyle.Fill;
            InitializeComponent();
            _syncContext = SynchronizationContext.Current!;
            InitializeBackupTab();
        }
        private TrionControlPanel.UI.CustomToggleButton TGLBackupEnabled;
        private MetroFramework.Controls.MetroTextBox TXTBackupIntervalDays;
        private MetroFramework.Controls.MetroTextBox TXTBackupDatabaseLocation;
        private MetroFramework.Controls.MetroTextBox TXTBackupFolder;
        private MetroFramework.Controls.MetroTextBox TXTBackupRetentionCount;
        private Label LBLLastBackupDate;
        private UI.Controls.CustomButton BTNBackupNow;
        private void InitializeBackupTab()
        {
            var tabPageBackup = new TabPage();
            tabPageBackup.BackColor = Color.FromArgb(45, 51, 59);
            tabPageBackup.Location = new Point(4, 34);
            tabPageBackup.Name = "TabPageBackup";
            tabPageBackup.Size = new Size(837, 332);
            tabPageBackup.Text = "Backup";
            tabPageBackup.ToolTipText = "View the database backup settings";

            var panelBackup = new MetroFramework.Controls.MetroPanel();
            panelBackup.BackColor = Color.FromArgb(28, 33, 40);
            panelBackup.Border = true;
            panelBackup.BorderColor = Color.Black;
            panelBackup.BorderSize = 1;
            panelBackup.CustomBackground = true;
            panelBackup.HorizontalScrollbar = false;
            panelBackup.VerticalScrollbar = false;
            panelBackup.Location = new Point(6, 6);
            panelBackup.Size = new Size(825, 320);
            panelBackup.Style = MetroFramework.MetroColorStyle.Blue;
            panelBackup.Theme = MetroFramework.MetroThemeStyle.Dark;

            int yPos = 10;

            // Enable Backup toggle
            var lblBackupEnabled = new Label();
            lblBackupEnabled.Text = "Enable Backup Prompt:";
            lblBackupEnabled.ForeColor = Color.White;
            lblBackupEnabled.Location = new Point(10, yPos);
            lblBackupEnabled.AutoSize = true;
            panelBackup.Controls.Add(lblBackupEnabled);

            TGLBackupEnabled = new TrionControlPanel.UI.CustomToggleButton();
            TGLBackupEnabled.Location = new Point(250, yPos - 3);
            TGLBackupEnabled.MinimumSize = new Size(45, 22);
            TGLBackupEnabled.Size = new Size(45, 22);
            TGLBackupEnabled.CheckedChanged += TGLBackupEnabled_CheckedChanged;
            panelBackup.Controls.Add(TGLBackupEnabled);

            yPos += 40;

            // Backup Interval
            var lblInterval = new Label();
            lblInterval.Text = "Backup Interval (days):";
            lblInterval.ForeColor = Color.White;
            lblInterval.Location = new Point(10, yPos);
            lblInterval.AutoSize = true;
            panelBackup.Controls.Add(lblInterval);

            TXTBackupIntervalDays = new MetroFramework.Controls.MetroTextBox();
            TXTBackupIntervalDays.Location = new Point(250, yPos - 3);
            TXTBackupIntervalDays.Size = new Size(80, 25);
            TXTBackupIntervalDays.Style = MetroFramework.MetroColorStyle.Blue;
            TXTBackupIntervalDays.Theme = MetroFramework.MetroThemeStyle.Dark;
            TXTBackupIntervalDays.UseStyleColors = true;
            TXTBackupIntervalDays.Leave += TXTBackupIntervalDays_Leave;
            panelBackup.Controls.Add(TXTBackupIntervalDays);

            yPos += 40;

            // Database Location
            var lblDbLoc = new Label();
            lblDbLoc.Text = "MySQL Data Location:";
            lblDbLoc.ForeColor = Color.White;
            lblDbLoc.Location = new Point(10, yPos);
            lblDbLoc.AutoSize = true;
            panelBackup.Controls.Add(lblDbLoc);

            TXTBackupDatabaseLocation = new MetroFramework.Controls.MetroTextBox();
            TXTBackupDatabaseLocation.Location = new Point(250, yPos - 3);
            TXTBackupDatabaseLocation.Size = new Size(430, 25);
            TXTBackupDatabaseLocation.Style = MetroFramework.MetroColorStyle.Blue;
            TXTBackupDatabaseLocation.Theme = MetroFramework.MetroThemeStyle.Dark;
            TXTBackupDatabaseLocation.UseStyleColors = true;
            TXTBackupDatabaseLocation.ReadOnly = true;
            panelBackup.Controls.Add(TXTBackupDatabaseLocation);

            var btnBrowseDbLoc = new UI.Controls.CustomButton();
            btnBrowseDbLoc.Text = "Browse";
            btnBrowseDbLoc.Location = new Point(690, yPos - 5);
            btnBrowseDbLoc.Size = new Size(80, 30);
            btnBrowseDbLoc.BackColor = Color.FromArgb(28, 33, 40);
            btnBrowseDbLoc.ForeColor = Color.White;
            btnBrowseDbLoc.FlatStyle = FlatStyle.Flat;
            btnBrowseDbLoc.FlatAppearance.BorderSize = 1;
            btnBrowseDbLoc.Click += BTNBrowseBackupDbLocation_Click;
            panelBackup.Controls.Add(btnBrowseDbLoc);

            yPos += 40;

            // Backup Folder
            var lblBackupFolder = new Label();
            lblBackupFolder.Text = "Backup Folder:";
            lblBackupFolder.ForeColor = Color.White;
            lblBackupFolder.Location = new Point(10, yPos);
            lblBackupFolder.AutoSize = true;
            panelBackup.Controls.Add(lblBackupFolder);

            TXTBackupFolder = new MetroFramework.Controls.MetroTextBox();
            TXTBackupFolder.Location = new Point(250, yPos - 3);
            TXTBackupFolder.Size = new Size(430, 25);
            TXTBackupFolder.Style = MetroFramework.MetroColorStyle.Blue;
            TXTBackupFolder.Theme = MetroFramework.MetroThemeStyle.Dark;
            TXTBackupFolder.UseStyleColors = true;
            TXTBackupFolder.ReadOnly = true;
            panelBackup.Controls.Add(TXTBackupFolder);

            var btnBrowseBackup = new UI.Controls.CustomButton();
            btnBrowseBackup.Text = "Browse";
            btnBrowseBackup.Location = new Point(690, yPos - 5);
            btnBrowseBackup.Size = new Size(80, 30);
            btnBrowseBackup.BackColor = Color.FromArgb(28, 33, 40);
            btnBrowseBackup.ForeColor = Color.White;
            btnBrowseBackup.FlatStyle = FlatStyle.Flat;
            btnBrowseBackup.FlatAppearance.BorderSize = 1;
            btnBrowseBackup.Click += BTNBrowseBackupFolder_Click;
            panelBackup.Controls.Add(btnBrowseBackup);

            yPos += 40;

            var lblBackupRetention = new Label();
            lblBackupRetention.Text = "Keep Latest Backups:";
            lblBackupRetention.ForeColor = Color.White;
            lblBackupRetention.Location = new Point(10, yPos);
            lblBackupRetention.AutoSize = true;
            panelBackup.Controls.Add(lblBackupRetention);

            TXTBackupRetentionCount = new MetroFramework.Controls.MetroTextBox();
            TXTBackupRetentionCount.Location = new Point(250, yPos - 3);
            TXTBackupRetentionCount.Size = new Size(80, 25);
            TXTBackupRetentionCount.Style = MetroFramework.MetroColorStyle.Blue;
            TXTBackupRetentionCount.Theme = MetroFramework.MetroThemeStyle.Dark;
            TXTBackupRetentionCount.UseStyleColors = true;
            TXTBackupRetentionCount.Leave += TXTBackupRetentionCount_Leave;
            panelBackup.Controls.Add(TXTBackupRetentionCount);

            yPos += 40;

            // Last Backup Date
            var lblLastBackup = new Label();
            lblLastBackup.Text = "Last Backup:";
            lblLastBackup.ForeColor = Color.White;
            lblLastBackup.Location = new Point(10, yPos);
            lblLastBackup.AutoSize = true;
            panelBackup.Controls.Add(lblLastBackup);

            LBLLastBackupDate = new Label();
            LBLLastBackupDate.Text = "Never";
            LBLLastBackupDate.ForeColor = Color.FromArgb(0, 174, 219);
            LBLLastBackupDate.Location = new Point(250, yPos);
            LBLLastBackupDate.AutoSize = true;
            panelBackup.Controls.Add(LBLLastBackupDate);

            yPos += 40;

            // Backup Now button
            BTNBackupNow = new UI.Controls.CustomButton();
            BTNBackupNow.Text = "Backup Now";
            BTNBackupNow.Location = new Point(250, yPos - 5);
            BTNBackupNow.Size = new Size(120, 30);
            BTNBackupNow.BackColor = Color.FromArgb(28, 33, 40);
            BTNBackupNow.ForeColor = Color.White;
            BTNBackupNow.FlatStyle = FlatStyle.Flat;
            BTNBackupNow.FlatAppearance.BorderSize = 1;
            BTNBackupNow.Click += BTNBackupNow_Click;
            panelBackup.Controls.Add(BTNBackupNow);

            tabPageBackup.Controls.Add(panelBackup);
            TBControler.TabPages.Add(tabPageBackup);
        }
        private void LoadBackupData()
        {
            TGLBackupEnabled.Checked = Setting.List.BackupEnabled;
            TXTBackupIntervalDays.Text = Setting.List.BackupIntervalDays.ToString();
            TXTBackupDatabaseLocation.Text = Setting.List.BackupDatabaseLocation ?? "N/A";
            TXTBackupFolder.Text = Setting.List.BackupFolder ?? "N/A";
            TXTBackupRetentionCount.Text = Setting.List.BackupRetentionCount.ToString();

            if (!string.IsNullOrEmpty(Setting.List.LastBackupDate) &&
                DateTime.TryParse(Setting.List.LastBackupDate, out DateTime lastBackup))
            {
                LBLLastBackupDate.Text = lastBackup.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                LBLLastBackupDate.Text = "Never";
            }
        }
        private void TGLBackupEnabled_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.BackupEnabled = TGLBackupEnabled.Checked;
        }
        private void TXTBackupIntervalDays_Leave(object sender, EventArgs e)
        {
            if (int.TryParse(TXTBackupIntervalDays.Text, out int days) && days > 0)
            {
                Setting.List.BackupIntervalDays = days;
            }
            else
            {
                TXTBackupIntervalDays.Text = Setting.List.BackupIntervalDays.ToString();
            }
        }
        private async void TXTBackupRetentionCount_Leave(object sender, EventArgs e)
        {
            if (int.TryParse(TXTBackupRetentionCount.Text, out int retentionCount) && retentionCount > 0)
            {
                Setting.List.BackupRetentionCount = retentionCount;
                await Setting.Save();
            }
            else
            {
                TXTBackupRetentionCount.Text = Setting.List.BackupRetentionCount.ToString();
            }
        }
        private async void BTNBrowseBackupDbLocation_Click(object sender, EventArgs e)
        {
            string folder = SettingsData.GetWorkingDirectory();
            if (!string.IsNullOrEmpty(folder))
            {
                Setting.List.BackupDatabaseLocation = folder;
                TXTBackupDatabaseLocation.Text = folder;
                await Setting.Save();
            }
        }
        private async void BTNBackupNow_Click(object sender, EventArgs e)
        {
            var mainForm = this.FindForm() as TrionControlPanelDesktop.MainForm;
            if (mainForm != null)
            {
                await mainForm.BackupNow();
                LoadBackupData();
            }
        }
        private async void BTNBrowseBackupFolder_Click(object sender, EventArgs e)
        {
            string folder = SettingsData.GetWorkingDirectory();
            if (!string.IsNullOrEmpty(folder))
            {
                Setting.List.BackupFolder = folder;
                TXTBackupFolder.Text = folder;
                await Setting.Save();
            }
        }
        private void EnableCustomNames()
        {
            TXTBoxLoginExecName.ReadOnly = !Setting.List.CustomNames;
            TXTBoxWorldExecName.ReadOnly = !Setting.List.CustomNames;
            TXTBoxMySQLExecName.ReadOnly = !Setting.List.CustomNames;
        }
        private void CBoxSelectItems()
        {
            switch (Setting.List.SelectedCore)
            {
                case Cores.AscEmu:
                    ComboBoxCores.SelectedItem = "AscEmu";
                    break;
                case Cores.AzerothCore:
                    ComboBoxCores.SelectedItem = "AzerothCore";
                    break;
                case Cores.CMaNGOS:
                    ComboBoxCores.SelectedItem = "CMaNGOS";
                    break;
                case Cores.CypherCore:
                    ComboBoxCores.SelectedItem = "CypherCore";
                    break;
                case Cores.TrinityCore:
                    ComboBoxCores.SelectedItem = "TrinityCore";
                    break;
                case Cores.TrinityCore335:
                    ComboBoxCores.SelectedItem = "CypherCore";
                    break;
                case Cores.TrinityCoreClassic:
                    ComboBoxCores.SelectedItem = "TrinityCore Classic";
                    break;
                case Cores.VMaNGOS:
                    ComboBoxCores.SelectedItem = "VMaNGOS";
                    break;
            }
            switch (Setting.List.SelectedSPP)
            {
                case SPP.Classic:
                    ComboBoxSPPVersion.SelectedItem = "World of Warcraft - Classic";
                    break;
                case SPP.TheBurningCrusade:
                    ComboBoxSPPVersion.SelectedItem = "World of Warcraft - The Burning Crusade";
                    break;
                case SPP.WrathOfTheLichKing:
                    ComboBoxSPPVersion.SelectedItem = "World of Warcraft - Wrath of the Lich King";
                    break;
                case SPP.Cataclysm:
                    ComboBoxSPPVersion.SelectedItem = "World of Warcraft - Cataclysm";
                    break;
                case SPP.MistOfPandaria:
                    ComboBoxSPPVersion.SelectedItem = "World of Warcraft - Mists of Pandaria";
                    break;
            }
            switch (Setting.List.DDNSerivce)
            {
                case DDNSerivce.Afraid:
                    ComboBoxDDNService.SelectedItem = "freedns.afraid.org";
                    break;
                case DDNSerivce.AllInkl:
                    ComboBoxDDNService.SelectedItem = "all-inkl.com";
                    break;
                case DDNSerivce.Cloudflare:
                    ComboBoxDDNService.SelectedItem = "cloudflare.com";
                    break;
                case DDNSerivce.DuckDNS:
                    ComboBoxDDNService.SelectedItem = "duckdns.org";
                    break;
                case DDNSerivce.NoIP:
                    ComboBoxDDNService.SelectedItem = "noip.com";
                    break;
                case DDNSerivce.Dynu:
                    ComboBoxDDNService.SelectedItem = "dynu.com";
                    break;
                case DDNSerivce.dynDNS:
                    ComboBoxDDNService.SelectedItem = "dyn.com";
                    break;
                case DDNSerivce.Enom:
                    ComboBoxDDNService.SelectedItem = "enom.com";
                    break;
                case DDNSerivce.Freemyip:
                    ComboBoxDDNService.SelectedItem = "freemyip.com";
                    break;
                case DDNSerivce.OVH:
                    ComboBoxDDNService.SelectedItem = "ovhcloud.com";
                    break;
                case DDNSerivce.STRATO:
                    ComboBoxDDNService.SelectedItem = "strato.de";
                    break;
            }
        }
        private void LoadData()
        {
            //Load Installed Emulators
            TGLClassicLaunch.Checked = Setting.List.LaunchClassicCore;
            TGLTBCLaunch.Checked = Setting.List.LaunchTBCCore;
            TGLWotLKLaunch.Checked = Setting.List.LaunchWotLKCore;
            TGLCataLaunch.Checked = Setting.List.LaunchCataCore;
            TGLMoPLaunch.Checked = Setting.List.LaunchMoPCore;
            TGLCustomInstalled.Checked = Setting.List.LaunchCustomCore;
            //Load Names
            TXTBoxLoginExecName.Text = Setting.List.CustomLogonExeName;
            TXTBoxWorldExecName.Text = Setting.List.CustomWorldExeName;
            TXTBoxMySQLExecName.Text = Setting.List.DBExeName;
            //Working Directory
            TXTBoxCoreLocation.Text = Setting.List.CustomWorkingDirectory;
            TXTBoxMySQLLocation.Text = Setting.List.DBLocation;
            //MySQL Host Data
            TXTMysqlHost.Text = Setting.List.DBServerHost;
            TXTMysqlPort.Text = Setting.List.DBServerPort;
            TXTMysqlUser.Text = Setting.List.DBServerUser;
            TXTMysqlPassword.Text = Setting.List.DBServerPassword;
            //MysqlDatabases
            TXTCharDatabase.Text = Setting.List.CharactersDatabase;
            TXTAuthDatabase.Text = Setting.List.AuthDatabase;
            TXTWorldDatabase.Text = Setting.List.WorldDatabase;
            //ToggleButtons
            TGLAutoUpdateTrion.Checked = Setting.List.AutoUpdateTrion;
            TGLAutoUpdateCore.Checked = Setting.List.AutoUpdateCore;
            TGLHideConsole.Checked = Setting.List.ConsolHide;
            TGLNotificationSound.Checked = Setting.List.NotificationSound;
            TGLStayInTrey.Checked = Setting.List.StayInTray;
            TGLCustomNames.Checked = Setting.List.CustomNames;
            TGLRunTrionStartup.Checked = Setting.List.RunWithWindows;
            TGLServerCrashDetection.Checked = Setting.List.ServerCrashDetection;
            //Update Loader
            EnableCustomNames();
            //Update Labels
            LBLTrionVersion.Text = $"Trion Version: Local {User.UI.Version.OFF.Trion} / Online: {User.UI.Version.ON.Trion}";
            LBLDBVersion.Text = $"Database Version:  Local: {User.UI.Version.OFF.Database} / Online: {User.UI.Version.ON.Database} ";
            LBLClassicVersion.Text = $"Classic Version: \n •Local: {User.UI.Version.OFF.Classic} \n •Online: {User.UI.Version.ON.Classic} ";
            LBLTBCVersion.Text = $"TBC Version: \n •Local: {User.UI.Version.OFF.TBC} \n •Online: {User.UI.Version.ON.TBC} ";
            LBLWotLKVersion.Text = $"WotLK Version: \n •Local: {User.UI.Version.OFF.WotLK} \n •Online: {User.UI.Version.ON.WotLK} ";
            LBLCataVersion.Text = $"Cata Version: \n •Local: {User.UI.Version.OFF.Cata} \n •Online: {User.UI.Version.ON.Cata} ";
            LBLMoPVersion.Text = $"MoP Version: \n •Local: {User.UI.Version.OFF.Mop} \n •Online: {User.UI.Version.ON.Mop} ";
            //DDNS
            TXTDDNSDomain.Text = Setting.List.DDNSDomain;
            TXTDDNSUsername.Text = Setting.List.DDNSUsername;
            TXTDDNSPassword.Text = Setting.List.DDNSPassword;
            TXTDDNSInterval.Text = Setting.List.DDNSInterval.ToString();
            TGLDDNSRunOnStartup.Checked = Setting.List.DDNSRunOnStartup;
            CBoxSelectItems();
            GeteSelectedDatabase();
            LoadBackupData();
            User.UI.Form.StartUpLoading++;
        }
        private void GeteSelectedDatabase()
        {
            if (Setting.List.SelectedDatabases == Databases.Classic)
            {
                TGLClassicDB.Checked = true;
            }
            if (Setting.List.SelectedDatabases == Databases.TBC)
            {
                TGLTbcDB.Checked = true;
            }
            if (Setting.List.SelectedDatabases == Databases.WotLK)
            {
                TGLWotlkDB.Checked = true;
            }
            if (Setting.List.SelectedDatabases == Databases.Cata)
            {
                TGLCataDB.Checked = true;
            }
            if (Setting.List.SelectedDatabases == Databases.MoP)
            {
                TGLMopDB.Checked = true;
            }
            if (Setting.List.SelectedDatabases == Databases.Custom)
            {
                TGLCustomDB.Checked = true;
                TXTAuthDatabase.ReadOnly = false;
                TXTCharDatabase.ReadOnly = false;
                TXTWorldDatabase.ReadOnly = false;
                TXTMysqlHost.ReadOnly = false;
                TXTMysqlPort.ReadOnly = false;
                TXTMysqlUser.ReadOnly = false;
                TXTMysqlPassword.ReadOnly = false;
            }
        }
        private void ComboBoxCores_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            switch (ComboBoxCores.SelectedItem)
            {
                case "AscEmu":
                    Setting.List.CustomWorldExeName = "world";
                    Setting.List.CustomLogonExeName = "logon";
                    Setting.List.SelectedCore = Cores.AscEmu;
                    Setting.List.CharactersDatabase = "ascemu_char";
                    Setting.List.WorldDatabase = "ascemu_world";
                    Setting.List.AuthDatabase = "ascemu_logon";
                    Setting.List.DBServerUser = "root";
                    Setting.List.DBServerPassword = "root";
                    break;
                case "AzerothCore":
                    Setting.List.CustomWorldExeName = "worldserver";
                    Setting.List.CustomLogonExeName = "authserver";
                    Setting.List.SelectedCore = Cores.AzerothCore;
                    Setting.List.CharactersDatabase = "acore_characters";
                    Setting.List.WorldDatabase = "acore_world";
                    Setting.List.AuthDatabase = "acore_auth";
                    Setting.List.DBServerUser = "acore";
                    Setting.List.DBServerPassword = "acore";
                    break;
                case "CMaNGOS":
                    Setting.List.CustomWorldExeName = "mangosd";
                    Setting.List.CustomLogonExeName = "realmd";
                    Setting.List.SelectedCore = Cores.CMaNGOS;
                    Setting.List.CharactersDatabase = "characters";
                    Setting.List.WorldDatabase = "mangos";
                    Setting.List.AuthDatabase = "realmd";
                    Setting.List.DBServerUser = "mangos";
                    Setting.List.DBServerPassword = "mangos";
                    break;
                case "CypherCore":
                    Setting.List.CustomWorldExeName = "WorldServer";
                    Setting.List.CustomLogonExeName = "BNetServer";
                    Setting.List.SelectedCore = Cores.CypherCore;
                    Setting.List.CharactersDatabase = "characters";
                    Setting.List.WorldDatabase = "world";
                    Setting.List.AuthDatabase = "auth";
                    Setting.List.DBServerUser = "trinity";
                    Setting.List.DBServerPassword = "trinity";
                    break;
                case "TrinityCore":
                    Setting.List.CustomWorldExeName = "worldserver";
                    Setting.List.CustomLogonExeName = "bnetserver";
                    Setting.List.SelectedCore = Cores.TrinityCore;
                    Setting.List.CharactersDatabase = "characters";
                    Setting.List.WorldDatabase = "world";
                    Setting.List.AuthDatabase = "auth";
                    Setting.List.DBServerUser = "trinity";
                    Setting.List.DBServerPassword = "trinity";
                    break;
                case "TrinityCore 3.3.5":
                    Setting.List.CustomLogonExeName = "authserver";
                    Setting.List.CustomWorldExeName = "worldserver";
                    Setting.List.SelectedCore = Cores.TrinityCore335;
                    Setting.List.CharactersDatabase = "characters";
                    Setting.List.WorldDatabase = "world";
                    Setting.List.AuthDatabase = "auth";
                    Setting.List.DBServerUser = "trinity";
                    Setting.List.DBServerPassword = "trinity";
                    break;
                case "TrinityCore Classic":
                    Setting.List.CustomLogonExeName = "bnetserver";
                    Setting.List.CustomWorldExeName = "worldserver";
                    Setting.List.SelectedCore = Cores.TrinityCoreClassic;
                    Setting.List.CharactersDatabase = "characters";
                    Setting.List.WorldDatabase = "world";
                    Setting.List.AuthDatabase = "auth";
                    Setting.List.DBServerUser = "trinity";
                    Setting.List.DBServerPassword = "trinity";
                    break;
                case "VMaNGOS":
                    Setting.List.CustomWorldExeName = "mangosd";
                    Setting.List.CustomLogonExeName = "realmd";
                    Setting.List.SelectedCore = Cores.VMaNGOS;
                    Setting.List.CharactersDatabase = "characters";
                    Setting.List.WorldDatabase = "mangos";
                    Setting.List.AuthDatabase = "realmd";
                    Setting.List.DBServerUser = "mangos";
                    Setting.List.DBServerPassword = "mangos";
                    break;
            }
            TXTBoxLoginExecName.Text = Setting.List.CustomLogonExeName;
            TXTBoxWorldExecName.Text = Setting.List.CustomWorldExeName;
        }
        private void TGLStayInTrey_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.StayInTray = TGLStayInTrey.Checked;
        }
        private void TGLNotificationSound_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.NotificationSound = TGLNotificationSound.Checked;
        }
        private void TGLHideConsole_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.ConsolHide = TGLHideConsole.Checked;
        }
        private void TGLAutoUpdateTrion_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.AutoUpdateTrion = TGLAutoUpdateTrion.Checked;
        }
        private void TGLAutoUpdateCore_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.AutoUpdateCore = TGLAutoUpdateCore.Checked;
        }
        private void TGLAutoUpdateMySQL_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.AutoUpdateMySQL = TGLAutoUpdateMySQL.Checked;
        }
        private void TGLCustomNames_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.CustomNames = TGLCustomNames.Checked;
            EnableCustomNames();
        }
        private void TGLServerStartup_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.RunServerWithWindows = TGLServerStartup.Checked;
        }
        private async void BTNMySQLExecLocation_Click(object sender, EventArgs e)
        {
            string Folder = SettingsData.GetWorkingDirectory();
            try
            {
                if (Folder != string.Empty)
                {
                    if (Infos.GetExecutableLocation(Folder, Setting.List.DBExeLoc) != string.Empty)
                    {
                        Setting.List.DBExeLoc = Infos.GetExecutableLocation(Folder, Setting.List.DBExeLoc);
                        Setting.List.DBWorkingDir = Infos.GetExecutableLocation(Folder, Setting.List.DBExeLoc);
                        Setting.List.DBLocation = Path.GetFullPath(Path.Combine(Setting.List.DBExeLoc, @"..\"));
                        await Setting.Save();
                        Setting.CreateMySQLConfigFile(Directory.GetCurrentDirectory());
                        await Setting.Save();
                        LoadData();
                    }
                    else
                    {
                        Infos.Message = "Faild to find MySQL Lication.";
                    }
                }
                else
                {
                    Infos.Message = "Faild to select the folder.";
                }
            }
            catch (Exception ex)
            {
                Infos.Message = ex.Message;
            }
        }
        private async void BTNCoreExecLovation_Click(object sender, EventArgs e)
        {
            string Folder = SettingsData.GetWorkingDirectory();
            if (Folder != string.Empty)
            {
                if (Infos.GetExecutableLocation(Folder, Setting.List.CustomWorldExeName) != string.Empty &&
                    Infos.GetExecutableLocation(Folder, Setting.List.CustomLogonExeName) != string.Empty)
                {
                    Setting.List.CustomWorldExeLoc = Infos.GetExecutableLocation(Folder, Setting.List.CustomWorldExeName);
                    Setting.List.CustomWorldExeLoc = Infos.GetExecutableLocation(Folder, Setting.List.CustomLogonExeName);
                    Setting.List.CustomWorkingDirectory = Path.GetFullPath(Folder);
                    await Setting.Save();
                    LoadData();
                }
            }
            else
            {
                Infos.Message = "Getting Core File location failed!";
            }
        }
        private void TimerWacher_Tick(object sender, EventArgs e)
        {
            if (DownloadData.Infos.Install.Classic == true ||
                DownloadData.Infos.Install.TBC == true ||
                DownloadData.Infos.Install.WotLK == true ||
                DownloadData.Infos.Install.Cata == true ||
                DownloadData.Infos.Install.Mop == true)
            {
                BTNInstallSPP.Enabled = false;
                BTNUninstallSPP.Enabled = false;
                BTNRepairSPP.Enabled = false;
                LBLReadingFiles.Visible = true;
            }
            else
            {
                BTNInstallSPP.Enabled = true;
                BTNUninstallSPP.Enabled = true;
                BTNRepairSPP.Enabled = true;
                LBLReadingFiles.Visible = false;
            }
        }
        private void BTNDiscord_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Links.Discord);
        }
        private void SaveDataTextbox(object state)
        {
            // Seve data
            Setting.List.DBExeName = TXTBoxMySQLExecName.Text;
            Setting.List.CustomWorldExeName = TXTBoxWorldExecName.Text;
            Setting.List.CustomLogonExeName = TXTBoxLoginExecName.Text;
            Setting.List.DBServerHost = TXTMysqlHost.Text;
            Setting.List.DBServerPort = TXTMysqlPort.Text;
            Setting.List.DBServerUser = TXTMysqlUser.Text;
            Setting.List.DBServerPassword = TXTMysqlPassword.Text;
            Setting.List.AuthDatabase = TXTAuthDatabase.Text;
            Setting.List.WorldDatabase = TXTWorldDatabase.Text;
            Setting.List.CharactersDatabase = TXTCharDatabase.Text;
            Setting.List.DDNSDomain = TXTDDNSDomain.Text;
            Setting.List.DDNSUsername = TXTDDNSUsername.Text;
            Setting.List.DDNSPassword = TXTDDNSPassword.Text;
            Setting.List.DDNSInterval = Convert.ToInt32(TXTDDNSInterval.Text);
        }
        private void TXTBox_TextChanged(object sender, EventArgs e)
        {
            // Stop the timer if it's running
            TextTimer?.Dispose();
            // Start a new timer
            TextTimer = new(SaveDataTextbox, null, 1000, Timeout.Infinite);
        }
        private async void BTNTrionUpdate_Click(object sender, EventArgs e)
        {
            BTNTrionUpdate.Enabled = false;
            if (User.UI.Version.Update.Trion)
            {
                Infos.Message = "Updateing Trion!";
                DownloadData.Infos.Install.Trion = true;
                await Main.StartUpdate(Links.Install.Trion, $"{Links.MainCDNHost}{Links.Hashe.Trion}", true);
            }
            if (User.UI.Version.Update.Database) { await Main.StartUpdate(Links.Install.Database, $"{Links.MainCDNHost}{Links.Hashe.Database}", true); }
            if (User.UI.Version.Update.Classic) { await Main.StartUpdate(Links.Install.Classic, $"{Links.MainCDNHost}{Links.Hashe.Classic}", true); }
            if (User.UI.Version.Update.TBC) { await Main.StartUpdate(Links.Install.TBC, $"{Links.MainCDNHost}{Links.Hashe.TBC}", true); }
            if (User.UI.Version.Update.WotLK) { await Main.StartUpdate(Links.Install.WotLK, $"{Links.MainCDNHost}{Links.Hashe.WotLK}", true); }
            if (User.UI.Version.Update.Cata) { await Main.StartUpdate(Links.Install.Cata, $"{Links.MainCDNHost}{Links.Hashe.Cata}", true); }
            if (User.UI.Version.Update.Mop) { await Main.StartUpdate(Links.Install.Mop, $"{Links.MainCDNHost}{Links.Hashe.Mop}", true); }
            BTNTrionUpdate.Enabled = true;
        }
        private void TGLRunTrionStartup_CheckedChanged(object sender, EventArgs e)
        {
            if (TGLRunTrionStartup.Checked == true) { SettingsData.AddToStartup("Trion Control Panel", Application.ExecutablePath.ToString()); Setting.List.RunWithWindows = true; }
            if (TGLRunTrionStartup.Checked == false) { SettingsData.RemoveFromStartup("Trion Control Panel"); Setting.List.RunWithWindows = false; }
        }
        private async void BTNTestConnection_Click(object sender, EventArgs e)
        {
            if (await Connect.Test() == true)
            {
                BTNTestConnection.ForeColor = Color.Green;
                BTNTestConnection.Text = "   Success!!";
                TimerConnectSucess.Start();
            }
            else
            {
                BTNTestConnection.ForeColor = Color.Red;
                BTNTestConnection.Text = "   Failed!!";
                TimerConnectSucess.Start();
            }
        }
        private void TimerConnectSucess_Tick(object sender, EventArgs e)
        {
            TimerConnectSucess.Stop();
            BTNTestConnection.ForeColor = Color.White;
            BTNTestConnection.Text = "   Test Connection";
        }
        private void BTNCoreOpenFolder_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Setting.List.CustomWorkingDirectory);
        }
        private void BTNMySQLOpenFolder_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Setting.List.DBLocation);
        }
        private async void BTNDeleteAuth_Click(object sender, EventArgs e)
        {
            BTNDeleteAuth.Text = "   Working!!";
            BTNDeleteAuth.ForeColor = Color.Orange;
            BTNDeleteAuth.Click -= BTNDeleteAuth_Click;
            // Get all tables in the database
            List<string> tables = await Access.GetTables(Connect.String(Setting.List.AuthDatabase));
            // Delete each table
            foreach (string table in tables)
            {
                Access.DeleteTable(Connect.String(Setting.List.AuthDatabase), table);
            }
            Infos.Message = "Auth Tables deleted successfully.";
            BTNDeleteAuth.Click += BTNDeleteAuth_Click;
            BTNDeleteAuth.Text = "   Delete Auth Database";
            BTNDeleteAuth.ForeColor = Color.White;
        }
        private async void BTNDeleteChar_Click(object sender, EventArgs e)
        {
            BTNDeleteChar.Text = "   Working!!";
            BTNDeleteChar.ForeColor = Color.Orange;
            BTNDeleteChar.Click -= BTNDeleteChar_Click;
            // Get all tables in the database
            List<string> tables = await Access.GetTables(Connect.String(Setting.List.AuthDatabase));
            // Delete each table
            foreach (string table in tables)
            {
                Access.DeleteTable(Connect.String(Setting.List.AuthDatabase), table);
            }
            Infos.Message = "Char Tables deleted successfully.";
            BTNDeleteChar.Click += BTNDeleteChar_Click;
            BTNDeleteChar.Text = "   Delete Char Database";
            BTNDeleteChar.ForeColor = Color.White;
        }
        private async void BTNDeleteWorld_Click(object sender, EventArgs e)
        {
            BTNDeleteWorld.Text = "   Working!!";
            BTNDeleteWorld.ForeColor = Color.Orange;
            BTNDeleteWorld.Click -= BTNDeleteWorld_Click;
            // Get all tables in the database
            List<string> tables = await Access.GetTables(Connect.String(Setting.List.WorldDatabase));
            // Delete each table
            foreach (string table in tables)
            {
                Access.DeleteTable(Connect.String(Setting.List.WorldDatabase), table);
            }
            Infos.Message = "Wolrd Tables deleted successfully.";
            BTNDeleteWorld.Click += BTNDeleteWorld_Click;
            BTNDeleteWorld.Text = "   Delete World Database";
            BTNDeleteWorld.ForeColor = Color.White;
        }
        private void ComboBoxDDNService_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            switch (ComboBoxDDNService.SelectedItem)
            {
                case "freedns.afraid.org":
                    Setting.List.DDNSerivce = DDNSerivce.Afraid;
                    break;
                case "all-inkl.com":
                    Setting.List.DDNSerivce = DDNSerivce.AllInkl;
                    break;
                case "cloudflare.com":
                    Setting.List.DDNSerivce = DDNSerivce.Cloudflare;
                    break;
                case "duckdns.org":
                    Setting.List.DDNSerivce = DDNSerivce.dynDNS;
                    break;
                case "noip.com":
                    Setting.List.DDNSerivce = DDNSerivce.NoIP;
                    break;
                case "dynu.com":
                    Setting.List.DDNSerivce = DDNSerivce.Dynu;
                    break;
                case "dyn.com":
                    Setting.List.DDNSerivce = DDNSerivce.dynDNS;
                    break;
                case "enom.com":
                    Setting.List.DDNSerivce = DDNSerivce.Enom;
                    break;
                case "freemyip.com":
                    Setting.List.DDNSerivce = DDNSerivce.Freemyip;
                    break;
                case "ovhcloud.com":
                    Setting.List.DDNSerivce = DDNSerivce.OVH;
                    break;
                case "strato.de":
                    Setting.List.DDNSerivce = DDNSerivce.STRATO;
                    break;
            }
        }
        private void TXTDDNSInterval_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsNumber(e.KeyChar);
        }
        private void TGLDDNSRunOnStartup_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.DDNSRunOnStartup = TGLDDNSRunOnStartup.Checked;
        }
        private async void TimerDDNSInterval_Tick(object sender, EventArgs e)
        {
            TimerDDNSInterval.Interval = Setting.List.DDNSInterval;
            string CurrentIP = await Helper.GetExternalIpAddress();
            string URL = await User.API.DDNSUpdateURL(TXTDDNSDomain.Text, TXTDDNSUsername.Text, TXTDDNSPassword.Text);
            if (!CurrentIP.Contains(Setting.List.IPAddress))
            {
                Infos.Message = $"Updateing {URL} with {CurrentIP}";
                SettingsData.UpdateDNSIP(URL, CurrentIP);
            }
        }
        private void BTNSaveData_Click(object sender, EventArgs e)
        {
            TimerDDNSInterval.Start();
        }
        private void BTNWebiste_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Links.DDNSWebsits());
        }
        private void ComboBoxSPPVersion_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            switch (ComboBoxSPPVersion.SelectedItem)
            {
                case "World of Warcraft - Classic":
                    Setting.List.SelectedSPP = SPP.Classic;
                    break;
                case "World of Warcraft - The Burning Crusade":
                    Setting.List.SelectedSPP = SPP.TheBurningCrusade;
                    break;
                case "World of Warcraft - Wrath of the Lich King":
                    Setting.List.SelectedSPP = SPP.WrathOfTheLichKing;
                    break;
                case "World of Warcraft - Cataclysm":
                    Setting.List.SelectedSPP = SPP.Cataclysm;
                    break;
                case "World of Warcraft - Mists of Pandaria":
                    Setting.List.SelectedSPP = SPP.MistOfPandaria;
                    break;
            }
        }
        private async void BTNInstallSPP_Click(object sender, EventArgs e)
        {
            switch (Setting.List.SelectedSPP)
            {
                case SPP.Classic:
                    BTNInstallSPP.Enabled = false;
                    if (!Setting.List.ClassicInstalled)
                    {
                        if (User.UI.Version.ON.Classic == $"N/A")
                        {
                            Infos.Message = "World of Warcraft - Classic is not available yet!";
                            break;
                        }
                        DownloadControl.Title = "Install World of Warcraft - Classic";
                        DownloadData.Infos.Install.Classic = true;
                        await DownlaodDatabase(false);
                        await StartInstall(Links.Install.Classic, $"{Links.MainCDNHost}{Links.Hashe.Classic}", true);
                    }
                    else
                    {
                        Infos.Message = "World of Warcraft - Classic already installed! To fix problems with the emulator, try the Repair button!";
                    }
                    BTNInstallSPP.Enabled = true;
                    break;
                case SPP.TheBurningCrusade:
                    BTNInstallSPP.Enabled = false;
                    if (!Setting.List.TBCInstalled)
                    {
                        if (User.UI.Version.ON.TBC == $"N/A")
                        {
                            Infos.Message = "World of Warcraft - The Burning Crusade is not available yet!";
                            break;
                        }
                        DownloadControl.Title = "Install World of Warcraft - The Burning Crusade";
                        DownloadData.Infos.Install.TBC = true;
                        await DownlaodDatabase(false);
                        await StartInstall(Links.Install.TBC, $"{Links.MainCDNHost}{Links.Hashe.TBC}", true);
                    }
                    else
                    {
                        Infos.Message = "World of Warcraft - The Burning Crusade already installed! To fix problems with the emulator, try the Repair button!";
                    }
                    BTNInstallSPP.Enabled = true;
                    break;
                case SPP.WrathOfTheLichKing:
                    BTNInstallSPP.Enabled = false;
                    if (!Setting.List.WotLKInstalled)
                    {
                        if (User.UI.Version.ON.WotLK == $"N/A")
                        {
                            Infos.Message = "World of Warcraft - Wrath of the Lich King is not available yet!";
                            break;
                        }
                        DownloadControl.Title = "Install World of Warcraft - Wrath of the Lich King";
                        DownloadData.Infos.Install.WotLK = true;
                        await DownlaodDatabase(false);
                        await StartInstall(Links.Install.WotLK, $"{Links.MainCDNHost}{Links.Hashe.WotLK}", true);
                    }
                    else
                    {
                        Infos.Message = "World of Warcraft - Wrath of the Lich King already installed! To fix problems with the emulator, try the Repair button!";
                    }
                    BTNInstallSPP.Enabled = true;
                    break;
                case SPP.Cataclysm:
                    BTNInstallSPP.Enabled = false;
                    if (!Setting.List.CataInstalled)
                    {
                        if (User.UI.Version.ON.Cata == $"N/A")
                        {
                            Infos.Message = "World of Warcraft - Cataclysm is not available yet!";
                            break;
                        }
                        DownloadControl.Title = "Install World of Warcraft - Cataclysm";
                        DownloadData.Infos.Install.Cata = true;
                        await DownlaodDatabase(false);
                        await StartInstall(Links.Install.Cata, $"{Links.MainCDNHost}{Links.Hashe.Cata}", true);
                    }
                    else
                    {
                        Infos.Message = "World of Warcraft - Cataclysm already installed! To fix problems with the emulator, try the Repair button!";
                    }
                    BTNInstallSPP.Enabled = true;
                    break;
                case SPP.MistOfPandaria:
                    BTNInstallSPP.Enabled = false;
                    if (!Setting.List.MOPInstalled)
                    {
                        if (User.UI.Version.ON.Mop == $"N/A")
                        {
                            Infos.Message = "World of Warcraft - Mists of Pandaria is not available yet!";
                            break;
                        }
                        DownloadControl.Title = "Install World of Warcraft - Mists of Pandaria";
                        DownloadData.Infos.Install.Mop = true;
                        await DownlaodDatabase(false);
                        await StartInstall(Links.Install.Mop, $"{Links.MainCDNHost}{Links.Hashe.Mop}", true);
                    }
                    else
                    {
                        Infos.Message = "World of Warcraft -  Mists of Pandaria already installed! To fix problems with the emulator, try the Repair button!";
                    }
                    BTNInstallSPP.Enabled = true;
                    break;
            }
        }
        private async Task DownlaodDatabase(bool startDownload)
        {
            if (!Setting.List.DBInstalled)
            {
                if (!Directory.Exists(Links.Install.Database)) { Directory.CreateDirectory(Links.Install.Database); }
                if (MessageBox.Show("It seems you need a database server. Would you like to download it?", "Question.", MessageBoxButtons.YesNo, MessageBoxIcon.None) == DialogResult.Yes)
                {

                    DownloadControl.Title = "Install Database";
                    DownloadData.Infos.Install.Database = true;
                    await StartInstall(Links.Install.Database, $"{Links.MainCDNHost}{Links.Hashe.Database}", startDownload);
                }
                else
                {
                    if (MessageBox.Show("The emulators require a database server. Are you sure you don't want to download it?", "Question.", MessageBoxButtons.YesNo, MessageBoxIcon.None) == DialogResult.No)
                    {
                        DownloadControl.Title = "Install Database";
                        DownloadData.Infos.Install.Database = true;
                        await StartInstall(Links.Install.Database, $"{Links.MainCDNHost}{Links.Hashe.Database}", startDownload);
                    }
                }
            }
        }
        private async Task StartInstall(string Directory, string WebLink, bool startDownload)
        {
            var progress = new Progress<string>(value => { LBLReadingFiles.Text = value; });
            await Task.Run(async () => await DownloadControl.CompareAndExportChangesOnline(Directory, WebLink, progress, startDownload));
        }
        private async static Task StartUninstall(string targetDirectory)
        {
            try
            {
                // Delete all files
                string[] files = Directory.GetFiles(targetDirectory);
                foreach (string file in files)
                {
                    await Task.Run(() => File.Delete(file));
                }

                // Delete all directories
                string[] directories = Directory.GetDirectories(targetDirectory);
                foreach (string directory in directories)
                {
                    await Task.Run(() => Directory.Delete(directory, true));
                }
            }
            catch (Exception ex)
            {
                Infos.Message = $"An error occurred: {ex.Message}";
            }
        }
        private async void BTNRepairSPP_Click(object sender, EventArgs e)
        {
            switch (Setting.List.SelectedSPP)
            {
                case SPP.Classic:
                    BTNRepairSPP.Enabled = false;
                    if (Setting.List.ClassicInstalled && !User.UI.Form.ClassicWorldRunning && !User.UI.Form.ClassicLogonRunning)
                    {
                        if (User.UI.Version.ON.Classic == $"N/A")
                        {
                            Infos.Message = "World of Warcraft - Classic is not available yet!";
                            break;
                        }
                        DownloadControl.Title = "Repair World of Warcraft - Classic";
                        DownloadData.Infos.Install.Classic = true;
                        await DownlaodDatabase(false);
                        await StartInstall(Links.Install.Classic, $"{Links.MainCDNHost}{Links.Hashe.Classic}", true);
                    }
                    else
                    {
                        if (!Setting.List.ClassicInstalled)
                        {
                            Infos.Message = "World of Warcraft – Classic is not installed! Install the Repack first!";
                            break;
                        }
                        Infos.Message = "World of Warcraft - Classic is Running! To fix the repack pleas stop the server first!";
                    }
                    BTNRepairSPP.Enabled = true;
                    break;
                case SPP.TheBurningCrusade:
                    BTNRepairSPP.Enabled = false;
                    if (Setting.List.TBCInstalled && !User.UI.Form.TBCWorldRunning && !User.UI.Form.TBCLogonRunning)
                    {
                        if (User.UI.Version.ON.TBC == $"N/A")
                        {
                            Infos.Message = "World of Warcraft - The Burning Crusade is not available yet!";
                            break;
                        }
                        DownloadControl.Title = "Repair World of Warcraft - The Burning Crusade";
                        DownloadData.Infos.Install.TBC = true;
                        await DownlaodDatabase(false);
                        await StartInstall(Links.Install.TBC, $"{Links.MainCDNHost}{Links.Hashe.TBC}", true);
                    }
                    else
                    {
                        if (!Setting.List.TBCInstalled)
                        {
                            Infos.Message = "World of Warcraft – The Burning Crusade is not installed! Install the Repack first!";
                            break;
                        }
                        Infos.Message = "World of Warcraft - The Burning Crusade is Running! To fix the repack pleas stop the server first!";
                    }
                    BTNRepairSPP.Enabled = true;
                    break;
                case SPP.WrathOfTheLichKing:
                    BTNRepairSPP.Enabled = false;
                    if (Setting.List.WotLKInstalled && !User.UI.Form.WotLKWorldRunning && !User.UI.Form.WotLKLogonRunning)
                    {
                        if (User.UI.Version.ON.WotLK == $"N/A")
                        {
                            Infos.Message = "World of Warcraft - Wrath of the Lich King is not available yet!";
                            break;
                        }
                        DownloadControl.Title = "Repair World of Warcraft - Wrath of the Lich King";
                        DownloadData.Infos.Install.WotLK = true;
                        await DownlaodDatabase(false);
                        await StartInstall(Links.Install.WotLK, $"{Links.MainCDNHost}{Links.Hashe.WotLK}", true);
                    }
                    else
                    {
                        if (!Setting.List.WotLKInstalled)
                        {
                            Infos.Message = "World of Warcraft – Wrath of the Lich King is not installed! Install the Repack first!";
                            break;
                        }
                        Infos.Message = "World of Warcraft - Wrath of the Lich King is Running! To fix the repack pleas stop the server first!";
                    }
                    BTNRepairSPP.Enabled = true;
                    break;
                case SPP.Cataclysm:
                    BTNRepairSPP.Enabled = false;
                    if (Setting.List.CataInstalled && !User.UI.Form.CataWorldRunning && !User.UI.Form.CataLogonRunning)
                    {
                        if (User.UI.Version.ON.Cata == $"N/A")
                        {
                            Infos.Message = "World of Warcraft - Cataclysm is not available yet!";
                            break;
                        }
                        DownloadControl.Title = "Repair World of Warcraft - Cataclysm";
                        DownloadData.Infos.Install.Cata = true;
                        await DownlaodDatabase(false);
                        await StartInstall(Links.Install.Cata, $"{Links.MainCDNHost}{Links.Hashe.Cata}", true);
                    }
                    else
                    {
                        if (!Setting.List.CataInstalled)
                        {
                            Infos.Message = "World of Warcraft – Cataclys is not installed! Install the Repack first!";
                            break;
                        }
                        Infos.Message = "World of Warcraft - Cataclysm is Running! To fix the repack pleas stop the server first!";
                    }
                    BTNRepairSPP.Enabled = true;
                    break;
                case SPP.MistOfPandaria:
                    BTNRepairSPP.Enabled = false;
                    if (Setting.List.MOPInstalled && !User.UI.Form.CataWorldRunning && !User.UI.Form.CataLogonRunning)
                    {
                        if (User.UI.Version.ON.Mop == $"N/A")
                        {
                            Infos.Message = "World of Warcraft - Mists of Pandaria is not available yet!";
                            break;
                        }
                        DownloadControl.Title = "Repair World of Warcraft - Mists of Pandaria";
                        DownloadData.Infos.Install.Mop = true;
                        await DownlaodDatabase(false);
                        await StartInstall(Links.Install.Mop, $"{Links.MainCDNHost}{Links.Hashe.Mop}", true);
                    }
                    else
                    {
                        if (!Setting.List.MOPInstalled)
                        {
                            Infos.Message = "World of Warcraft – Mists of Pandaria is not installed! Install the Repack first!";
                            break;
                        }
                        Infos.Message = "World of Warcraft - Mists of Pandaria is Running! To fix the repack pleas stop the server first!";
                    }
                    BTNRepairSPP.Enabled = true;
                    break;
            }
        }
        private async void BTNUninstallSPP_Click(object sender, EventArgs e)
        {
            switch (Setting.List.SelectedSPP)
            {
                case SPP.Classic:
                    if (Setting.List.ClassicInstalled && !User.UI.Form.ClassicWorldRunning && !User.UI.Form.ClassicLogonRunning)
                    {
                        BTNUninstallSPP.Enabled = false;
                        Infos.Message = "World of Warcraft – Classic will be uninstalled!";
                        BTNUninstallSPP.Text = "Working!!";
                        await StartUninstall(Links.Install.Classic);
                        BTNUninstallSPP.Text = "Uninstall S.P.P.";
                        BTNUninstallSPP.Enabled = true;
                    }
                    else
                    {
                        if (!Setting.List.ClassicInstalled)
                        {
                            Infos.Message = "World of Warcraft – Classic is not installed! Install the Repack first!";
                            break;
                        }
                        Infos.Message = "World of Warcraft - Classic is Running! To uninstall the repack pleas stop the server first!";
                    }
                    break;
                case SPP.TheBurningCrusade:
                    if (Setting.List.TBCInstalled && !User.UI.Form.TBCWorldRunning && !User.UI.Form.TBCLogonRunning)
                    {
                        BTNUninstallSPP.Enabled = false;
                        Infos.Message = "World of Warcraft – The Burning Crusade will be uninstalled!";
                        BTNUninstallSPP.Text = "Working!!";
                        await StartUninstall(Links.Install.TBC);
                        BTNUninstallSPP.Text = "Uninstall S.P.P.";
                        BTNUninstallSPP.Enabled = true;
                    }
                    else
                    {
                        if (!Setting.List.TBCInstalled)
                        {
                            Infos.Message = "World of Warcraft – The Burning Crusade is not installed! Install the Repack first!";
                            break;
                        }
                        Infos.Message = "World of Warcraft - The Burning Crusade is Running! To uninstall the repack pleas stop the server first!";
                    }
                    break;
                case SPP.WrathOfTheLichKing:
                    if (Setting.List.WotLKInstalled && !User.UI.Form.WotLKWorldRunning && !User.UI.Form.WotLKLogonRunning)
                    {
                        BTNUninstallSPP.Enabled = false;
                        Infos.Message = "World of Warcraft – Wrath of the Lich King will be uninstalled!";
                        BTNUninstallSPP.Text = "Working!!";
                        await StartUninstall(Links.Install.WotLK);
                        BTNUninstallSPP.Text = "Uninstall S.P.P.";
                        BTNUninstallSPP.Enabled = true;
                    }
                    else
                    {
                        if (!Setting.List.WotLKInstalled)
                        {
                            Infos.Message = "World of Warcraft – Wrath of the Lich King is not installed! Install the Repack first!";
                            break;
                        }
                        Infos.Message = "World of Warcraft - Wrath of the Lich King is Running! To uninstall the repack pleas stop the server first!";
                    }
                    break;
                case SPP.Cataclysm:
                    if (Setting.List.CataInstalled && !User.UI.Form.CataWorldRunning && !User.UI.Form.CataLogonRunning)
                    {
                        BTNUninstallSPP.Enabled = false;
                        Infos.Message = "World of Warcraft – Cataclys will be uninstalled!";
                        BTNUninstallSPP.Text = "Working!!";
                        await StartUninstall(Links.Install.Cata);
                        BTNUninstallSPP.Text = "Uninstall S.P.P.";
                        BTNUninstallSPP.Enabled = true;
                    }
                    else
                    {
                        if (!Setting.List.CataInstalled)
                        {
                            Infos.Message = "World of Warcraft – Cataclys is not installed! Install the Repack first!";
                            break;
                        }
                        Infos.Message = "World of Warcraft - Cataclysm is Running! To uninstall the repack pleas stop the server first!";
                    }
                    break;
                case SPP.MistOfPandaria:
                    if (Setting.List.MOPInstalled && !User.UI.Form.CataWorldRunning && !User.UI.Form.CataLogonRunning)
                    {
                        BTNUninstallSPP.Enabled = false;
                        Infos.Message = "World of Warcraft – Mists of Pandaria will be uninstalled!";
                        BTNUninstallSPP.Text = "Working!!";
                        await StartUninstall(Links.Install.Mop);
                        BTNUninstallSPP.Text = "Uninstall S.P.P.";
                        BTNUninstallSPP.Enabled = true;
                    }
                    else
                    {
                        if (!Setting.List.MOPInstalled)
                        {
                            Infos.Message = "World of Warcraft – Mists of Pandaria is not installed! Install the Repack first!";
                            break;
                        }
                        Infos.Message = "World of Warcraft - Mists of Pandaria is Running! To uninstall the repack pleas stop the server first!";
                    }
                    break;
            }
        }
        private void SettingsControl_Load(object sender, EventArgs e)
        {
            LoadData();
        }
        private void TimerEnDis_Tick(object sender, EventArgs e)
        {
            if (RefreshData)
            {
                RefreshData = false;
                LoadData();
            }
        }

        private async void BTNDownlaodMySQL_Click(object sender, EventArgs e)
        {
            DownloadControl.Title = "Install Database";
            DownloadData.Infos.Install.Database = true;
            await StartInstall(Links.Install.Database, $"{Links.MainCDNHost}{Links.Hashe.Database}", true);
        }



        private void BTNAscEmuWebsite_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Links.Emulators.AscEmu);
        }

        private void BTNACoreWebsite_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Links.Emulators.AzerothCore);
        }

        private void BTNSkyFireWebsite_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Links.Emulators.SkyFire);
        }

        private void BTNCMangosWebsite_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Links.Emulators.CMaNGOS);
        }

        private void BTNCypherWebsite_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Links.Emulators.CypherCore);
        }

        private void BTNFirelandsWebsite_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Links.Emulators.FirelandsCore);
        }
        private void BTNTrinityCoreWebsite_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Links.Emulators.TrinityCore);
        }
        private void BTNVMangosWebsite_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Links.Emulators.VMaNGOS);
        }
        private void TGLClassicLaunch_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.LaunchClassicCore = TGLClassicLaunch.Checked;
        }
        private void TGLTBCLaunch_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.LaunchTBCCore = TGLTBCLaunch.Checked;
        }
        private void TGLWotLKLaunch_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.LaunchWotLKCore = TGLWotLKLaunch.Checked;
        }
        private void TGLCataLaunch_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.LaunchCataCore = TGLCataLaunch.Checked;
        }
        private void TGLMoPLaunch_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.LaunchMoPCore = TGLMoPLaunch.Checked;
        }

        private void TGLServerCrashDetection_CheckedChanged(object sender, EventArgs e)
        {
            Setting.List.ServerCrashDetection = TGLServerCrashDetection.Checked;
        }

        private void TGLClassicDB_Click(object sender, EventArgs e)
        {
            if (TGLClassicDB.Checked == true)
            {
                TXTMysqlPassword.ReadOnly = true;
                TGLTbcDB.Checked = false;
                TGLWotlkDB.Checked = false;
                TGLCataDB.Checked = false;
                TGLMopDB.Checked = false;
                TGLCustomDB.Checked = false;
                Setting.List.AuthDatabase = "classic_auth";
                Setting.List.CharactersDatabase = "classic_characters";
                Setting.List.WorldDatabase = "classic_world";
                Setting.List.DBServerHost = "localhost";
                Setting.List.DBServerPort = "3306";
                Setting.List.DBServerUser = "phoenix";
                Setting.List.DBServerPassword = "phoenix";
                Setting.List.SelectedDatabases = Databases.Classic;
                LoadData();
            }
            else { TGLClassicDB.Checked = true; }
        }

        private void TGLTbcDB_Click(object sender, EventArgs e)
        {
            if (TGLTbcDB.Checked == true)
            {
                TGLClassicDB.Checked = false;
                TGLWotlkDB.Checked = false;
                TGLCataDB.Checked = false;
                TGLMopDB.Checked = false;
                TGLCustomDB.Checked = false;
                Setting.List.AuthDatabase = "tbc_auth";
                Setting.List.CharactersDatabase = "tbc_characters";
                Setting.List.WorldDatabase = "tbc_world";
                Setting.List.DBServerHost = "localhost";
                Setting.List.DBServerPort = "3306";
                Setting.List.DBServerUser = "phoenix";
                Setting.List.DBServerPassword = "phoenix";
                Setting.List.SelectedDatabases = Databases.TBC;
                LoadData();

            }
            else { TGLTbcDB.Checked = true; }
        }

        private void TGLWotlkDB_Click(object sender, EventArgs e)
        {
            if (TGLWotlkDB.Checked == true)
            {
                TGLClassicDB.Checked = false;
                TGLTbcDB.Checked = false;
                TGLCataDB.Checked = false;
                TGLMopDB.Checked = false;
                TGLCustomDB.Checked = false;
                Setting.List.AuthDatabase = "wotlk_auth";
                Setting.List.CharactersDatabase = "wotlk_characters";
                Setting.List.WorldDatabase = "wotlk_world";
                Setting.List.DBServerHost = "localhost";
                Setting.List.DBServerPort = "3306";
                Setting.List.DBServerUser = "phoenix";
                Setting.List.DBServerPassword = "phoenix";
                Setting.List.SelectedDatabases = Databases.WotLK;
                LoadData();
            }
            else { TGLWotlkDB.Checked = true; }
        }

        private void TGLCataDB_Click(object sender, EventArgs e)
        {
            if (TGLCataDB.Checked == true)
            {
                TGLClassicDB.Checked = false;
                TGLTbcDB.Checked = false;
                TGLWotlkDB.Checked = false;
                TGLMopDB.Checked = false;
                TGLCustomDB.Checked = false;
                Setting.List.AuthDatabase = "cata_auth";
                Setting.List.CharactersDatabase = "cata_characters";
                Setting.List.WorldDatabase = "cata_world";
                Setting.List.DBServerHost = "localhost";
                Setting.List.DBServerPort = "3306";
                Setting.List.DBServerUser = "phoenix";
                Setting.List.DBServerPassword = "phoenix";
                Setting.List.SelectedDatabases = Databases.Cata;
                LoadData();
            }
            else { TGLCataDB.Checked = true; }
        }

        private void TGLMopDB_Click(object sender, EventArgs e)
        {
            if (TGLMopDB.Checked == true)
            {
                TGLClassicDB.Checked = false;
                TGLTbcDB.Checked = false;
                TGLWotlkDB.Checked = false;
                TGLCataDB.Checked = false;
                TGLCustomDB.Checked = false;
                Setting.List.AuthDatabase = "mop_auth";
                Setting.List.CharactersDatabase = "mop_characters";
                Setting.List.WorldDatabase = "mop_world";
                Setting.List.DBServerHost = "localhost";
                Setting.List.DBServerPort = "3306";
                Setting.List.DBServerUser = "phoenix";
                Setting.List.DBServerPassword = "phoenix";
                Setting.List.SelectedDatabases = Databases.MoP;
                LoadData();
            }
            else { TGLMopDB.Checked = true; }
        }

        private void TGLCustomDB_Click(object sender, EventArgs e)
        {
            if (TGLCustomDB.Checked == true)
            {
                TGLClassicDB.Checked = false;
                TGLTbcDB.Checked = false;
                TGLWotlkDB.Checked = false;
                TGLCataDB.Checked = false;
                TGLMopDB.Checked = false;
                ComboBoxCores_OnSelectedIndexChanged(sender, e);
                Setting.List.DBServerHost = "localhost";
                Setting.List.DBServerPort = "3306";
                Setting.List.DBServerUser = "root";
                Setting.List.DBServerPassword = "FlyingPhoenix";
                Setting.List.SelectedDatabases = Databases.Custom;
                LoadData();
            }
            else { TGLCustomDB.Checked = true; }
        }

        private void TGLCustomDB_CheckedChanged(object sender, EventArgs e)
        {
            if (TGLCustomDB.Checked == true)
            {
                TXTAuthDatabase.ReadOnly = false;
                TXTCharDatabase.ReadOnly = false;
                TXTWorldDatabase.ReadOnly = false;
                TXTMysqlHost.ReadOnly = false;
                TXTMysqlPort.ReadOnly = false;
                TXTMysqlUser.ReadOnly = false;
                TXTMysqlPassword.ReadOnly = false;
            }
            else
            {
                TXTAuthDatabase.ReadOnly = true;
                TXTCharDatabase.ReadOnly = true;
                TXTWorldDatabase.ReadOnly = true;
                TXTMysqlHost.ReadOnly = true;
                TXTMysqlPort.ReadOnly = true;
                TXTMysqlUser.ReadOnly = true;
                TXTMysqlPassword.ReadOnly = true;
            }
        }
    }
}
