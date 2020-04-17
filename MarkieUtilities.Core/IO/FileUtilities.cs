using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace MarkieUtilities.Core.IO {
    public static class FileUtilities {
        public const int DefaultBufferSize = 81920;

        /// <summary>
        /// Async version of ExtractToDirectory with progress reporting.
        /// </summary>
        public static async Task ExtractToDirectoryAsync(
            string file,
            string path,
            ProgressCallback progressCallback = null,
            bool deleteAfter = false
        ) {
            if ( path.IsNullOrEmpty() ) {
                path = Path.GetDirectoryName( file );
            }

            using ( FileStream stream = File.OpenRead( file ) ) {
                using ( ZipArchive zipFile = new ZipArchive( stream, ZipArchiveMode.Read ) ) {
                    int count = 0;
                    int total = zipFile.Entries.Count;
                    foreach ( ZipArchiveEntry entry in zipFile.Entries ) {
                        await Task.Run( () => {
                            string destination = Path.GetFullPath( Path.Combine( path, entry.FullName ) );

                            if ( Path.GetFileName( destination ).IsEmpty() ) {
                                if ( !Directory.Exists( destination ) ) {
                                    Directory.CreateDirectory( destination );
                                }
                            }
                            else {
                                string directory = Path.GetDirectoryName( destination );
                                if ( !Directory.Exists( directory ) ) {
                                    Directory.CreateDirectory( directory );
                                }

                                entry.ExtractToFile( destination, true );
                            }
                        } );

                        count += 1;
                        progressCallback?.Invoke( count, total );
                    }
                }
            }

            if ( deleteAfter ) {
                File.Delete( file );
            }
        }

        /// <summary>
        /// Copy all files from one directory to the other.
        /// </summary>
        public static async Task CopyAllAsync(
            string sourceDirectory,
            string destinationDirectory,
            CancellationToken? cancellationToken,
            ProgressCallback progressCallback = null,
            bool recursive = false
        ) {
            await CopyAllAsyncInternal(
                sourceDirectory,
                destinationDirectory,
                cancellationToken,
                progressCallback,
                recursive,
                0,
                0
            );
        }

        internal static async Task CopyAllAsyncInternal(
            string sourceDirectory,
            string destinationDirectory,
            CancellationToken? cancellationToken,
            ProgressCallback progressCallback,
            bool recursive,
            int count,
            int total
        ) {
            bool hasCancelToken = cancellationToken.HasValue;
            CancellationToken cancelToken = cancellationToken.GetValueOrDefault();

            if ( !Directory.Exists( sourceDirectory ) ) {
                throw new DirectoryNotFoundException( "Source directory does not exist or could not be found: " + sourceDirectory );
            }

            if ( !Directory.Exists( destinationDirectory ) ) {
                Directory.CreateDirectory( destinationDirectory );
            }

            string[] files = Directory.GetFiles( sourceDirectory );
            total += files.Length;
            foreach ( string file in files ) {
                string path = Path.Combine( destinationDirectory, file.Substring( file.LastIndexOf( "\\" ) + 1 ) );
                using ( FileStream sourceStream = File.Open( file, FileMode.Open ) ) {
                    using ( FileStream destinationStream = File.Create( path ) ) {
                        if ( hasCancelToken ) {
                            await sourceStream.CopyToAsync( destinationStream, DefaultBufferSize, cancelToken );
                        }
                        else {
                            await sourceStream.CopyToAsync( destinationStream, DefaultBufferSize );
                        }

                        count += 1;
                        progressCallback?.Invoke( count, total );
                    }
                }
            }

            if ( recursive ) {
                IEnumerable<string> directories = Directory.GetDirectories( sourceDirectory );
                foreach ( string directory in directories ) {
                    string path = Path.Combine( destinationDirectory, directory.Substring( directory.LastIndexOf( "\\" ) + 1 ) );
                    await CopyAllAsyncInternal(
                        directory,
                        path,
                        cancellationToken,
                        progressCallback,
                        recursive,
                        count,
                        total
                    );
                }
            }
        }
    }
}
