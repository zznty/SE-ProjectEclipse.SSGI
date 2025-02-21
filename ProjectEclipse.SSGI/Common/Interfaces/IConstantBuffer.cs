using System;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace ProjectEclipse.SSGI.Common.Interfaces
{
    public interface IConstantBuffer : IDisposable
    {
        Buffer Buffer { get; }
    }
}
