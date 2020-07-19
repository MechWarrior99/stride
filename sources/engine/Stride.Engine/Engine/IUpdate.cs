using System;
using System.Collections.Generic;
using System.Text;

namespace Stride.Engine
{
    public interface IUpdate
    {
        void Update();
    }

    public interface IStart
    {
        void Start();
    }

    public interface ICancel
    {
        void Cancel();
    }
}
