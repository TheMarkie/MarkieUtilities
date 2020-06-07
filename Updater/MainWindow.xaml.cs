using System;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows.Media;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;
using MarkieUtilities.Core;
using MarkieUtilities.Core.Log;
using MarkieUtilities.Core.Net;
using MarkieUtilities.Core.Config;
using MarkieUtilities.Core.Json;
using MarkieUtilities.Core.IO;

namespace Updater {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private const string TempDirectoryName = "Temp";

        // A shade of yellow.
        private static readonly Color OutdatedVersionColor = Color.FromRgb( 255, 255, 115 );
        // A shade of green.
        private static readonly Color UpdatedVersionColor = Color.FromRgb( 138, 255, 69 );

        private static readonly ILogSource _log = Log.GetLogSource( typeof( MainWindow ) );

        #region Config values
        public IniFile ConfigFile { get; private set; }
        public IniSection MainConfig { get; private set; }

        public string CurrentVersion {
            get { return CurrentVersionTextBox.Text; }
            private set { CurrentVersionTextBox.Text = value; }
        }
        public string LatestVersion {
            get { return LatestVersionTextBox.Text; }
            private set { LatestVersionTextBox.Text = value; }
        }
        #endregion

        public enum OperationStage {
            Check = 0,
            Update = 1,
            Cancel = 2,
            Done
        }
        public OperationStage CurrentStage { get; private set; }

        private GithubHelper _githubHelper;
        private AdvancedHttpClient _httpClient;
        private CancellationTokenSource _cancelTokenSource;

        // Cache the download size in MB to show on UI.
        private double _downloadSizeCache;

        //==============================================
        // Initialization
        //==============================================
        #region Initialization
        public MainWindow() {
            InitializeComponent();

            VersionLabel.Content = App.CurrentVersion;

            Log.AddLogCallback( App.LogLevel, OnLogCallback );

            Initialize();
        }

        private async void Initialize() {
            ToggleWindow( false );
            try {
                _log.Info( "Initializing..." );

                ConfigFile = await IniFile.LoadAsync( App.ExecutableName + ".ini" );
                MainConfig = ConfigFile[App.ExecutableName];
                CurrentVersion = MainConfig["CurrentVersion"];
                _log.Info( "Config loaded." );

                IniSection githubSection = ConfigFile["Github"];
                _githubHelper = new GithubHelper(
                    githubSection["UserName"],
                    githubSection["RepoName"],
                    githubSection["BaseUri"],
                    githubSection["BaseApiUri"]
                );
                _log.Info( "Github helper initialized." );

                _httpClient = new AdvancedHttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
                // Required to use GitHub API.
                _httpClient.DefaultRequestHeaders.UserAgent.Add( new ProductInfoHeaderValue( App.ExecutableName, App.CurrentVersion ) );
                _log.Info( "HTTP client initalized." );

                SetCurrentStage( OperationStage.Check );
            }
            catch ( Exception e ) {
                string errorMessage = string.Format( "Failed to initialize: {0}", e.Message );
                _log.Error( errorMessage );
                App.ExitWithMessage( errorMessage, MessageBoxImage.Error );
            }
            ToggleWindow( true );
        }
        #endregion

        //==============================================
        // Control behaviours
        //==============================================
        #region Control behaviours
        private void MainButton_Click( object sender, RoutedEventArgs e ) {
            switch ( CurrentStage ) {
                case OperationStage.Check:
                    StartCheck();
                    break;
                case OperationStage.Update:
                    StartUpdate();
                    break;
                case OperationStage.Cancel:
                    if ( _cancelTokenSource != null ) {
                        _cancelTokenSource.Cancel();
                    }
                    break;
            }
        }

        private void CurrentVersionTextBox_PreviewMouseUp( object sender, MouseButtonEventArgs e ) {
            OpenChangelog( CurrentVersion );
        }

        private void LatestVersionTextBox_PreviewMouseUp( object sender, MouseButtonEventArgs e ) {
            OpenChangelog( LatestVersion );
        }
        #endregion

        //==============================================
        // Functionality
        //==============================================
        #region Functionality
        private async void StartCheck() {
            ToggleMainButton( false );
            try {
                string uri = _githubHelper.GetLatestReleaseApiUri();

                _log.Info( "Retrieving latest version tag..." );
                using ( HttpResponseMessage response = await _httpClient.GetAsync( uri ) ) {
                    response.EnsureSuccessStatusCode();

                    string json = await response.Content.ReadAsStringAsync();
                    dynamic payload = SimpleJson.DeserializeObject( json );
                    string latestVersion = payload["tag_name"];
                    LatestVersion = latestVersion;

                    _log.Info( "Latest version tag '{0}' retrieved.", latestVersion );
                    if ( string.Compare( CurrentVersion, latestVersion, true ) != 0 ) {
                        SetCurrentStage( OperationStage.Update );
                        CurrentVersionTextBox.Background = new SolidColorBrush( OutdatedVersionColor );
                    }
                    else {
                        _log.Info( "Congratulations, you are on the latest version." );
                        SetCurrentStage( OperationStage.Done );
                        CurrentVersionTextBox.Background = new SolidColorBrush( UpdatedVersionColor );
                    }
                }
            }
            catch ( Exception e ) {
                _log.Error( "Failed to retrieve latest version: " + e.Message );

                SetCurrentStage( OperationStage.Check );
            }
        }

