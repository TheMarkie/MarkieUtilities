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

        public string GetUri( string section, string subsection, string tag ) {
            return string.Format(
                "{0}/{1}/{2}/{3}/{4}/{5}",
                BaseUri,
                UserName,
                RepoName,
                section,
                subsection,
                tag
            );
        }

        public string GetApiUri( string section, string subsection, string tag ) {
            return string.Format(
                "{0}/{1}/{2}/{3}/{4}/{5}",
                BaseApiUri,
                section,
                subsection,
                UserName,
                RepoName,
                tag
            );
        }

        public string GetReleaseApiUri( string tag ) {
            return GetApiUri( "repos", "releases", tag );
        }
        public string GetLatestReleaseApiUri() {
            return GetApiUri( "repos", "releases", "latest" );
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
