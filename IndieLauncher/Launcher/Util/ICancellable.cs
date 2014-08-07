using System;

namespace Dan200.Launcher.Util
{
    public interface ICancellable
    {
        bool Cancelled { get; }
        void Cancel();
    }
}

