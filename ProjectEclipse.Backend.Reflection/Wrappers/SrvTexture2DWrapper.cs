using ProjectEclipse.Common;
using ProjectEclipse.Common.Interfaces;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using VRageMath;

namespace ProjectEclipse.Backend.Reflection.Wrappers
{
    public readonly struct SrvTexture2DWrapper : ITexture2DSrv
    {
        public Texture2D Texture => (Texture2D)ViewBindableAccessor.GetResource(_srvBindable);
        public ShaderResourceView Srv => ViewBindableAccessor.GetSrv(_srvBindable);
        public Vector2I Size => ViewBindableAccessor.GetSize(_srvBindable);
        public Format Format => Srv.Description.Format;
        public int MipLevels => Srv.Description.Texture2D.MipLevels;

        private readonly object _srvBindable;

        public SrvTexture2DWrapper(object srvTex2dBindable)
        {
            if (!srvTex2dBindable.GetType().IsAssignableTo(ViewBindableAccessor.Type_ISrvBindable))
                throw new ArgumentException();
            _srvBindable = srvTex2dBindable;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
