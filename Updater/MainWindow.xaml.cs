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
using MarkieUtilities.Core.Log;
using MarkieUtilities.Core.Net;
using MarkieUtilities.Core.Config;
using MarkieUtilities.Core.Json;
using MarkieUtilities.Core.IO;
using System.Net;

namespace Updater {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private static readonly ILogSource _log = Log.GetLogSource( typeof( MainWindow ) );

        #region Config values
        public IniFile ConfigFile { get; private set; }
        public IniSection MainConfig { get; private set; }
        public LatestVersionResponse LatestVersionResponse { get; private set; }

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
                CurrentVersion = MainConfig[Constants.LatestVersionKey];
                _log.Info( "Config loaded." );

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
            OpenCurrentVersionDescription();
        }

        private void LatestVersionTextBox_PreviewMouseUp( object sender, MouseButtonEventArgs e ) {
            OpenLatestVersionDescription();
        }

        private void DiscordButton_Click(object sender, RoutedEventArgs e) {
            Process.Start(MainConfig[Constants.DiscordUriKey]);
        }
        #endregion

        //==============================================
        // Functionality
        //==============================================
        #region Functionality
        private async void StartCheck() {
            ToggleMainButton( false );
            try {
                string latestVersionUri = MainConfig[Constants.LatestVersionApiUriKey];

                _log.Info( "Retrieving latest version tag..." );
                using (HttpResponseMessage response = await _httpClient.GetAsync(latestVersionUri)) {
                    response.EnsureSuccessStatusCode();

                    string json = await response.Content.ReadAsStringAsync();
                    dynamic data = SimpleJson.DeserializeObject(json);
                    LatestVersionResponse = new LatestVersionResponse {
                        LatestVersion = data[Constants.ApiLatestVersionKey],
                        LatestVersionUri = data[Constants.ApiLatestVersionUriKey],
                        LatestVersionPatchUri = data[Constants.ApiLatestVersionPatchUriKey],
                        LatestVersionDescriptionUri = data[Constants.ApiLatestVersionDescriptionUriKey]
                    };
                    string latestVersion = LatestVersionResponse.LatestVersion;

                    _log.Info( "Latest version tag '{0}' retrieved.", latestVersion );
                    LatestVersion = latestVersion;
                    if ( string.Compare( CurrentVersion, latestVersion, true ) != 0 ) {
                        SetCurrentStage( OperationStage.Update );
                        CurrentVersionTextBox.Background = new SolidColorBrush(Constants.OutdatedVersionColor);
                    }
                    else {
                        _log.Info( "Congratulations, you are on the latest version." );
                        SetCurrentStage( OperationStage.Done );
                        CurrentVersionTextBox.Background = new SolidColorBrush(Constants.UpdatedVersionColor);
                    }
                }
            }
            catch ( Exception e ) {
                _log.Error( "Failed to retrieve latest version: " + e.Message );

                SetCurrentStage( OperationStage.Check );
            }
        }

        private async void StartUpdate() {
            if (LatestVersionResponse == null) {
                return;
            }

            ResetProgressBars();

            string currentDirectory = Directory.GetCurrentDirectory();
            string installDirectory = Path.GetDirectoryName( currentDirectory );
            string tempDirectory = string.Format("{0}\\{1}\\", currentDirectory, Constants.TempDirectoryName);
            string latestVersion = LatestVersion;
            try {
                _log.Info( "Preparing temp directory..." );
                if ( Directory.Exists( tempDirectory ) ) {
                    Directory.Delete( tempDirectory, true );
                }
                Directory.CreateDirectory( tempDirectory );

                _cancelTokenSource = new CancellationTokenSource();
                SetCurrentStage( OperationStage.Cancel );

                _log.Info("Sending download request...");
                string uri = CleanUpdateCheckBox.IsChecked.GetValueOrDefault() ? LatestVersionResponse.LatestVersionUri : LatestVersionResponse.LatestVersionPatchUri;
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
                CurrentVersionTextBox.Background = new SolidColorBrush(Constants.UpdatedVersionColor);
            }
            catch ( Exception e ) {
                bool handled = false;
                if ( e is TaskCanceledException ) {
                    _log.Info( "Update canceled." );
                    handled = true;
                }
                else if ( e is HttpRequestException ) {
                    if ( e.Message.Contains( "404" ) ) {
                        if ( CleanUpdateCheckBox.IsChecked.GetValueOrDefault() ) {
                            _log.Error( "Update file not found, please contact the developer!" );
                        }
                        else {
                            _log.Error( "Patch file not found, you should try again but with \"Clean update\" enabled." );
                        }
                        handled = true;
                    }
                }

                if ( !handled ) {
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
                MainConfig[Constants.LatestVersionKey] = latestVersion;
                ConfigFile.Save();
                _log.Info( "Config updated." );
            }
            catch ( Exception e ) {
                _log.Error( "Failed to update config even though update was sucessful: " + e.Message );
            }

            _log.Info( "All done, you are now on the latest version." );
        }

        public void OpenCurrentVersionDescription() {
            Process.Start(MainConfig[Constants.LatestVersionDescriptionUriKey]);
        }
        public void OpenLatestVersionDescription() {
            if (LatestVersionResponse != null) {
                Process.Start(LatestVersionResponse.LatestVersionDescriptionUri);
            }
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
        #endregion

        private void DiscordLabel_Click(object sender, RoutedEventArgs e) {

        }
    }
}
