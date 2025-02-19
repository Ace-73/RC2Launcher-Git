using GameLauncher;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Timers;

namespace GameLauncher
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate,
        awaitingInput,
        downloadingCRF2Manager,
        downloadingCSC
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string rootPath;
        private string versionFile;
        private string launcherVersionFile;
        private string tempZip;
        private string pluginPath;
        private string gameExe;
        private string launcherAssistantExe;
        private string launcherPath;
        private string configFile;
        private bool isNewInstall;
        private string chosenUserName;
        private string assistantPath;
        private string botDirectory;
        private string CRF2ManagerExe;
        private string CRF2ManagerVersionFile;
        private string CSCExe;
        private Timer RC2SessionTimer;
        private DateTime NextRC2SessionDateTime;
        private string launcherVersionFileLink;
        private string launcherAssistantZipLink;
        private string modVersionFileLink;
        private string modInitInstallZipLink;
        private string modUpdateZipLink;
        private string CRF2ManagerVersionFileLink;
        private string CRF2ManagerZipLink;
        private string CSCZipLink;
        private string StarterBotsZipLink;
        private string NextSessionString;
        private string NextSessionFile;
        private string NextSessionFileLink;
        

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play Bobocraft 2";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Install/Update Failed - Retry";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Installing Mod";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Updating";
                        break;
                    case LauncherStatus.awaitingInput:
                        PlayButton.Content = "Done";
                        break;
                    case LauncherStatus.downloadingCRF2Manager:
                        PlayButton.Content = "Downloading CRF2 Manager";
                        break;
                    case LauncherStatus.downloadingCSC:
                        PlayButton.Content = "Downloading CSC";
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            launcherPath = Directory.GetCurrentDirectory();
            assistantPath = launcherPath;
            gameExe = Path.Combine(launcherPath, "Robocraft 2.exe");
            if (File.Exists(gameExe))
            {
                rootPath = launcherPath;
            }
            else 
            { 
                rootPath = Path.Combine(launcherPath, "Game");
                gameExe = Path.Combine(rootPath, "Robocraft 2.exe");
                if (!File.Exists(gameExe))
                    {
                    MessageBox.Show("Unable to find Robocraft 2 installation. Please make sure this file is placed inside the main /Robocraft 2 folder.");
                    }
            }
            launcherAssistantExe = Path.Combine(assistantPath, "Bobocraft 2 Launcher Update Assistant.exe");
            versionFile = Path.Combine(rootPath, "version.txt");
            launcherVersionFile = Path.Combine(assistantPath, "launcherversion.txt");
            tempZip = Path.Combine(rootPath, "temp");
            pluginPath = Path.Combine(rootPath, "BepInEx", "plugins");
            configFile = Path.Combine(rootPath, "BepInEx", "config", "RC2MPWE.cfg");
            CRF2ManagerExe = Path.Combine(launcherPath, "BOBOBloodhound.exe");
            CSCExe = Path.Combine(launcherPath, "Connection Health Calculator.exe");
            CRF2ManagerVersionFile = Path.Combine(launcherPath, "crfmanagerversion.txt");
            NextSessionFile = Path.Combine(launcherPath,"nextsessiondatetime.txt");
            botDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "Freejam", "Robocraft 2", "Modded", "Machines");
            FAQFullText.Text = "Frequently Asked Questions:\r\n\tWhat is this? Is this the new Robocraft?\r\nThis game was originally under development by Freejam under the title ‘Robocraft 2’ until development was cancelled in early 2024. Freejam decided to change directions with their project, which is now under development as ‘Robocraft 2’ (often referred to as ‘The Robocraft 2 Rebuild’ by the community). When the original was cancelled, the community decided to preserve it and set up dedicated community servers so we could still play together. This launcher exists to help you play that original version of Robocraft 2, plus some community bug fixes and balance changes. If you are interested in the new version being currently developed by Freejam you can request access to the playtest on the Robocraft steam store page or visit Robocraft2.com for more information. \r\n\r\n\tHow do I use this thing?\r\nJust put it inside your main installation folder, “\\Robocraft 2” and run it, ask the discord if you are running into any problems and someone will help you. It will modify your vanilla Robocraft 2 install to a modded one and check for updates so you’ll have the latest community patch and will be able to connect to the community server. This launcher only works for windows users, check the discord for mac/linux information.\r\n\r\n\tHow do I install bots/precons/maps?\r\nThese are all stored inside your application data folder. To access it, follow these steps:\r\nPress the windows key, type ‘appdata’ and press enter\r\nNavigate to ‘\\AppData\\LocalLow\\Freejam\\Robocraft 2’\r\nBots are located in: Modded\\Machines\r\nMaps are located in: Mock\\Worlds\r\nPrecons are located in: Modded\\Precons\r\n\r\n\tCan I share this game on social media?\r\nYes, but you must make it clear that this is not an official Freejam project or endorsed by or affiliated with Freejam. This can be with a text disclaimer in the description, for example. Freejam has asked us to do this and we think it is quite reasonable and understandable, given that their new project is also called ‘Robocraft 2’ and they probably want to avoid confusion.\r\n\r\n\tCredits\r\nOriginal Game: Freejam\r\nMod/Server Build: NorbiPeti\r\nMain Server Host: shadowcrafter01\r\nBalance Changes: OXxzyDoOM\r\nDiscord Operator: Loading_._._.\r\nCRF2 Manager: Robocrafter Art (ARTGUK)\r\nLauncher: Ace73Streaming";
            launcherVersionFileLink = "https://drive.google.com/uc?export=download&id=1MnPRLYIwUUQ_QBPMol8TQmQkaISoTldD";
            launcherAssistantZipLink = "https://cloud.norbipeti.eu/s/ZwRmsKb3gLNKKNH/download/assist.zip";
            modVersionFileLink = "https://cloud.norbipeti.eu/s/j6TFGJbbS5z9Dp4/download/version.txt";
            modInitInstallZipLink = "https://cloud.norbipeti.eu/s/kZSSjFc2jqa22Hw/download/sus.zip";
            modUpdateZipLink = "https://cloud.norbipeti.eu/s/yyk3LBaZsXa4GpR/download/RC2MPWE.zip";
            StarterBotsZipLink = "https://drive.google.com/uc?export=download&id=1DBX1tnU2rw7zVcgXFHAydG4wsbK2O-go";
            CRF2ManagerVersionFileLink = "https://drive.google.com/uc?export=download&id=1SS5O7LRtFwPi6XbBB4It-tHhIzJw-_qN";
            CRF2ManagerZipLink = "https://drive.usercontent.google.com/u/0/uc?id=1ah4QN3HOj2nsCsHKxRkyTI_eKJIK3atb&export=download";
            CSCZipLink = "https://cloud.norbipeti.eu/s/6dTzZyAbyXRwHc9/download/Connection%20Health%20Calculator.zip";
            NextSessionFileLink = "https://drive.google.com/uc?export=download&id=1lMctvUExhyjw8FRrpiCKmfVnqNQMI7U7";
            SetupTimer();
        }

       private void SetupTimer()
        {
            PullSessionTimeFromLink();
            RC2SessionTimer = new Timer(1000);
            RC2SessionTimer.Elapsed += TimerElapsed;
            RC2SessionTimer.Start();
        }


        
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var timeLeft = NextRC2SessionDateTime - DateTime.Now;
                if (timeLeft.TotalSeconds <= 0)
                {
                    PullSessionTimeFromLink();
                    timeLeft = NextRC2SessionDateTime - DateTime.Now;
                }
                CountdownLabel.Content = $"{timeLeft.Days} days {timeLeft.Hours} hours {timeLeft.Minutes} minutes {timeLeft.Seconds} seconds";
            });
        }
        private void PullSessionTimeFromLink()
        {
            var currentUTCtime = DateTime.UtcNow;
            var Localtimezone = TimeZoneInfo.Local;
            var UTCtimezone = TimeZoneInfo.Utc;
            WebClient webClient = new WebClient();
            NextSessionString = webClient.DownloadString(NextSessionFileLink);
            NextRC2SessionDateTime = DateTime.Parse(NextSessionString);
            if (NextRC2SessionDateTime > DateTime.UtcNow)
            {
                NextRC2SessionDateTime = TimeZoneInfo.ConvertTime(NextRC2SessionDateTime, Localtimezone);
            }
            else
            {
                CalculateDefaultSessionTime();
            }
        }


        private void CalculateDefaultSessionTime()
        {
            var currentUTCtime = DateTime.UtcNow;
            var daysUntilFriday = ((int)DayOfWeek.Friday - (int)currentUTCtime.DayOfWeek + 7) % 7;
            var nextFriday = currentUTCtime.AddDays(daysUntilFriday);
            var PSTtimezone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            var Localtimezone = TimeZoneInfo.Local;
            NextRC2SessionDateTime = new DateTime(nextFriday.Year, nextFriday.Month, nextFriday.Day, 9, 0, 0, DateTimeKind.Unspecified);
            NextRC2SessionDateTime = TimeZoneInfo.ConvertTime(NextRC2SessionDateTime, PSTtimezone);
            NextRC2SessionDateTime = TimeZoneInfo.ConvertTime(NextRC2SessionDateTime, Localtimezone);
        }


        private void OpenCRF2Manager()
        {
            if (File.Exists(CRF2ManagerExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo CRF2ManagerProcess = new ProcessStartInfo(CRF2ManagerExe);
                CRF2ManagerProcess.WorkingDirectory = rootPath;
                Process.Start(CRF2ManagerProcess);
            }
            else if (Status != LauncherStatus.ready)
            {
                MessageBox.Show($"Please Update The Launcher Before Running The CRF2 Manager");
            }
            else if (!File.Exists(CRF2ManagerExe))
            {
                UpdateCRF2Manager(Version.zero);
            }
        }

        private void OpenCSC()
        {
            if (File.Exists(CSCExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo CSCCalcProcess = new ProcessStartInfo(CSCExe);
                CSCCalcProcess.WorkingDirectory = rootPath;
                Process.Start(CSCCalcProcess);
            }
            else if (Status != LauncherStatus.ready)
            {
                MessageBox.Show($"Please Update The Launcher Before Running The CRF2 Manager");
            }
            else if (!File.Exists(CSCExe))
            {
                DownloadCSC(Version.zero);
            }
        }


        private void DownloadCSC(Version _onlineCSCCalcVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                Status = LauncherStatus.downloadingCSC;
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadCSCCompletedCallback);
                webClient.DownloadFileAsync(new Uri(CSCZipLink), tempZip, Version.zero);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error downloading CSC Calculator: {ex}");
            }
        }

        private void DownloadCSCCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                ZipFile.ExtractToDirectory(tempZip, assistantPath, true);
                ProcessStartInfo CSCProcess = new ProcessStartInfo(CSCExe);
                CSCProcess.WorkingDirectory = rootPath;
                Process.Start(CSCProcess);
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing CSC Calculator: {ex}");
            }
        }





        private void UpdateCRF2Manager(Version _onlineCRF2ManagerVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                Status = LauncherStatus.downloadingCRF2Manager;
                _onlineCRF2ManagerVersion = new Version(webClient.DownloadString(CRF2ManagerVersionFileLink));
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadCRF2ManagerCompletedCallback);
                webClient.DownloadFileAsync(new Uri(CRF2ManagerZipLink), tempZip, _onlineCRF2ManagerVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error downloading CRF2 Manager: {ex}");
            }
        }

        private void DownloadCRF2ManagerCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineCRF2ManagerVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(tempZip, assistantPath, true);
                File.WriteAllText(CRF2ManagerVersionFile, onlineCRF2ManagerVersion);
                ProcessStartInfo CRF2ManagerProcess = new ProcessStartInfo(CRF2ManagerExe);
                CRF2ManagerProcess.WorkingDirectory = rootPath;
                Process.Start(CRF2ManagerProcess);
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing CRF2 Manager: {ex}");
            }
        }



        private void CheckForCRFManagerUpdates()
        {
            if (File.Exists(CRF2ManagerVersionFile))
            {
                Version localCRF2ManagerVersion = new Version(File.ReadAllText(CRF2ManagerVersionFile));
                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineCRF2ManagerVersion = new Version(webClient.DownloadString(CRF2ManagerVersionFileLink));

                    if (onlineCRF2ManagerVersion.IsDifferentThan(localCRF2ManagerVersion))
                    {
                        UpdateCRF2Manager(onlineCRF2ManagerVersion);
                    }
                    else
                    {
                        OpenCRF2Manager();
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for CRF2Manager updates: {ex}");
                }
            }
            else
            {
                UpdateCRF2Manager(Version.zero);
            }
        }



        private void CheckForLauncherUpdates()
        {
            if (File.Exists(launcherVersionFile))
            {
                Version localLauncherVersion = new Version(File.ReadAllText(launcherVersionFile));
                LauncherVersionText.Text = "Launcher Version: " + localLauncherVersion.ToString();
                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineLauncherVersion = new Version(webClient.DownloadString(launcherVersionFileLink));

                    if (onlineLauncherVersion.IsDifferentThan(localLauncherVersion))
                    {
                        UpdateLauncherAssistant(onlineLauncherVersion);
                    }
                    else
                    {
                        if (File.Exists(launcherAssistantExe)) 
                        { 
                            File.Delete(launcherAssistantExe); 
                        }
                        CheckForUpdates();
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for launcher updates: {ex}");
                }
            }
            else
            {
                UpdateLauncherAssistant(Version.zero);
            }
        }

        private void UpdateLauncherAssistant(Version _onlinelauncherVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                    Status = LauncherStatus.downloadingUpdate;
                    _onlinelauncherVersion = new Version(webClient.DownloadString(launcherVersionFileLink));
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadLauncherAssistantCompletedCallback);
                    webClient.DownloadFileAsync(new Uri(launcherAssistantZipLink), tempZip, _onlinelauncherVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error downloading launcher update: {ex}");
            }
        }

        private void DownloadLauncherAssistantCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineLauncherVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(tempZip, assistantPath, true);
                ProcessStartInfo process = new ProcessStartInfo(@launcherAssistantExe);
                File.Delete(tempZip);
                Process.Start(process);
                this.Close();
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error updating launcher assistant: {ex}");
            }
        }



        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = "Mod Version: " + localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString(modVersionFileLink));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallModFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for mod updates: {ex}");
                }
            }
            else
            {
                InstallModFiles(false, Version.zero);
            }
        }

        private void InstallModFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    isNewInstall = false;
                    Status = LauncherStatus.downloadingUpdate;
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadModCompletedCallback);
                    webClient.DownloadFileAsync(new Uri(modUpdateZipLink), tempZip, _onlineVersion);
                }
                else
                {
                    isNewInstall = true;
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString(modVersionFileLink));
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadModCompletedCallback);
                    webClient.DownloadFileAsync(new Uri(modInitInstallZipLink), tempZip, _onlineVersion);
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error downloading mod files: {ex}");
            }
        }

        private void InstallStarterBots(Version _zero)
        {
            try
            {
                WebClient webClient = new WebClient();
                Status = LauncherStatus.downloadingUpdate;
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadStarterBotsCompletedCallback);
                webClient.DownloadFileAsync(new Uri(StarterBotsZipLink), tempZip, _zero);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error downloading community starter bots: {ex}");
            }
        }

        private void DownloadStarterBotsCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                ZipFile.ExtractToDirectory(tempZip, botDirectory, true);
                File.Delete(tempZip);
                MessageBox.Show("Community Starter Bots installed to Robocraft 2 bot directory: \r\n\r\n" + botDirectory + "\r\n\r\nWelcome to Bobocraft 2!");
                Status = LauncherStatus.downloadingUpdate;
                CheckForUpdates();
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error downloading community starter bots: {ex}");
            }
        }

        private void DownloadModCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                if (isNewInstall)
                {
                    ZipFile.ExtractToDirectory(tempZip, rootPath, true);
                    File.Delete(tempZip);
                    File.WriteAllText(versionFile, "0.0.1");
                    VersionText.Text = "Mod Version: 0.0.1";
                    Status = LauncherStatus.awaitingInput;
                    mainWindowBox.Visibility = Visibility.Visible;
                }
                else
                {
                    ZipFile.ExtractToDirectory(tempZip, rootPath, true);
                    File.Delete(tempZip);
                    File.WriteAllText(versionFile, onlineVersion);
                    VersionText.Text = "Mod Version: " + onlineVersion;
                    Status = LauncherStatus.ready;
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing/updating mod: {ex}");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForLauncherUpdates();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = rootPath;
                Process.Start(startInfo);
                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
            else if (Status == LauncherStatus.awaitingInput)
            {
                CheckInputUsername();
            }
        }
        private void CRF2ManagerButton_Click(object sender, RoutedEventArgs e)
        {
            CheckForCRFManagerUpdates();
        }

        private void CSCButton_Click(object sender, RoutedEventArgs e)
        {
            OpenCSC();
        }

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "https://discord.gg/3jRESN4Dv3",
                UseShellExecute = true
            });
        }

        private void FAQButton_Click(object sender, RoutedEventArgs e)
        {
            if (FAQFullText.Visibility == Visibility.Visible) { FAQFullText.Visibility = Visibility.Hidden; }
            else { FAQFullText.Visibility = Visibility.Visible; }
        }

        private void CheckInputUsername()
        {
            chosenUserName = mainWindowBox.Text;
            if (chosenUserName.Length > 0 && chosenUserName.Length < 20 && chosenUserName != "Enter Username")
            {
                WriteUserName(chosenUserName);
            }
            else 
            {
                MessageBox.Show("Invalid Username");
            }
        }

        private void WriteUserName(string UserName)
        {
            if (File.Exists(configFile))
            {
                string configString = File.ReadAllText(configFile);
                configString = configString.Replace("UserName = Mod_Player", "UserName = " + chosenUserName);
                File.WriteAllText(configFile, configString);
                mainWindowBox.Visibility = Visibility.Hidden;
                InstallStarterBots(Version.zero);
            }
            else {
                try
                {
                    Directory.CreateDirectory(Path.Combine(rootPath, "BepInEx", "config"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating config directory: {ex}");
                }
                try
                {
                    File.WriteAllText(configFile, "[Auth]\r\nUserName = " + UserName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error writing config file: {ex}");
                }
                mainWindowBox.Visibility = Visibility.Hidden;
                InstallStarterBots(Version.zero);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }







    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }
        internal Version(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
