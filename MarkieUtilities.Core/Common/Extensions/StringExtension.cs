namespace MarkieUtilities.Core {
    public static class StringExtension {
        public static bool IsNullOrEmpty( this string s ) {
            return string.IsNullOrEmpty( s );
        }
        public static bool IsEmpty( this string s ) {
            return s.Length == 0;
        }
    }
}
