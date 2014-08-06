using System;
using System.Threading;
using System.Threading.Tasks;
using Dan200.Launcher.RSS;

namespace Dan200.Launcher.Main
{
    public class GameUpdater
    {
        private string m_gameTitle;
        private string m_optionalGameVersion;
        private string m_optionalUpdateURL;

        private GameUpdateStage m_stage;
        private double m_progress;
        private bool m_cancelled;

        private GameUpdatePrompt m_currentPrompt;
        private bool m_promptResponse;
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

                    if( StageChanged != null )
                    {
                        StageChanged.Invoke( this, EventArgs.Empty );
                    }
                    if( ProgressChanged != null )
                    {
                        ProgressChanged.Invoke( this, EventArgs.Empty );
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
                    if( ProgressChanged != null )
                    {
                        ProgressChanged.Invoke( this, EventArgs.Empty );
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

        private bool Cancelled
        {
            get
            {
                lock( this )
                {
                    return m_cancelled;
                }
            }
        }

        public event EventHandler StageChanged;
        public event EventHandler ProgressChanged;
        public event EventHandler PromptChanged;

        public GameUpdater( string gameTitle, string optionalGameVersion, string optionalUpdateURL )
        {
            m_gameTitle = gameTitle;
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
                m_promptResponse = false;
                if( PromptChanged != null )
                {
                    PromptChanged.Invoke( this, EventArgs.Empty );
                }
            }
            m_promptWaitHandle.WaitOne();
            return m_promptResponse;
        }

        private bool DownloadAndInstall( string gameVersion, string downloadURL, bool isNew )
        {
            // Download the game
            if( !Installer.IsGameDownloaded( m_gameTitle, gameVersion ) )
            {
                string embeddedGameTitle, embeddedGameVersion, embeddedDownloadURL;
                if( Installer.GetEmbeddedGame( out embeddedGameTitle, out embeddedGameVersion, out embeddedDownloadURL ) && embeddedGameVersion == gameVersion )
                {
                    // Install from the embedded resources
                    Stage = GameUpdateStage.ExtractingUpdate;
                    if( !Installer.ExtractEmbeddedGame( delegate( int progress ) {
                        StageProgress = (double)progress / 100.0;
                    } ) )
                    {
                        Stage = GameUpdateStage.Failed;
                        return false;
                    }
                }
                else
                {
                    // Download from the URL
                    Stage = GameUpdateStage.DownloadingUpdate;
                    if( !Installer.DownloadGame( m_gameTitle, gameVersion, downloadURL, delegate( int progress ) {
                        StageProgress = (double)progress / 100.0;
                    } ) )
                    {
                        Stage = GameUpdateStage.Failed;
                        return false;
                    }
                }
                if( Cancelled )
                {
                    Stage = GameUpdateStage.Cancelled;
                    return false;
                }
                StageProgress = 1.0;
            }

            // Install the game
            Stage = GameUpdateStage.InstallingUpdate;
            if( !Installer.InstallGame( m_gameTitle, gameVersion ) )
            {
                Stage = GameUpdateStage.Failed;
                return false;
            }
            if( Cancelled )
            {
                Stage = GameUpdateStage.Cancelled;
                return false;
            }
            StageProgress = 0.99;
            Installer.RecordLatestInstalledVersion( m_gameTitle, gameVersion, isNew );
            StageProgress = 1.0;
            return true;
        }

        private bool Launch( string gameVersion )
        {
            Stage = GameUpdateStage.LaunchingGame;
            if( !GameLauncher.LaunchGame( m_gameTitle, gameVersion ) )
            {
                Stage = GameUpdateStage.Failed;
                return false;
            }
            StageProgress = 1.0;
            return true;
        }

        public bool CheckForUpdates( string updateURL, out string o_gameVersion, out string o_downloadURL, out bool o_gameVersionIsNewest )
        {
            // Download RSS file
            Stage = GameUpdateStage.CheckingForUpdate;
            var rssFile = RSSFile.Download( m_optionalUpdateURL, delegate(int percentage) {
                StageProgress = 0.99 * ((double)percentage / 100.0);
            } );
            if( Cancelled )
            {
                o_gameVersion = null;
                o_downloadURL = null;
                o_gameVersionIsNewest = false;
                return false;
            }
            if( rssFile == null )
            {
                o_gameVersion = null;
                o_downloadURL = null;
                o_gameVersionIsNewest = false;
                return false;
            }
            StageProgress = 0.99;

            // Inspect RSS file for version download info
            string gameDescription;
            string updateDescription;
            if( m_optionalGameVersion != null )
            {
                if( !Installer.GetSpecifiedVersionInfo( rssFile, m_gameTitle, m_optionalGameVersion, out gameDescription, out o_downloadURL, out updateDescription, out o_gameVersionIsNewest ) )
                {
                    o_gameVersion = null;
                    o_downloadURL = null;
                    return false;
                }
                else
                {
                    o_gameVersion = m_optionalGameVersion;
                }
            }
            else
            {
                if( !Installer.GetLatestVersionInfo( rssFile, m_gameTitle, out o_gameVersion, out gameDescription, out o_downloadURL, out updateDescription, out o_gameVersionIsNewest ) )
                {
                    o_gameVersion = null;
                    o_downloadURL = null;
                    return false;
                }
            }
            if( Cancelled )
            {
                o_gameVersion = null;
                o_downloadURL = null;
                return false;
            }
            StageProgress = 1.0;
            return true;
        }

        public void Start()
        {
            if( Stage == GameUpdateStage.NotStarted )
            {
                Task.Factory.StartNew( delegate
                {
                    // If a specific version is requested and already installed, just launch it
                    if( m_optionalGameVersion != null )
                    {
                        if( Installer.IsGameInstalled( m_gameTitle, m_optionalGameVersion ) )
                        {
                            Launch( m_optionalGameVersion );
                            return;
                        }
                    }

                    // Figure out what version we need to try and install (and where to get it)
                    string latestInstalledVersion = Installer.GetLatestInstalledVersion( m_gameTitle );
                    string embeddedGameTitle, embeddedGameVersion, embeddedGameUpdateURL;
                    Installer.GetEmbeddedGame( out embeddedGameTitle, out embeddedGameVersion, out embeddedGameUpdateURL );

                    string gameVersion, downloadURL;
                    bool gameVersionIsNewest = false;
                    if( m_optionalUpdateURL == null || !CheckForUpdates( m_optionalUpdateURL, out gameVersion, out downloadURL, out gameVersionIsNewest ) )
                    {
                        if( Cancelled )
                        {
                            Stage = GameUpdateStage.Cancelled;
                            gameVersion = null;
                            downloadURL = null;
                            return;
                        }
                        if( m_optionalGameVersion != null )
                        {
                            // Use the version specified
                            gameVersion = m_optionalGameVersion;
                            downloadURL = null;
                        }
                        else if( latestInstalledVersion != null )
                        {
                            // Use the latest installed version
                            gameVersion = latestInstalledVersion;
                            downloadURL = null;
                        }
                        else if( embeddedGameVersion != null)
                        {
                            // Use the embedded version
                            gameVersion = embeddedGameVersion;
                            downloadURL = null;
                        }
                        else
                        {
                            // Give up
                            Stage = GameUpdateStage.Failed;
                            gameVersion = null;
                            downloadURL = null;
                            return;
                        }
                    }

                    // See if the game needs to be updated
                    if( !Installer.IsGameInstalled( m_gameTitle, gameVersion ) )
                    {
                        if( m_optionalGameVersion != null )
                        {
                            // If a specific version was requested and we don't have it, we must download it
                            if( !DownloadAndInstall( gameVersion, downloadURL, gameVersionIsNewest ) )
                            {
                                return;
                            }
                        }
                        else
                        {
                            // If no specific version was requested and we have an existing version, give the player a choice
                            string fallbackVersion = null;
                            if( latestInstalledVersion != null )
                            {
                                fallbackVersion = latestInstalledVersion;
                            }
                            else if( embeddedGameVersion != null )
                            {
                                fallbackVersion = embeddedGameVersion;
                            }
                            if( fallbackVersion == null || ShowPrompt( GameUpdatePrompt.DownloadNewVersion ) )
                            {
                                if( !DownloadAndInstall( gameVersion, downloadURL, gameVersionIsNewest ) )
                                {
                                    if( fallbackVersion == null || !ShowPrompt( GameUpdatePrompt.LaunchOldVersion ) )
                                    {
                                        Stage = GameUpdateStage.Failed;
                                        return;
                                    }
                                    else
                                    {
                                        if( !Installer.IsGameInstalled( m_gameTitle, fallbackVersion ) &&
                                            !DownloadAndInstall( gameVersion, null, false ) )
                                        {
                                            Stage = GameUpdateStage.Failed;
                                            return;
                                        }
                                        else
                                        {
                                            gameVersion = fallbackVersion;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Launch the game
                    if( !Launch( gameVersion ) )
                    {
                        return;
                    }

                    // Finish
                    Stage = GameUpdateStage.Finished;
                } );
            }
        }

        public void AnswerPrompt( bool response )
        {
            lock( this )
            {
                if( m_currentPrompt != GameUpdatePrompt.None )
                {
                    m_currentPrompt = GameUpdatePrompt.None;
                    m_promptResponse = response;
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

