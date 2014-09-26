using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dan200.Launcher.Util;
using Dan200.Launcher.Main;

namespace Dan200.Launcher.RSS
{
	public class RSSFile
	{
        public readonly IList<RSSChannel> Channels;

        public static RSSFile Download( string url, ProgressDelegate listener, ICancellable cancelObject )
        {
            try
            {
                Logger.Log( "Downloading RSS file from {0}", url );
                var request = HttpWebRequest.Create( url );
                request.Timeout = 15000;
                using( var response = request.GetResponse() )
                {
                    using( var stream = new ProgressStream( response.GetResponseStream(), response.ContentLength, listener, cancelObject ) )
                    {
                        try
                        {
                            return new RSSFile( stream );
                        }
                        finally
                        {
                            stream.Close();
                        }
                    }
                }
            }
            catch( Exception e )
            {
                Logger.Log( "Caught exception: {0}", e.ToString() );
                return null;
            }
        }

        public RSSFile()
        {
            Channels = new List<RSSChannel>();
        }

        public RSSFile( Stream stream ) : this()
        {
            // Read document
            Logger.Log( "Parsing RSS file" );
            var document = new XmlDocument();
            try
            {
                document.Load( stream );
            }
            catch( Exception e )
            {
                Logger.Log( "Caught exception: {0}", e.ToString() );
                return;
            }
            finally
            {
                stream.Close();
            }

            // Parse document
            var root = document.DocumentElement;
            if( root.Name == "rss" )
            {
                var rss = root;
                var channelItems = rss.GetElementsByTagName( "channel" ).OfType<XmlElement>();
                foreach( var channelItem in channelItems )
                {
                    var channel = new RSSChannel();

                    // Parse channel info
                    {
                        var title = channelItem[ "title" ];
                        if( title != null )
                        {
                            channel.Title = title.InnerText;
                        }
                        var description = channelItem[ "description" ];
                        if( description != null )
                        {
                            channel.Description = description.InnerText;
                        }
                        var link = channelItem[ "link" ];
                        if( link != null )
                        {
                            channel.Link = link.InnerText;
                        }
                    }

                    // Parse entries
                    {
                        var entryItems = channelItem.GetElementsByTagName( "item" ).OfType<XmlNode>();
                        foreach( var entryItem in entryItems )
                        {
                            var entry = new RSSEntry();

                            // Parse entry info
                            var title = entryItem[ "title" ];
                            if( title != null )
                            {
                                entry.Title = title.InnerText;
                            }
                            var description = entryItem[ "description" ];
                            if( description != null )
                            {
                                entry.Description = description.InnerText;
                            }
                            var link = entryItem[ "link" ];
                            if( link != null )
                            {
                                entry.Link = link.InnerText;
                            }

                            // Store entry
                            channel.Entries.Add( entry );
                        }
                    }

                    // Store channel
                    Channels.Add( channel );
                }
            }
        }
	}
}

