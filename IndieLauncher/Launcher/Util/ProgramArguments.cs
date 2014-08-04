using System;
using System.Collections.Generic;

namespace Dan200.Launcher.Util
{
	public class ProgramArguments : KeyValuePairs
	{
		public ProgramArguments( string[] args )
		{
			string lastOption = null;
			foreach( string arg in args )
			{
				if( arg.StartsWith( "-" ) )
				{
					if( lastOption != null )
					{
						Set( lastOption, true );
					}
					lastOption = arg.Substring( 1 );
				}
				else if( lastOption != null )
				{
					Set( lastOption, arg );
					lastOption = null;
				}
			}
			if( lastOption != null )
			{
				Set( lastOption, true );
			}
		}
	}
}
