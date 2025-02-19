using Bobocraft_2_Launcher_Update_Assistant;
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

namespace Bobocraft_2_Launcher_Update_Assistant
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate,
        awaitingInput
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
        private string modZip;
        private string launcherExe;
        private string launcherPath;
        private string configFile;
        private bool isNewInstall;
        private string chosenUserName;

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
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            launcherPath = Directory.GetCurrentDirectory();
            rootPath = launcherPath;
            launcherExe = Path.Combine(rootPath, "Bobocraft 2 Launcher.exe");
            versionFile = Path.Combine(rootPath, "version.txt");
            launcherVersionFile = Path.Combine(rootPath, "launcherversion.txt");
            tempZip = Path.Combine(rootPath, "temp");
            FAQFullText.Text = "Frequently Asked Questions:\r\n\r\n\tWhat is this? Is this the new Robocraft?\r\nThis game was originally under development by Freejam under the title ‘Robocraft 2’ until development was cancelled in early 2024. Freejam decided to change directions with their project, which is now under development as ‘Robocraft 2’ (often referred to as ‘The Robocraft 2 Rebuild’ by the community). When the original was cancelled, the community decided to preserve it and set up dedicated community servers so we could still play together. This launcher exists to help you play that original version of Robocraft 2, plus some community bug fixes and balance changes. If you are interested in the new version being currently developed by Freejam you can request access to the playtest on the Robocraft steam store page or visit Robocraft2.com for more information. \r\n\r\n\tHow do I use this thing?\r\nJust put it inside your main installation folder, “\\Robocraft 2” and run it, ask the discord if you are running into any problems and someone will help you. It will modify your vanilla Robocraft 2 install to a modded one and check for updates so you’ll have the latest community patch and will be able to connect to the community server. This launcher only works for windows users, check the discord for mac/linux information.\r\n\r\n\tHow do I install bots/precons/maps?\r\nThese are all stored inside your application data folder. To access it, follow these steps:\r\nPress the windows key, type ‘%appdata%’ and press enter\r\nNavigate to ‘\\AppData\\LocalLow\\Freejam\\Robocraft 2’\r\nBots are located in: Modded\\Machines\r\nMaps are located in: Mock\\Worlds\r\nPrecons are located in: Modded\\Precons\r\n\r\n\tCan I share this game on social media?\r\nYes, but you must make it clear that this is not an official Freejam project or endorsed by or affiliated with Freejam. This can be with a text disclaimer in the description, for example. Freejam has asked us to do this and we think it is quite reasonable and understandable, given that their new project is also called ‘Robocraft 2’ and they probably want to avoid confusion.\r\n\r\n\tCredits\r\nOriginal Game: Freejam\r\nMod/Server Build: NorbiPeti\r\nMain Server Host: shadowcrafter01\r\nBalance Changes: OXxzyDoOM\r\nDiscord Operator: Loading_._._.\r\nLauncher: Ace73Streaming\r\n";
        }

        private void CheckForLauncherUpdates()
        {
            if (File.Exists(launcherVersionFile))
            {
                Version localLauncherVersion = new Version(File.ReadAllText(launcherVersionFile));
                LauncherVersionText.Text = localLauncherVersion.ToString();
                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineLauncherVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=16YSzW2p-mWDyS4249HdsNivMHvPU6uOu"));

                    if (onlineLauncherVersion.IsDifferentThan(localLauncherVersion))
                    {
                        UpdateLauncher(onlineLauncherVersion);
                    }
                    else
                    {
                        ProcessStartInfo process = new ProcessStartInfo(@launcherExe);
                        Process.Start(process);
                        this.Close();
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
                UpdateLauncher(Version.zero);
            }
        }

        private void UpdateLauncher(Version _onlinelauncherVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                Status = LauncherStatus.downloadingUpdate;
                _onlinelauncherVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=16YSzW2p-mWDyS4249HdsNivMHvPU6uOu"));
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadLauncherCompletedCallback);
                webClient.DownloadFileAsync(new Uri("https://cloud.norbipeti.eu/s/YWYN2mQL4p7ptWn/download/asd.zip"), tempZip, _onlinelauncherVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error downloading launcher update: {ex}");
            }
        }

        private void DownloadLauncherCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineLauncherVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(tempZip, rootPath, true);
                ProcessStartInfo process = new ProcessStartInfo(@launcherExe);
                File.WriteAllText(launcherVersionFile, onlineLauncherVersion);
                File.Delete(tempZip);
                Process.Start(process);
                this.Close();
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error updating launcher: {ex}");
            }
        }


        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {

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


        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForLauncherUpdates();
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
