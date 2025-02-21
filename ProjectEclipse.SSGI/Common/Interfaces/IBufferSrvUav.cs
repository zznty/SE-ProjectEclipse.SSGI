using System;
using SharpDX.Direct3D11;

namespace ProjectEclipse.SSGI.Common.Interfaces
{
    public interface IBufferSrvUav : IBufferSrv, IDisposable
    {
        UnorderedAccessView Uav { get; }
    }
}
