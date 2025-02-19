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

namespace GameLauncher
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
        private string gameExe;
        private string launcherAssistantExe;
        private string launcherPath;
        private string configFile;
        private bool isNewInstall;
        private string chosenUserName;
        private string assistantPath;
        private string botDirectory;

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
            modZip = Path.Combine(rootPath, "BepInEx", "plugins");
            configFile = Path.Combine(rootPath, "BepInEx", "config", "RC2MPWE.cfg");
            botDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Bobocraft 2 Community Starter Bots");
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
                    _onlinelauncherVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=16YSzW2p-mWDyS4249HdsNivMHvPU6uOu"));
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadLauncherAssistantCompletedCallback);
                    webClient.DownloadFileAsync(new Uri("https://cloud.norbipeti.eu/s/ZwRmsKb3gLNKKNH/download/assist.zip"), tempZip, _onlinelauncherVersion);
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
                VersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("https://cloud.norbipeti.eu/s/j6TFGJbbS5z9Dp4/download/version.txt"));

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
                    webClient.DownloadFileAsync(new Uri("https://cloud.norbipeti.eu/s/yyk3LBaZsXa4GpR/download/RC2MPWE.zip"), tempZip, _onlineVersion);
                }
                else
                {
                    isNewInstall = true;
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString("https://cloud.norbipeti.eu/s/j6TFGJbbS5z9Dp4/download/version.txt"));
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadModCompletedCallback);
                    webClient.DownloadFileAsync(new Uri("https://cloud.norbipeti.eu/s/kZSSjFc2jqa22Hw/download/sus.zip"), tempZip, _onlineVersion);
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
                webClient.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&id=1DBX1tnU2rw7zVcgXFHAydG4wsbK2O-go"), tempZip, _zero);
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
                MessageBox.Show("A collection of Community Starter Bots was placed your desktop, please check the readme file for instructions on how to use them, and welcome to Bobocraft 2!");
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
                    VersionText.Text = "0.0.1";
                    Status = LauncherStatus.awaitingInput;
                    mainWindowBox.Visibility = Visibility.Visible;
                }
                else
                {
                    ZipFile.ExtractToDirectory(tempZip, modZip, true);
                    File.Delete(tempZip);
                    File.WriteAllText(versionFile, onlineVersion);
                    VersionText.Text = onlineVersion;
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
