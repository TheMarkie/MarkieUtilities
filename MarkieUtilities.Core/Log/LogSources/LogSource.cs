namespace MarkieUtilities.Core.Log {
    class LogSource : ILogSource {
        public string Category { get; }

        public LogSource( string category ) {
            Category = category;
        }

        //==============================================
        // Logging
        //==============================================
        #region Debug
        public void Debug( object value ) {
            Log.Print( LogLevel.Debug, Category, value );
        }
        public void Debug( string message ) {
            Log.Print( LogLevel.Debug, Category, message );
        }
        public void Debug( string format, params object[] args ) {
            Log.Print( LogLevel.Debug, Category, format, args );
        }
        #endregion

        #region Info
        public void Info( object value ) {
            Log.Print( LogLevel.Info, Category, value );
        }
        public void Info( string message ) {
            Log.Print( LogLevel.Info, Category, message );
        }
        public void Info( string format, params object[] args ) {
            Log.Print( LogLevel.Info, Category, format, args );
        }
        #endregion

        #region Warning
        public void Warning( object value ) {
            Log.Print( LogLevel.Warning, Category, value );
        }
        public void Warning( string message ) {
            Log.Print( LogLevel.Warning, Category, message );
        }
        public void Warning( string format, params object[] args ) {
            Log.Print( LogLevel.Warning, Category, format, args );
        }
        #endregion

        #region Error
        public void Error( object value ) {
            Log.Print( LogLevel.Error, Category, value );
        }
        public void Error( string message ) {
            Log.Print( LogLevel.Error, Category, message );
        }
        public void Error( string format, params object[] args ) {
            Log.Print( LogLevel.Error, Category, format, args );
        }
        #endregion

        #region Fatal
        public void Fatal( object value ) {
            Log.Print( LogLevel.Fatal, Category, value );
        }
        public void Fatal( string message ) {
            Log.Print( LogLevel.Fatal, Category, message );
        }
        public void Fatal( string format, params object[] args ) {
            Log.Print( LogLevel.Fatal, Category, format, args );
        }
        #endregion
    }
}
