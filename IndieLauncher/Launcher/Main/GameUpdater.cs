using System;
using System.Threading;
using System.Threading.Tasks;
using Dan200.Launcher.RSS;
using Dan200.Launcher.Util;

namespace Dan200.Launcher.Main
{
    public class GameUpdater : ICancellable
    {
        private string m_gameTitle;
        private string m_gameDescription;
        private string m_optionalGameVersion;
        private string m_optionalUpdateURL;

        private GameUpdateStage m_stage;
        private double m_progress;
        private bool m_cancelled;

        private GameUpdatePrompt m_currentPrompt;
        private string m_customMessage;
        private bool m_promptResponse;
        private string m_promptUsername;
        private string m_promptPassword;
        private AutoResetEvent m_promptWaitHandle;

        public GameUpdateStage Stage
        {
            get
            {
                lock( this )
                {
                    return m_stage;
                }
            }
            private set
            {
                lock( this )
                {
                    m_stage = value;
                    if( value == GameUpdateStage.Finished ||
                        value == GameUpdateStage.Cancelled ||
                        value == GameUpdateStage.Failed )
                    {
                        m_progress = 1.0f;
                    }
                    else
                    {
                        m_progress = 0.0f;
                    }

                    var stageChanged = StageChanged;
                    if( stageChanged != null )
                    {
                        stageChanged.Invoke( this, EventArgs.Empty );
                    }
                    var progressChanged = ProgressChanged;
                    if( progressChanged != null )
                    {
                        progressChanged.Invoke( this, EventArgs.Empty );
                    }
                }
            }
        }

        public double StageProgress
        {
            get
            {
                lock( this )
                {
                    return m_progress;
                }
            }
            private set
            {
                lock( this )
                {
                    m_progress = value;
                    var progressChanged = ProgressChanged;
                    if( progressChanged != null )
                    {
                        progressChanged.Invoke( this, EventArgs.Empty );
                    }
                }
            }
        }

        public GameUpdatePrompt CurrentPrompt
        {
            get
            {
                lock( this )
                {
                    return m_currentPrompt;
                }
            }
        }

        public string CustomMessage
        {
            get
            {
                lock( this )
                {
                    return m_customMessage;
                }
            }
        }

        public bool Cancelled
        {
            get
            {
                lock( this )
                {
                    return m_cancelled;
                }
            }
        }

        public string GameDescription
        {
            get
            {
                lock( this )
                {
                    return m_gameDescription;
                }
            }
            private set
            {
                lock( this )
                {
                    m_gameDescription = value;
                }
            }
        }

        public string PreviouslyEnteredUsername
        {
            get
            {
                lock( this )
                {
                    return m_promptUsername;
                }
            }
        }

        public string PreviouslyEnteredPassword
        {
            get
            {
                lock( this )
                {
                    return m_promptPassword;
                }
            }
        }

        public event EventHandler StageChanged;
        public event EventHandler ProgressChanged;
        public event EventHandler PromptChanged;

        public GameUpdater( string gameTitle, string optionalGameVersion, string optionalUpdateURL )
        {
            m_gameTitle = gameTitle;
            m_gameDescription = gameTitle;
            m_optionalGameVersion = optionalGameVersion;
            m_optionalUpdateURL = optionalUpdateURL;

            m_stage = GameUpdateStage.NotStarted;
            m_progress = 0.0f;
            m_cancelled = false;

            m_currentPrompt = GameUpdatePrompt.None;
            m_promptWaitHandle = new AutoResetEvent( false );
        }

        private bool ShowPrompt( GameUpdatePrompt prompt )
        {
            lock( this )
            {
                m_currentPrompt = prompt;
                var promptChanged = PromptChanged;
                if( promptChanged != null )
                {
                    promptChanged.Invoke( this, EventArgs.Empty );
                }
            }
            m_promptWaitHandle.WaitOne();
            return m_promptResponse;
        }

        private bool ShowUsernamePrompt( ref string o_username )
        {
            if( ShowPrompt( GameUpdatePrompt.Username ) )
            {
                o_username = m_promptUsername;
                return true;
            }
            return false;
        }

        private bool ShowPasswordPrompt( ref string o_password )
        {
            if( ShowPrompt( GameUpdatePrompt.Password ) )
            {
                o_password = m_promptPassword;
                return true;
            }
            return false;
        }

