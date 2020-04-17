using System.IO;

namespace MarkieUtilities.Core.IO {
    public static class PathUtilities {
        /// <summary>
        /// Append file to the path if it doesn't already contain any file.
        /// </summary>
        public static string AppendFileToPath( string path, string file ) {
            if ( path.IsNullOrEmpty() ) {
                return file;
            }
            else {
                string existingFile = Path.GetFileName( path );
                if ( existingFile.IsEmpty() ) {
                    path = Path.Combine( path, file );
                }
            }

            return path;
        }
    }
}
