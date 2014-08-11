using System;
using Dan200.Launcher.Util;

namespace Dan200.Launcher.Main
{
    public enum GameUpdatePrompt
    {
        None,
        DownloadNewVersion,
        LaunchOldVersion,
        Username,
        Password,
        UsernameAndPassword,
    }

    public static class GameUpdatePromptExtensions
    {
        public static string GetQuestion( this GameUpdatePrompt prompt, Language lang, string gameTitle )
        {
            switch( prompt )
            {
                case GameUpdatePrompt.None:
                {
                    return "";
                }
                case GameUpdatePrompt.DownloadNewVersion:
                {
                    return lang.Translate( "prompt.download_new_version", gameTitle );
                }
                case GameUpdatePrompt.LaunchOldVersion:
                {
                    return lang.Translate( "prompt.launch_old_version", gameTitle );
                }
                default:
                {
                    return "";
                }
            }
        }
    }
}

