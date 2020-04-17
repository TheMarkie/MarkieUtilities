namespace MarkieUtilities.Core.Log {
    class CallbackLogTarget : ILogTarget {
        public LogLevel LogLevel { get; }

        private LogCallback _logCallback;

        public CallbackLogTarget( LogLevel logLevel, LogCallback logCallback ) {
            LogLevel = logLevel;
            _logCallback = logCallback;
        }

        public void Print( LogMessage message ) {
            _logCallback( message );
        }
    }
}
