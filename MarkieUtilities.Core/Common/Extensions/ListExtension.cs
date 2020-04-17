using System.Collections.Generic;

namespace MarkieUtilities.Core {
    public static class ListExtension {
        public static bool IsNullOrEmpty<T>( this List<T> list ) {
            return ( list == null || list.Count <= 0 );
        }
    }
}
