using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using VRageMath;

namespace ProjectEclipse.Common.Interfaces
{
    public interface ITexture2DSrv : IDisposable
    {
        Texture2D Texture { get; }
        ShaderResourceView Srv { get; }
        Vector2I Size { get; }
        Format Format { get; }
    }
}
