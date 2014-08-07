using System;
using System.IO;
using Dan200.Launcher.Main;

namespace Dan200.Launcher.Util
{
    public delegate void ProgressDelegate( int percentage );

    public class ProgressStream : Stream
    {
        private Stream m_innerStream;
        private ProgressDelegate m_listener;
        private ICancellable m_cancelObject;

        private long m_length;
        private long m_position;
        private int m_lastProgress;

        public override bool CanRead
        {
            get
            {
                return m_innerStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return m_innerStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return m_innerStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return m_position;
            }
            set
            {
                m_innerStream.Position = value;
                m_position = value;
                EmitProgress();
            }
        }

        public ProgressStream( Stream innerStream, long lengthHint, ProgressDelegate listener, ICancellable cancelObject )
        {
            m_innerStream = innerStream;
            m_listener = listener;
            m_cancelObject = cancelObject;

            m_length = lengthHint;
            m_position = 0;
            m_lastProgress = -1;
            EmitProgress();
        }

        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                m_innerStream.Dispose();
            }
        }

        public override void Close()
        {
            m_innerStream.Close();
        }

        public override void Flush()
        {
            m_innerStream.Flush();
        }

        public override int ReadByte()
        {
            CheckCancel();
            var result = m_innerStream.ReadByte();
            m_position++;
            EmitProgress();
            return result;
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            CheckCancel();
            var result = m_innerStream.Read( buffer, offset, count );
            m_position += result;
            EmitProgress();
            return result;
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            CheckCancel();
            var result = m_innerStream.Seek( offset, origin );
            m_position = result;
            EmitProgress();
            return result;
        }

        public override void SetLength( long value )
        {
            throw new InvalidOperationException();
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            throw new InvalidOperationException();
        }

        private void EmitProgress()
        {
            long length = m_length;
            if( length < 0 && m_innerStream.CanSeek )
            {
                length = m_innerStream.Length;
            }

            if( length > 0 )
            {
                int percentage = Math.Min( (int)((m_position * 100) / length), 100 );
                if( percentage != m_lastProgress )
                {
                    m_listener.Invoke( percentage );
                    m_lastProgress = percentage;
                }
            }
        }

        private void CheckCancel()
        {
            if( m_cancelObject.Cancelled )
            {
                throw new IOCancelledException();
            }
        }
    }
}

