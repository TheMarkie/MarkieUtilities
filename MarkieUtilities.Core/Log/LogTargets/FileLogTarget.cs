using System;
using System.IO;
using System.Threading;

namespace MarkieUtilities.Core.Log {
    class FileLogTarget : ILogTarget {
        const int MessageCountFlushThreshold = 20;
        const int MessageFlushInterval = 1000;

        public LogLevel LogLevel { get; }

        private StreamWriter _logStream;

        private int _messageCount;
        private Timer _timer;

        public static FileLogTarget NewLogFile( LogLevel level, string path ) {
            try {
                StreamWriter stream = new StreamWriter( path );

                return new FileLogTarget( level, stream );
            }
#if DEBUG
            catch ( Exception e) {
                throw e;
#else
            catch {
                return null;
#endif
            }
        }
        private FileLogTarget( LogLevel logLevel, StreamWriter logStream ) {
            LogLevel = logLevel;
            _logStream = logStream;
            _messageCount = 0;
            _timer = new Timer( OnTimerCallback, null, MessageFlushInterval, MessageFlushInterval );
        }

        public void Print( LogMessage message ) {
            _logStream.WriteLine( message.ToString() );

            _messageCount += 1;
            if ( _messageCount >= MessageCountFlushThreshold ) {
                _logStream.Flush();

                _messageCount = 0;
            }
        }

        private void OnTimerCallback( object _ ) {
            _logStream.Flush();
        }
    }
}