        private bool ShowUsernameAndPasswordPrompt( ref string o_username, ref string o_password )
        {
            if( ShowPrompt( GameUpdatePrompt.UsernameAndPassword ) )
            {
                o_username = m_promptUsername;
                o_password = m_promptPassword;
                return true;
            }
            return false;
        }

        private bool ShowCustomMessagePrompt( string customMessage )
        {
            m_customMessage = customMessage;
            return ShowPrompt( GameUpdatePrompt.CustomMessage );
        }

        private bool Extract()
        {
            string embeddedGameVersion = Installer.GetEmbeddedGameVersion( m_gameTitle );
            if( !Installer.IsGameDownloaded( m_gameTitle, embeddedGameVersion ) )
            {
                Stage = GameUpdateStage.ExtractingUpdate;
                if( !Installer.ExtractEmbeddedGame( delegate( int progress ) {
                    StageProgress = (double)progress / 100.0;
                }, this ) )
                {
                    return false;
                }
                if( Cancelled )
                {
                    return false;
                }
                StageProgress = 1.0;
            }
            return true;
        }

        private bool Download( string gameVersion, string downloadURL, string username=null, string password=null )
        {
            if( !Installer.IsGameDownloaded( m_gameTitle, gameVersion ) )
            {
                Stage = GameUpdateStage.DownloadingUpdate;

                string embeddedGameTitle, embeddedGameVersion, embeddedGameURL, embeddedUsername, embeddedPassword;
                if( Installer.GetEmbeddedGameInfo( out embeddedGameTitle, out embeddedGameVersion, out embeddedGameURL, out embeddedUsername, out embeddedPassword ) )
                {
                    if( username == null )
                    {
                        username = embeddedUsername;
                    }
                    if( password == null )
                    {
                        password = embeddedPassword;
                    }
                }

                bool authFailure;
                string customMessage;
                if( !Installer.DownloadGame(
                    m_gameTitle, gameVersion,
                    downloadURL, 
                    username,
                    password,
                    delegate( int progress ) {
                        StageProgress = (double)progress / 100.0;
                    },
                    this,
                    out authFailure,
                    out customMessage
                ) )
                {
                    if( Cancelled )
                    {
                        return false;
                    }
                    if( customMessage != null && !ShowCustomMessagePrompt( customMessage ) )
                    {
                        return false;
                    }
                    if( authFailure )
                    {
                        if( embeddedUsername == null && embeddedPassword == null )
                        {
                            if( ShowUsernameAndPasswordPrompt( ref username, ref password ) )
                            {
                                return Download( gameVersion, downloadURL, username, password );
                            }
                        }
                        else if( embeddedUsername == null )
                        {
                            if( ShowUsernamePrompt( ref username ) )
                            {
                                return Download( gameVersion, downloadURL, username, embeddedPassword );
                            }
                        }
                        else if( embeddedPassword == null )
                        {
                            if( ShowPasswordPrompt( ref password ) )
                            {
                                return Download( gameVersion, downloadURL, embeddedUsername, password );
                            }
                        }
                    }
                    return false;
                }
                if( Cancelled )
                {
                    return false;
                }
                if( customMessage != null && !ShowCustomMessagePrompt( customMessage ) )
                {
                    return false;
                }
                StageProgress = 1.0;
            }
            return true;
        }

        private bool Install( string gameVersion )
        {
            if( !Installer.IsGameInstalled( m_gameTitle, gameVersion ) )
            {
                Stage = GameUpdateStage.InstallingUpdate;
                if( !Installer.InstallGame( m_gameTitle, gameVersion, delegate( int progress ) {
                    StageProgress = (double)progress / 100.0;
                }, this ) )
                {
                    return false;
                }
                if( Cancelled )
                {
                    return false;
                }
                StageProgress = 1.0;
            }
            return true;
        }

        private bool DownloadAndInstall( string gameVersion, string downloadURL, bool isLatest )
        {
            if( !Installer.IsGameInstalled( m_gameTitle, gameVersion ) )
            {
                if( !Download( gameVersion, downloadURL ) )
                {
                    return false;
                }
                if( !Install( gameVersion ) )
                {
                    return false;
                }
            }
            Installer.RecordLatestInstalledVersion( m_gameTitle, gameVersion, isLatest );
            return true;
        }
    
