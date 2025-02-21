using System;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace ProjectEclipse.SSGI.Common.Interfaces
{
    public interface IBufferSrv : IDisposable
    {
        Buffer Buffer { get; }
        ShaderResourceView Srv { get; }
    }
}
