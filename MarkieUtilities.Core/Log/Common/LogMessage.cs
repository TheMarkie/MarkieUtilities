using System;
using System.Text;

namespace MarkieUtilities.Core.Log {
    public class LogMessage {
        const string TimestampFormat = "[{0:HH:mm:ss.fff}]";
        const string InfoFormat = "[{0}]";

        /// <summary>
        /// Unix Timestamp in milliseconds.
        /// </summary>
        public long Timestamp { get; }
        public LogLevel LogLevel { get; }
        public string Category { get; }
        public string Message { get; }

        public LogMessage( long timestamp, LogLevel logLevel, string category, object value ) {
            Timestamp = timestamp;
            LogLevel = logLevel;
            Category = category;
            Message = value.ToString();
        }

        public LogMessage( long timestamp, LogLevel logLevel, string category, string message ) {
            Timestamp = timestamp;
            LogLevel = logLevel;
            Category = category;
            Message = message;
        }

        public override string ToString() {
            return string.Format( "[{0:HH:mm:ss.fff}][{1}][{2}] {3}",
                DateTimeOffset.FromUnixTimeMilliseconds(Timestamp),
                LogLevel,
                Category,
                Message
            );
        }
        public string ToStringWith( bool timestamp = true, bool logLevel = true, bool category = true ) {
            if ( timestamp && logLevel && category ) {
                return ToString();
            }
            else if ( !( timestamp && logLevel && category ) ) {
                return Message;
            }

            StringBuilder stringBuilder = new StringBuilder();

            if ( timestamp ) {
                stringBuilder.AppendFormat( TimestampFormat, DateTimeOffset.FromUnixTimeMilliseconds( Timestamp ) );
            }
            if ( logLevel ) {
                stringBuilder.AppendFormat( InfoFormat, LogLevel );
            }
            if ( category ) {
                stringBuilder.AppendFormat( InfoFormat, Category );
            }
            stringBuilder.Append( " " + Message );

            return stringBuilder.ToString();
        }
    }
}
