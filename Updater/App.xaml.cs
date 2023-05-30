using System;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using MarkieUtilities.Core;
using MarkieUtilities.Core.Log;

namespace Updater {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public const string CurrentVersion = "0.2.0";

        public static LogLevel LogLevel { get; }

        public static string ExecutablePath { get; }
        public static string ExecutableName { get; }
        public static string[] AssemblyPaths { get; }

        // Files to delete after exiting.
        private static List<string> _deleteAfterExit = new List<string>();

        static App() {
#if DEBUG
            LogLevel = LogLevel.All;
#else
            LogLevel = LogLevel.Info;
#endif

            ExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
            ExecutableName = Path.GetFileNameWithoutExtension( ExecutablePath );
            // Manually defined because we're currently using only MarkieUtilities.Core
            AssemblyPaths = new string[] {
                CommonUtilities.GetAssemblyLocation()
            };

            Log.AddLogFile( LogLevel, ExecutableName + ".log" );
        }

        //==============================================
        // Self-updating functionality
        //==============================================
        #region Self-updating functionality
        /// <summary>
        /// Rename this application and loaded assemblies to something else for self-updating purposes.
        /// </summary>
        public static void ObsoleteSelf() {
            string oldExe = ExecutablePath + Constants.ObsoleteSuffix;
            if ( File.Exists( oldExe ) ) {
                File.Delete( oldExe );
            }
            File.Move( ExecutablePath, oldExe );
            
            foreach ( string assembly in AssemblyPaths ) {
                string oldAss = assembly + Constants.ObsoleteSuffix;
                if ( File.Exists( oldAss ) ) {
                    File.Delete( oldAss );
                }
                File.Move( assembly, oldAss );
            }
        }

        /// <summary>
        /// Mark application or assembly for delete IF there is replacement. Otherwise restore.
        /// </summary>
        public static void PruneSelf() {
            // We only mark for delete because we can't delete a running application or loaded assembly.

            string oldExe = ExecutablePath + Constants.ObsoleteSuffix;
            if ( File.Exists( oldExe ) ) {
                if ( File.Exists( ExecutablePath ) ) {
                    _deleteAfterExit.Add( oldExe );
                }
                else {
                    File.Move( oldExe, ExecutablePath );
                }
            }

            foreach ( string assembly in AssemblyPaths ) {
                string oldAss = assembly + Constants.ObsoleteSuffix;
                if ( File.Exists( oldAss ) ) {
                    if ( File.Exists( assembly ) ) {
                        _deleteAfterExit.Add( oldAss );
                    }
                    else {
                        File.Move( oldAss, assembly );
                    }
                }
            }
        }
        #endregion

        //==============================================
        // Callbacks
        //==============================================
        #region Callbacks
        private void Application_Exit( object sender, ExitEventArgs e ) {
            if ( _deleteAfterExit.Count > 0 ) {
                StringBuilder stringBuilder = new StringBuilder();
                foreach ( string file in _deleteAfterExit ) {
                    stringBuilder.Append( string.Format( " \"{0}\"", file ) );
                }

                // Queue a cmd process to delete the files 1 second after exiting.
                ProcessStartInfo Info = new ProcessStartInfo() {
                    Arguments = "/C choice /C Y /N /D Y /T 1 & del" + stringBuilder.ToString(),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd.exe"
                };
                Process.Start( Info );
            }
        }
        #endregion

        //==============================================
        // Misc
        //==============================================
        #region Misc
        public static void ExitWithMessage( string message, MessageBoxImage type, string title = null ) {
            MessageBox.Show( message, title.IsNullOrEmpty() ? type.ToString() : title, MessageBoxButton.OK, type );
            Current.Shutdown();
        }
        #endregion
    }
}
