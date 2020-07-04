using System;
using System.Collections.Generic;
using System.Text;

namespace MarkieUtilities.Core.Net {
    public class GithubHelper {
        public string UserName { get; }
        public string RepoName { get; }

        public string BaseUri { get; }
        public string BaseApiUri { get; }

        public GithubHelper( string userName, string repoName, string baseUri, string baseApiUri ) {
            UserName = userName;
            if ( UserName.IsNullOrEmpty() ) {
                throw new ArgumentException( "UserName is empty." );
            }

            RepoName = repoName;
            if ( RepoName.IsNullOrEmpty() ) {
                throw new ArgumentException( "RepoName is empty." );
            }

            BaseUri = baseUri;
            if ( BaseUri.IsNullOrEmpty() ) {
                throw new ArgumentException( "BaseUri is empty." );
            }

            BaseApiUri = baseApiUri;
            if ( BaseApiUri.IsNullOrEmpty() ) {
                throw new ArgumentException( "BaseApiUri is empty." );
            }
        }

        public string GetUri( params string[] args ) {
            StringBuilder builder = new StringBuilder( string.Format(
                "{0}/{1}/{2}/",
                BaseUri,
                UserName,
                RepoName
            ) );

            for ( int i = 0; i < args.Length; i++ ) {
                builder.Append( args[i] + "/" );
            }

            return builder.ToString();
        }

        public string GetReleaseApiUri( string tag ) {
            return string.Format(
                "{0}/repos/{1}/{2}/releases/{3}",
                BaseApiUri,
                UserName,
                RepoName,
                tag
            );
        }
        public string GetLatestReleaseApiUri() {
            return GetReleaseApiUri( "latest" );
        }

        public string GetReleaseUri( string tag ) {
            return GetUri( "releases", "tag", tag );
        }

        public string GetDownloadUri( string tag, string fileName ) {
            return string.Format(
                "{0}/{1}",
                GetUri( "releases", "download", tag ),
                fileName
            );
        }
    }
}