        private bool ExtractAndInstall()
        {
            string embeddedGameVersion = Installer.GetEmbeddedGameVersion( m_gameTitle );
            if( !Installer.IsGameInstalled( m_gameTitle, embeddedGameVersion ) )
            {
                if( !Extract() )
                {
                    return false;
                }
                if( !Install( embeddedGameVersion ) )
                {
                    return false;
                }
            }
            Installer.RecordLatestInstalledVersion( m_gameTitle, embeddedGameVersion, false );
            return true;
        }

        private bool Launch( string gameVersion )
        {
            Stage = GameUpdateStage.LaunchingGame;
            if( Cancelled )
            {
                return false;
            }
            if( !GameLauncher.LaunchGame( m_gameTitle, gameVersion ) )
            {
                return false;
            }
            StageProgress = 1.0;
            return true;
        }

        private RSSFile DownloadRSSFile( string updateURL )
        {
            // Download RSS file
            Stage = GameUpdateStage.CheckingForUpdate;
            var rssFile = RSSFile.Download( m_optionalUpdateURL, delegate(int percentage) {
                StageProgress = (double)percentage / 100.0;
            }, this );
            if( rssFile == null )
            {
                return null;
            }
            if( Cancelled )
            {
                return null;
            }
            StageProgress = 1.0;
            return rssFile;
        }

        public bool GetSpecificDownloadURL( string updateURL, string gameVersion, out string o_downloadURL, out bool o_isNewest )
        {
            // Get the RSS file
            var rssFile = DownloadRSSFile( updateURL );
            if( rssFile == null )
            {
                o_downloadURL = null;
                o_isNewest = false;
                return false;
            }

            // Inspect RSS file for version download info
            string gameDescription;
            string updateDescription;
            if( !Installer.GetSpecificVersionInfo( rssFile, m_gameTitle, gameVersion, out gameDescription, out o_downloadURL, out updateDescription, out o_isNewest ) )
            {
                o_downloadURL = null;
                o_isNewest = false;
                return false;
            }
            GameDescription = gameDescription;
            return true;
        }

        public bool GetLatestDownloadURL( string updateURL, out string o_gameVersion, out string o_downloadURL )
        {
            // Get the RSS file
            var rssFile = DownloadRSSFile( updateURL );
            if( rssFile == null )
            {
                o_gameVersion = null;
                o_downloadURL = null;
                return false;
            }

            // Inspect RSS file for version download info
            string gameDescription;
            string updateDescription;
            bool gameVersionIsNewest;
            if( !Installer.GetLatestVersionInfo( rssFile, m_gameTitle, out o_gameVersion, out gameDescription, out o_downloadURL, out updateDescription, out gameVersionIsNewest ) )
            {
                o_gameVersion = null;
                o_downloadURL = null;
                return false;
            }
            GameDescription = gameDescription;
            return true;
        }

        private void Fail()
        {
            Stage = GameUpdateStage.Failed;
        }

        private void Finish()
        {
            Stage = GameUpdateStage.Finished;
        }

        private void FailOrCancel()
        {
            if( Cancelled )
            {
                Stage = GameUpdateStage.Cancelled;
            }
            else
            {
                Stage = GameUpdateStage.Failed;
            }
        }

        public bool TryCancel()
        {
            if( Cancelled )
            {
                Stage = GameUpdateStage.Cancelled;
                return true;
            }
            return false;
        }

