using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MarkieUtilities.Core.Log {
    public static class Log {
        private static List<ILogTarget> _targets;
        
        static Log() {
#if DEBUG
            _targets = new List<ILogTarget> {
                new DebugLogTarget( LogLevel.All )
            };
#else
            _targets = new List<ILogTarget>();
#endif
        }

        //==============================================
        // Logging
        //==============================================
        #region Logging
        internal static void Print( LogLevel level, string category, object value ) {
            if ( level == LogLevel.None ) {
                return;
            }

            LogMessage logMessage = new LogMessage(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                level,
                category,
                value
            );

            foreach ( ILogTarget target in _targets ) {
                if ( level > target.LogLevel ) {
                    continue;
                }

                target.Print( logMessage );
            }
        }

        internal static void Print( LogLevel level, string category, string message ) {
            if ( level == LogLevel.None ) {
                return;
            }

            LogMessage logMessage = new LogMessage(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                level,
                category,
                message
            );

            foreach ( ILogTarget target in _targets ) {
                if ( level > target.LogLevel ) {
                    continue;
                }

                target.Print( logMessage );
            }
        }

        internal static void Print( LogLevel level, string category, string format, params object[] args ) {
            if ( level == LogLevel.None ) {
                return;
            }

            LogMessage logMessage = new LogMessage(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                level,
                category,
                string.Format( format, args )
            );

            foreach ( ILogTarget target in _targets ) {
                if ( level > target.LogLevel ) {
                    continue;
                }

                target.Print( logMessage );
            }
        }
        #endregion

        //==============================================
        // Initializing
        //==============================================
        #region Initializing
        public static ILogSource GetLogSource( Type type ) {
            return new LogSource( string.Format( "{0}::{1}", type.Namespace, type.Name ) );
        }
        public static ILogSource GetLogSource( string category ) {
            if ( category.IsNullOrEmpty() ) {
                category = "Undefined";
            }

            return new LogSource( category );
        }

        public static void AddLogFile( LogLevel level, string path ) {
            if ( level == LogLevel.None ) {
                return;
            }

            FileLogTarget target = FileLogTarget.NewLogFile( level, path );
            if ( target != null ) {
                _targets.Add( target );
            }
        }

        public static void AddLogCallback( LogLevel level, LogCallback callback ) {
            if ( callback != null && level != LogLevel.None ) {
                _targets.Add( new CallbackLogTarget( level, callback ) );
            }
        }
        #endregion
    }
}
