using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using MarkieUtilities.Core.Log;
using MarkieUtilities.Core.IO;

namespace MarkieUtilities.Core.Net {
    public class AdvancedHttpClient : HttpClient {
        public const int DefaultBufferSize = 81920;

        private static readonly ILogSource _log = Log.Log.GetLogSource( typeof( AdvancedHttpClient ) );

        public async Task<string> DownloadFileAsync(
            string uri,
            string path,
            CancellationToken? cancellationToken,
            ProgressCallback progressCallback = null
        ) {
            bool hasCancelToken = cancellationToken.HasValue;
            CancellationToken cancelToken = cancellationToken.GetValueOrDefault();

            Task<HttpResponseMessage> getAsyncTask;
            if ( hasCancelToken ) {
                getAsyncTask = GetAsync( uri, HttpCompletionOption.ResponseHeadersRead, cancelToken );
            }
            else {
                getAsyncTask = GetAsync( uri, HttpCompletionOption.ResponseHeadersRead );
            }

            using ( HttpResponseMessage response = await getAsyncTask ) {
                response.EnsureSuccessStatusCode();
#if DEBUG
                if ( !response.Content.Headers.ContentLength.HasValue ) {
                    _log.Warning( "Download request returned with unknown content length: " + uri );
                }
#endif

                using ( Stream contentStream = await response.Content.ReadAsStreamAsync() ) {
                    string fileName = response.Content.Headers.ContentDisposition.FileName;
                    path = PathUtilities.AppendFileToPath( path, fileName );
                    string directory = Path.GetDirectoryName( path );
                    if ( !Directory.Exists( directory ) ) {
                        Directory.CreateDirectory( directory );
                    }

                    using ( Stream fileStream = new FileStream( path, FileMode.Create, FileAccess.Write, FileShare.None, DefaultBufferSize, true ) ) {
                        _log.Info( "Downloading {0}...", fileName );

                        long bytesCopied = 0;
                        long totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault();
                        byte[] buffer = new byte[DefaultBufferSize];
                        do {
                            int bytesRead;
                            if ( hasCancelToken ) {
                                bytesRead = await contentStream.ReadAsync( buffer, 0, buffer.Length, cancelToken );
                            }
                            else {
                                bytesRead = await contentStream.ReadAsync( buffer, 0, buffer.Length );
                            }

                            if ( bytesRead <= 0 ) {
                                break;
                            }

                            if ( hasCancelToken ) {
                                await fileStream.WriteAsync( buffer, 0, bytesRead, cancelToken );
                            }
                            else {
                                await fileStream.WriteAsync( buffer, 0, bytesRead );
                            }

                            bytesCopied += bytesRead;

                            progressCallback?.Invoke( ( int ) bytesCopied, ( int ) totalBytes );
                        }
                        while ( true );

                        _log.Info( "Downloaded " + fileName );

                        return path;
                    }
                }
            }
        }
    }
}
