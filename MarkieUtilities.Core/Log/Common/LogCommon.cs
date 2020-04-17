namespace MarkieUtilities.Core.Log {
    public enum LogLevel {
        None = 0,
        Fatal = 1,
        Error = 2,
        Warning = 3,
        Info = 4,
        Debug = 5,

        All
    }

    public delegate void LogCallback( LogMessage message );
}
