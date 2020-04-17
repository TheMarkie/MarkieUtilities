using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using MarkieUtilities.Core.Log;

namespace MarkieUtilities.Core.Config {
    public class IniSection : IDictionary<string, string> {
        private static readonly ILogSource _log = Log.Log.GetLogSource( typeof( IniSection ) );

        private readonly Dictionary<string, string> _dictionary;

        //==============================================
        // Construction
        //==============================================
        #region Construction
        public IniSection() {
            _dictionary = new Dictionary<string, string>();
        }

        public IniSection( Dictionary<string, string> dictionary ) {
            _dictionary = new Dictionary<string, string>( dictionary );
        }

        public IniSection( IniSection iniSection ) {
            _dictionary = new Dictionary<string, string>( iniSection._dictionary );
        }
        #endregion

        //==============================================
        // IDictionary implementation
        //==============================================
        #region IDictionary implementation
        #region Properties
        public ICollection<string> Keys => _dictionary.Keys;

        public ICollection<string> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => false;
        #endregion

        #region Management
        public void Add( string key, string value ) {
            _dictionary.Add( key, value );
        }

        public bool Remove( string key ) {
            return _dictionary.Remove( key );
        }

        public void Clear() {
            _dictionary.Clear();
        }

        public string this[string key] {
            get {
                if ( _dictionary.TryGetValue( key, out string value ) ) {
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

        public bool TryGetValue( string key, out string value ) {
            return _dictionary.TryGetValue( key, out value );
        }
        #endregion

        #region Enumerator
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _dictionary.GetEnumerator();
        }
        #endregion
        #endregion

        #region Not implemented
        public void Add( KeyValuePair<string, string> item ) {
            throw new NotImplementedException();
        }

        public bool Remove( KeyValuePair<string, string> item ) {
            throw new NotImplementedException();
        }

        public bool Contains( KeyValuePair<string, string> item ) {
            throw new NotImplementedException();
        }

        public void CopyTo( KeyValuePair<string, string>[] array, int arrayIndex ) {
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
        public bool TryGetValue<T>( string key, out T value ) where T : IConvertible {
            if ( _dictionary.TryGetValue( key, out string stringValue ) ) {
                try {
                    value = ( T ) Convert.ChangeType( stringValue, typeof( T ), CultureInfo.InvariantCulture );
                    return true;
                }
                catch ( Exception e ) {
                    _log.Warning( "Failed to convert value to type '{0}': {1} ['{2}': '{3}']", typeof( T ), e.Message, key, stringValue );
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// NOTE: Cache your calls to this function to maintain performance.
        /// </summary>
        public T GetValueOrDefault<T>( string key, T defaultValue = default ) where T : IConvertible {
            if ( _dictionary.TryGetValue( key, out string value ) ) {
                try {
                    return ( T ) Convert.ChangeType( value, typeof( T ), CultureInfo.InvariantCulture );
                }
                catch ( Exception e ) {
                    _log.Warning( "Failed to convert value to type '{0}': {1} ['{2}': '{3}']", typeof( T ), e.Message, key, value );
                }
            }

            return defaultValue;
        }

        public string GetValueOrDefault( string key, string defaultValue = default ) {
            if ( _dictionary.TryGetValue( key, out string value ) ) {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Get all the specified values and concat them into a string.
        /// </summary>
        public string GetCombinedValues( string seperator, params string[] keys ) {
            if ( keys.Length <= 0 ) {
                return string.Empty;
            }

            List<string> values = new List<string>();
            for ( int i = 0, count = keys.Length; i < count; i++ ) {
                if ( _dictionary.TryGetValue( keys[i], out string value ) ) {
                    values.Add( value );
                }
            }

            return string.Join( seperator, values );
        }
        #endregion
    }
}
