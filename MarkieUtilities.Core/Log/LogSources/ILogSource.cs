namespace MarkieUtilities.Core.Log {
    public interface ILogSource {
        string Category { get; }

        void Debug( object value );
        void Debug( string message );
        void Debug( string format, params object[] args );

        void Info( object value );
        void Info( string message );
        void Info( string format, params object[] args );

        void Warning( object value );
        void Warning( string message );
        void Warning( string format, params object[] args );

        void Error( object value );
        void Error( string message );
        void Error( string format, params object[] args );

        void Fatal( object value );
        void Fatal( string message );
        void Fatal( string format, params object[] args );
    }
}