        private async void StartUpdate() {
            ResetProgressBars();

            string currentDirectory = Directory.GetCurrentDirectory();
            string installDirectory = Path.GetDirectoryName( currentDirectory );
            string tempDirectory = string.Format( "{0}\\{1}\\", currentDirectory, TempDirectoryName );
            string latestVersion = LatestVersion;
            try {
                _log.Info( "Preparing temp directory..." );
                if ( Directory.Exists( tempDirectory ) ) {
                    Directory.Delete( tempDirectory, true );
                }
                Directory.CreateDirectory( tempDirectory );

                _log.Info( "Sending download request..." );
                string fileName = GetFullFileName(
                    MainConfig["FileName"],
                    latestVersion,
                    CleanUpdateCheckBox.IsChecked.GetValueOrDefault()
                );
                string uri = _githubHelper.GetDownloadUri( latestVersion, fileName );

                _cancelTokenSource = new CancellationTokenSource();
                SetCurrentStage( OperationStage.Cancel );

                string file = await _httpClient.DownloadFileAsync(
                    uri,
                    tempDirectory,
                    _cancelTokenSource.Token,
                    OnDownloadProgressCallback
                );

                ToggleMainButton( false );

                _log.Info( "Preparing files for installing..." );
                App.ObsoleteSelf();

                _log.Info( "Installing update files..." );
                await FileUtilities.ExtractToDirectoryAsync( file, installDirectory, OnInstallProgressCallback, true );

                App.PruneSelf();

                _log.Info( "Update installed successfully." );
                SetCurrentStage( OperationStage.Done );
                CurrentVersion = latestVersion;
                CurrentVersionTextBox.Background = new SolidColorBrush( UpdatedVersionColor );
            }
            catch ( Exception e ) {
                if ( e is TaskCanceledException ) {
                    _log.Info( "Update canceled." );
                }
                else {
                    _log.Error( "Failed to update: " + e.Message );
                }

                ResetProgressBars();
                SetCurrentStage( OperationStage.Update );
            }
            finally {
                _log.Info( "Cleaning up temp files..." );
                if ( Directory.Exists( tempDirectory ) ) {
                    Directory.Delete( tempDirectory, true );
                }
            }

            if ( CurrentStage == OperationStage.Update ) {
                return;
            }

            try {
                _log.Info( "Updating config..." );
                // Reload the ini file in case there was an update to the ini itself.
                ConfigFile = await IniFile.LoadAsync( App.ExecutableName + ".ini" );
                MainConfig = ConfigFile[App.ExecutableName];
                string currentVersion = MainConfig["CurrentVersion"];
                if ( string.Compare( currentVersion, latestVersion, true ) != 0 ) {
                    MainConfig["CurrentVersion"] = latestVersion;

                    ConfigFile.Save();
                }
                _log.Info( "Config updated." );
            }
            catch ( Exception e ) {
                _log.Error( "Failed to update config even though update was sucessful: " + e.Message );
            }

            _log.Info( "All done, you are now on the latest version." );
        }

        public void OpenChangelog( string version ) {
            Process.Start( _githubHelper.GetReleaseUri( version ) );
        }
        #endregion

        //==============================================
        // Callbacks
        //==============================================
        #region Callbacks
        private void OnLogCallback( LogMessage message ) {
            LogTextBox.AppendText( message.Message + Environment.NewLine );
            LogTextBox.LineDown();
        }

        private void OnDownloadProgressCallback( int count, int total ) {
            if ( total <= 0 ) {
                DownloadProgressBar.IsIndeterminate = true;

                return;
            }
            else if ( DownloadProgressBar.IsIndeterminate ) {
                DownloadProgressBar.IsIndeterminate = false;
            }

            double progress = ( double ) count / total;
            DownloadProgressBar.Value = progress;

            if ( _downloadSizeCache <= 0 ) {
                _downloadSizeCache = total / 1000000d;
            }
            DownloadProgressTextBlock.Text = string.Format( "{0:0.00}/{1:0.00} MB ({2:###}%)", count / 1000000d, _downloadSizeCache, progress * 100 );
        }

        private void OnInstallProgressCallback( int count, int total ) {
            double progress = ( double ) count / total;
            InstallProgressBar.Value = progress;
            InstallProgressTextBlock.Text = string.Format( "{0}/{1} files ({2:###}%)", count, total, progress * 100 );
        }
        #endregion

        //==============================================
        // Control management
        //==============================================
        #region Control management
        private void ToggleWindow( bool enable ) {
            if ( enable != IsEnabled ) {
                IsEnabled = enable;
            }
        }

        private void ToggleMainButton( bool enable ) {
            if ( enable != MainButton.IsEnabled ) {
                MainButton.IsEnabled = enable;
            }
        }

        private void ResetProgressBars() {
            DownloadProgressBar.IsIndeterminate = false;
            DownloadProgressBar.Value = 0;
            DownloadProgressBar.Maximum = 1;
            DownloadProgressTextBlock.Text = string.Empty;
            _downloadSizeCache = 0;
            InstallProgressBar.IsIndeterminate = false;
            InstallProgressBar.Value = 0;
            InstallProgressBar.Maximum = 1;
            InstallProgressTextBlock.Text = string.Empty;
        }
        #endregion

        //==============================================
        // Misc
        //==============================================
        #region Misc
        private void SetCurrentStage( OperationStage currentStage ) {
            if ( currentStage != CurrentStage ) {
                CurrentStage = currentStage;
                MainButton.Content = currentStage.ToString();
            }

            ToggleMainButton( CurrentStage != OperationStage.Done );
            CleanUpdateCheckBox.IsEnabled = CurrentStage == OperationStage.Update;
        }

        private string GetFullFileName( string fileName, string version, bool cleanInstall ) {
            return string.Format( "{0}{1}{2}.zip",
                fileName,
                version,
                cleanInstall ? string.Empty : "patch"
            );
        }
        #endregion
    }
}
