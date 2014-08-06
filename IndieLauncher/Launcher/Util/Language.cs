using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Dan200.Launcher.Util
{
	public class Language
	{
		private static IDictionary<string, Language> s_languages = new Dictionary<string, Language>();

        public static void LoadAll()
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            foreach( string resource in currentAssembly.GetManifestResourceNames() )
            {
                if( resource.StartsWith( "Resources.Languages." ) )
                {
                    string fileName = resource.Substring( "Resources.Languages.".Length );
                    if( fileName.EndsWith( ".lang" ) )
                    {
                        var kvp = new KeyValuePairs();
                        kvp.Load( currentAssembly.GetManifestResourceStream( resource ) );

                        string code = fileName.Substring( 0, fileName.Length - 5 );
                        s_languages.Add( code, new Language( code, kvp ) );
                    }
                }
            }
        }

        public static Language Get( string code )
        {
            if( s_languages.ContainsKey( code ) )
            {
                return s_languages[ code ];
            }
            return null;
        }

		public static Language GetMostSimilarTo( string code )
		{
			// Look for an exact match
            Language exactMatch = Language.Get( code );
			if( exactMatch != null )
			{
				return exactMatch;
			}

			int underscoreIndex = code.IndexOf( '_' );
			if( underscoreIndex != 0 )
			{
				// Look for a root match on the language part (ie: en_GB -> en)
				string langPart = (underscoreIndex > 0) ? code.Substring( 0, underscoreIndex ) : code;
                Language langPartMatch = Get( langPart );
				if( langPartMatch != null )
				{
					return langPartMatch;
				}

				// Look for a similar match on the language part (ie: en_GB -> en_US)
                foreach( string otherCode in s_languages.Keys )
				{
					if( otherCode.StartsWith( langPart ) )
					{
                        return Language.Get( otherCode );
					}
				}
			}

			// If there was nothing simular, use english
            return Language.Get( "en" );
		}

		private string m_code;
		private KeyValuePairs m_translations;

		public string Code
		{
			get
			{
				return m_code;
			}
		}

		public bool IsEnglish
		{
			get
			{
				return Code.StartsWith( "en" );
			}
		}

		public bool IsDebug
		{
			get
			{
				return Code == "debug";
			}
		}

		public Language Parent
		{
			get
			{
				if( m_translations.ContainsKey( "meta.parent_language" ) )
				{
                    return Language.Get( m_translations.GetString( "meta.parent_language" ) );
				}
				return null;
			}
		}

		public string Name
		{
			get
			{
				return Translate( "meta.native_language_name" );
			}
		}

		public string EnglishName
		{
			get
			{
				return Translate( "meta.english_language_name" );
			}
		}

		public IEnumerable<string> Translators
		{
			get
			{
				if( m_translations.ContainsKey( "meta.translator_name" ) )
				{
					var names = m_translations.GetString( "meta.translator_name" ).Split( ',' );
					for( int i = 0; i < names.Length; ++i )
					{
						var name = names[ i ].Trim();
						if( name.Length > 0 )
						{
							yield return name;
						}
					}
				}
			}
		}

		private Language( string code, KeyValuePairs translations )
		{
			m_code = code;
			m_translations = translations;
		}

        public bool HasTranslation( string symbol )
        {
            if( m_translations.ContainsKey( symbol ) )
            {
                return true;
            }
            if( Parent != null )
            {
                return Parent.HasTranslation( symbol );
            }
            return false;
        }

		public string Translate( string symbol )
		{
			if( m_translations.ContainsKey( symbol ) )
			{
				return m_translations.GetString( symbol );
			}
			if( Parent != null )
			{
				return Parent.Translate( symbol );
			}
			return symbol;
		}

        public string TranslateCount( string baseSymbol, long number )
        {
            if( number == 1 )
            {
                return Translate( baseSymbol + ".singular", number );
            }
            else
            {
                return Translate( baseSymbol + ".plural", number );
            }
        }

		public string Translate( string symbol, object arg1 )
		{
			return string.Format( Translate( symbol ), arg1 );
		}

		public string Translate( string symbol, object arg1, object arg2 )
		{
			return string.Format( Translate( symbol ), arg1, arg2 );
		}

		public string Translate( string symbol, object arg1, object arg2, object arg3 )
		{
			return string.Format( Translate( symbol ), arg1, arg2, arg3 );
		}

		public string Translate( string symbol, params object[] args )
		{
			return string.Format( Translate( symbol ), args );
		}
	}
}

