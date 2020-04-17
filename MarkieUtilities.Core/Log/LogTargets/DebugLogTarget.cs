using System.Diagnostics;

namespace MarkieUtilities.Core.Log {
    class DebugLogTarget : ILogTarget {
        public LogLevel LogLevel { get; }

        public DebugLogTarget( LogLevel logLevel ) {
            LogLevel = logLevel;
        }

        public void Print( LogMessage message ) {
            Debug.WriteLine( message.ToStringWith( timestamp: false ) );
        }
    }
}
