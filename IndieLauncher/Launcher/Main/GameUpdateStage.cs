using System;
using Dan200.Launcher.Util;

namespace Dan200.Launcher.Main
{
    public enum GameUpdateStage
    {
        NotStarted,
        CheckingForUpdate,
        ExtractingUpdate,
        DownloadingUpdate,
        InstallingUpdate,
        LaunchingGame,
        Finished,
        Cancelled,
        Failed
    }

    public static class GameUpdateStageExtensions
    {
        public static string GetStatus( this GameUpdateStage stage, Language lang )
        {
            switch( stage )
            {
                case GameUpdateStage.NotStarted:
                {
                    return "";
                }
                case GameUpdateStage.CheckingForUpdate:
                {
                    return lang.Translate( "status.checking" );
                }
                case GameUpdateStage.ExtractingUpdate:
                {
                    return lang.Translate( "status.extracting" );
                }
                case GameUpdateStage.DownloadingUpdate:
                {
                    return lang.Translate( "status.downloading" );
                }
                case GameUpdateStage.InstallingUpdate:
                {
                    return lang.Translate( "status.installing" );
                }
                case GameUpdateStage.LaunchingGame:
                {
                    return lang.Translate( "status.launching" );
                }
                case GameUpdateStage.Finished:
                {
                    return lang.Translate( "status.complete" );
                }
                case GameUpdateStage.Cancelled:
                {
                    return lang.Translate( "status.cancelled" );
                }
                case GameUpdateStage.Failed:
                {
                    return lang.Translate( "status.failed" );
                }
                default:
                {
                    return lang.Translate( "status.unknown" );
                }
            }
        }
    }
}
