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