        public void Start()
        {
            if( Stage == GameUpdateStage.NotStarted )
            {
                Task.Factory.StartNew( delegate()
                {
                    try
                    {
                        string latestInstalledVersion = Installer.GetLatestInstalledVersion( m_gameTitle );
                        string embeddedGameVersion = Installer.GetEmbeddedGameVersion( m_gameTitle );
                        if( m_optionalGameVersion != null )
                        {
                            // A specific version has been requested
                            // Try to locate it:
                            if( Installer.IsGameInstalled( m_gameTitle, m_optionalGameVersion ) )
                            {
                                if( m_optionalGameVersion == embeddedGameVersion )
                                {
                                    // Try to extract it
                                    if( !ExtractAndInstall() )
                                    {
                                        FailOrCancel();
                                        return;
                                    }
                                }
                                else if( m_optionalUpdateURL != null )
                                {
                                    // Try to download it
                                    string downloadURL;
                                    bool isNewest;
                                    if( !GetSpecificDownloadURL( m_optionalUpdateURL, m_optionalGameVersion, out downloadURL, out isNewest ) )
                                    {
                                        FailOrCancel();
                                        return;
                                    }
                                    if( !DownloadAndInstall( m_optionalGameVersion, downloadURL, isNewest ) )
                                    {
                                        FailOrCancel();
                                        return;
                                    }
                                }
                                else
                                {
                                    // Give up
                                    Fail();
                                    return;
                                }
                            }

                            // Try to run it
                            if( !Launch( m_optionalGameVersion ) )
                            {
                                FailOrCancel();
                                return;
                            }

                            // Finish
                            Finish();
                        }
                        else
                        {
                            // The "latest" version has been requested
                            // Try to determine what it is:
                            string latestVersion = null;
                            string latestVersionDownloadURL = null;
                            if( m_optionalUpdateURL != null )
                            {
                                if( !GetLatestDownloadURL( m_optionalUpdateURL, out latestVersion, out latestVersionDownloadURL ) )
                                {
                                    if( TryCancel() )
                                    {
                                        return;
                                    }
                                }
                            }

                            string launchVersion = null;
                            if( latestVersion != null )
                            {
                                if( Installer.IsGameInstalled( m_gameTitle, latestVersion ) )
                                {
                                    // If we already have it, there's nothing to do
                                    launchVersion = latestVersion;
                                    Installer.RecordLatestInstalledVersion( m_gameTitle, latestVersion, true );
                                }
                                else
                                {
                                    // Try to download it (with the users consent)
                                    if( latestVersionDownloadURL != null )
                                    {
                                        bool fallbackAvailable = (latestInstalledVersion != null) || (embeddedGameVersion != null);
                                        bool userPromptResult = false;
                                        if( fallbackAvailable )
                                        {
                                            userPromptResult = ShowPrompt( GameUpdatePrompt.DownloadNewVersion );
                                            if( TryCancel() )
                                            {
                                                return;
                                            }
                                        }
                                        if( !fallbackAvailable || userPromptResult )
                                        {
                                            if( TryCancel() )
                                            {
                                                return;
                                            }
                                            if( DownloadAndInstall( latestVersion, latestVersionDownloadURL, true ) )
                                            {
                                                launchVersion = latestVersion;
                                            }
                                            else
                                            {
                                                if( TryCancel() )
                                                {
                                                    return;
                                                }
                                                if( !fallbackAvailable )
                                                {
                                                    Fail();
                                                    return;
                                                }
                                                else if( !ShowPrompt( GameUpdatePrompt.LaunchOldVersion ) )
                                                {
                                                    Fail();
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Try one of the fallback methods
                            if( launchVersion == null )
                            {
                                if( latestInstalledVersion != null )
                                {
                                    launchVersion = latestInstalledVersion;
                                }
                                else if( embeddedGameVersion != null )
                                {
                                    if( ExtractAndInstall() )
                                    {
                                        launchVersion = embeddedGameVersion;
                                    }
                                    else
                                    {
                                        FailOrCancel();
                                        return;
                                    }
                                }
                                else
                                {
                                    Fail();
                                    return;
                                }
                            }

                            // Try to run it
                            if( !Launch( launchVersion ) )
                            {
                                FailOrCancel();
                                return;
                            }

                            // Finish
                            Finish();
                        }
                    }
                    catch( Exception e )
                    {
                        Console.WriteLine( e.ToString() );
                        Console.WriteLine( e.StackTrace );
                        Fail();
                    }
                } );
            }
        }

        public void AnswerPrompt( bool response, string username=null, string password=null )
        {
            lock( this )
            {
                if( m_currentPrompt != GameUpdatePrompt.None )
                {
                    m_currentPrompt = GameUpdatePrompt.None;
                    m_promptResponse = response;
                    m_promptUsername = username;
                    m_promptPassword = password;
                    m_promptWaitHandle.Set();
                }
            }
        }

        public void Cancel()
        {
            lock( this )
            {
                m_cancelled = true;
            }
        }
    }
}

