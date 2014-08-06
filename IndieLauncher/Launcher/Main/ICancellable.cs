using System;

namespace Dan200.Launcher.Main
{
    public interface ICancellable
    {
        bool Cancelled { get; }
        void Cancel();
    }
}

