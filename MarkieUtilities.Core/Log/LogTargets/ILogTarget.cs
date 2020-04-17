namespace MarkieUtilities.Core.Log {
    interface ILogTarget {
        LogLevel LogLevel { get; }

        void Print( LogMessage message );
    }
}
