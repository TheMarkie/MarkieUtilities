using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MarkieUtilities.Core.Log;

namespace MarkieUtilities.Core.Config {
    public class IniFile : IDictionary<string, IniSection> {
        public const char CommentDelimiter = ';';
        public const char SectionStartChar = '[';
        public const char SectionEndChar = ']';
        public const char KeyValueDelimiter = '=';

        private static readonly ILogSource _log = Log.Log.GetLogSource( typeof( IniFile ) );

        private string _path;
        private readonly Dictionary<string, IniSection> _dictionary;

        private KeyValuePair<string, IniSection> _sectionCache;

        /// <summary>
        /// NOTE: This does not load the ini file, use IniFile.LoadAsync instead.
        /// </summary>
        public IniFile( string path, Dictionary<string, IniSection> dictionary ) {
            _path = path;
            _dictionary = dictionary;
        }

        //==============================================
        // Loading and Saving
        //==============================================
        #region Loading and Saving
        public static async Task<IniFile> LoadAsync( string path ) {
            return await Task.Run( () => {
                using ( StreamReader stream = new StreamReader( path ) ) {
                    Dictionary<string, IniSection> dictionary = new Dictionary<string, IniSection>();
                    string line = null;
                    IniSection section = null;
                    while ( ( line = stream.ReadLine() ) != null ) {
                        line = line.Trim();
                        if ( line.Length <= 0 || line[0].Equals( CommentDelimiter ) ) {
                            continue;
                        }

                        if ( line[0].Equals( SectionStartChar ) ) {
                            int i = line.IndexOf( SectionEndChar );
                            if ( i > 1 ) {
                                string name = line.Substring( 1, i - 1 ).Trim();
                                section = new IniSection();
                                dictionary[name] = section;
                            }
                        }
                        else if ( section != null ) {
                            int i = line.IndexOf( KeyValueDelimiter );
                            if ( i > 0 ) {
                                string key = line.Substring( 0, i ).Trim();
                                string value = line.Substring( i + 1 ).Trim();
                                if ( key.Length > 0 ) {
                                    section[key] = value;
                                }
                            }
                        }
                    }

                    return new IniFile( path, dictionary );
                }
            } );
        }

        public void Save( string path = null ) {
            using ( StreamWriter stream = new StreamWriter( path.IsNullOrEmpty() ? _path : path ) ) {
                string sectionFormat = SectionStartChar + "{0}" + SectionEndChar;
                string pairFormat = "{0}" + KeyValueDelimiter + "{1}";
                foreach ( KeyValuePair<string, IniSection> section in _dictionary ) {
                    stream.WriteLine( sectionFormat, section.Key );
                    foreach ( KeyValuePair<string, string> pair in section.Value ) {
                        stream.WriteLine( pairFormat, pair.Key, pair.Value );
                    }
                    stream.WriteLine();
                }

                stream.Flush();
            }
        }
        #endregion

        //==============================================
        // IDictionary implementation
        //==============================================
        #region IDictionary implementation
        #region Properties
        public ICollection<string> Keys => _dictionary.Keys;

        public ICollection<IniSection> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => false;
        #endregion

        #region Management
        public void Add( string key, IniSection value ) {
            _dictionary.Add( key, value );
        }

        public bool Remove( string key ) {
            return _dictionary.Remove( key );
        }

        public void Clear() {
            _dictionary.Clear();
        }

        public IniSection this[string key] {
            get {
                if ( _dictionary.TryGetValue( key, out IniSection value ) ) {
                    return value;
                }
                else {
                    return null;
                }
            }
            set {
                if ( _dictionary.ContainsKey( key ) ) {
                    _dictionary[key] = value;
                }
                else {
                    _dictionary.Add( key, value );
                }
            }
        }

        public bool ContainsKey( string key ) {
            return _dictionary.ContainsKey( key );
        }

        public bool TryGetValue( string key, out IniSection value ) {
            return _dictionary.TryGetValue( key, out value );
        }
        #endregion

        #region Enumerator
        public IEnumerator<KeyValuePair<string, IniSection>> GetEnumerator() {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _dictionary.GetEnumerator();
        }
        #endregion
        #endregion

        #region Not implemented
        public void Add( KeyValuePair<string, IniSection> item ) {
            throw new NotImplementedException();
        }

        public bool Remove( KeyValuePair<string, IniSection> item ) {
            throw new NotImplementedException();
        }

        public bool Contains( KeyValuePair<string, IniSection> item ) {
            throw new NotImplementedException();
        }

        public void CopyTo( KeyValuePair<string, IniSection>[] array, int arrayIndex ) {
            throw new NotImplementedException();
        }
        #endregion

        //==============================================
        // Functionality
        //==============================================
        #region Functionality
        /// <summary>
        /// NOTE: Cache your calls to this function to maintain performance.
        /// </summary>
        public bool TryGetValue<T>( string sectionName, string key, out T value ) where T : IConvertible {
            if ( _dictionary.TryGetValue( sectionName, out IniSection section ) ) {
                return section.TryGetValue( key, out value );
            }
            else {
                value = default;
                return false;
            }
        }

        public bool TryGetValue( string sectionName, string key, out string value ) {
            if ( sectionName == _sectionCache.Key ) {
                return _sectionCache.Value.TryGetValue( key, out value );
            }
            else if ( _dictionary.TryGetValue( sectionName, out IniSection section ) ) {
                return section.TryGetValue( key, out value );
            }
            else {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// NOTE: Cache your calls to this function to maintain performance.
        /// </summary>
        public T GetValueOrDefault<T>( string sectionName, string key, T defaultValue ) where T : IConvertible {
            if ( _dictionary.TryGetValue( sectionName, out IniSection section ) ) {
                return section.GetValueOrDefault( key, defaultValue );
            }
            else {
                return defaultValue;
            }
        }

        public string GetValue( string sectionName, string key, string defaultValue = default ) {
            if ( _dictionary.TryGetValue( sectionName, out IniSection section ) ) {
                return section.GetValueOrDefault( key, defaultValue );
            }
            else {
                return defaultValue;
            }
        }
        #endregion
    }
}