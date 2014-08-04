using System;

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dan200.Launcher.Util
{
    public class KeyValuePairs
    {
        private string m_comment;
        private IDictionary<string, string> m_pairs;
        private bool m_modified;

        public string Comment
        {
            get
            {
                return m_comment;
            }
            set
            {
                if( m_comment != value )
                {
                    m_comment = value;
                    m_modified = true;
                }
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                return m_pairs.Keys;
            }
        }

        public bool Modified
        {
            get
            {
                return m_modified;
            }
            set
            {
                m_modified = value;
            }
        }

        public KeyValuePairs()
        {
            m_comment = null;
            m_pairs = new SortedDictionary<string, string>();
            m_modified = false;
        }

        public void Load( Stream stream )
        {
            using( var reader = new StreamReader( stream, Encoding.UTF8 ) )
            {
                Load( reader );
            }
        }

        public void Load( TextReader reader )
        {
            string line = null;
            while( (line = reader.ReadLine()) != null )
            {
                int commentIndex;
                if( line.StartsWith( "//" ) )
                {
                    commentIndex = 0;
                }
                else
                {
                    commentIndex = line.IndexOf( " //" );
                    commentIndex = (commentIndex >= 0) ? (commentIndex + 1) : -1;
                }
                if( commentIndex >= 0 )
                {
                    if( m_pairs.Count == 0 && Comment == null )
                    {
                        Comment = line.Substring( commentIndex + 2 ).Trim();
                    }
                    line = line.Substring( 0, commentIndex );
                }

                int equalsIndex = line.IndexOf( '=' );
                if( equalsIndex >= 0 )
                {
                    string key = line.Substring( 0, equalsIndex ).Trim();
                    string value = line.Substring( equalsIndex + 1 ).Trim();
                    if( value.Length > 0 )
                    {
                        Set( key, value );
                    }
                }
            }
            reader.Close();
        }

        public bool ContainsKey( string key )
        {
            return m_pairs.ContainsKey( key );
        }

        public void Remove( string key )
        {
            if( m_pairs.ContainsKey( key ) )
            {
                m_pairs.Remove( key );
                m_modified = true;
            }
        }

        public void Clear()
        {
            if( m_pairs.Count > 0 )
            {
                m_pairs.Clear();
                m_modified = true;
            }
        }

        public string GetString( string key )
        {
            return GetString( key, null );
        }

        public string GetString( string key, string _default )
        {
            if( m_pairs.ContainsKey( key ) )
            {
                return m_pairs[ key ];
            }
            return _default;
        }

        public int GetInteger( string key )
        {
            return GetInteger( key, 0 );
        }

        public int GetInteger( string key, int _default )
        {
            if( m_pairs.ContainsKey( key ) )
            {
                int result;
                if( int.TryParse( m_pairs[ key ], out result ) )
                {
                    return result;
                }
            }
            return _default;
        }

        public float GetFloat( string key )
        {
            return GetFloat( key, 0.0f );
        }

        public float GetFloat( string key, float _default )
        {
            if( m_pairs.ContainsKey( key ) )
            {
                float result;
                if( float.TryParse( m_pairs[ key ], out result ) )
                {
                    return result;
                }
            }
            return _default;
        }

        public bool GetBool( string key )
        {
            return GetBool( key, false );
        }

        public bool GetBool( string key, bool _default )
        {
            if( m_pairs.ContainsKey( key ) )
            {
                string value = m_pairs[ key ];
                if( value == "true" )
                {
                    return true;
                }
                if( value == "false" )
                {
                    return false;
                }
            }
            return _default;
        }

        public void Set( string key, string value )
        {
            if( m_pairs.ContainsKey( key ) )
            {
                if( value != null )
                {
                    if( m_pairs[ key ] != value )
                    {
                        m_pairs[ key ] = value;
                        m_modified = true;
                    }
                }
                else
                {
                    m_pairs.Remove( key );
                    m_modified = true;
                }
            }
            else if( value != null )
            {
                m_pairs.Add( key, value );
                m_modified = true;
            }
        }

        public void Set( string key, int value )
        {
            Set( key, value.ToString() );
        }

        public void Set( string key, float value )
        {
            Set( key, value.ToString() );
        }

        public void Set( string key, bool value )
        {
            Set( key, value ? "true" : "false" );
        }

        public void Ensure( string key, string _default )
        {
            Set( key, GetString( key, _default ) );
        }

        public void Ensure( string key, int _default )
        {
            Set( key, GetInteger( key, _default ) );
        }

        public void Ensure( string key, float _default )
        {
            Set( key, GetFloat( key, _default ) );
        }

        public void Ensure( string key, bool _default )
        {
            Set( key, GetBool( key, _default ) );
        }
    }
}

