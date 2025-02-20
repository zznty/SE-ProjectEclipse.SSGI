using ProjectEclipse.Common;
using ProjectEclipse.Common.Interfaces;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using VRageMath;

namespace ProjectEclipse.Backend.Reflection.Wrappers
{
    public readonly struct RtvTexture2DWrapper : ITexture2DSrvRtv
    {
        public Texture2D Texture => (Texture2D)ViewBindableAccessor.GetResource(_rtvBindable);
        public ShaderResourceView Srv => ViewBindableAccessor.GetSrv(_rtvBindable);
        public RenderTargetView Rtv => ViewBindableAccessor.GetRtv(_rtvBindable);
        public Vector2I Size => ViewBindableAccessor.GetSize(_rtvBindable);
        public Format Format => Srv.Description.Format;
        public int MipLevels => Srv.Description.Texture2D.MipLevels;

        private readonly object _rtvBindable;

        public RtvTexture2DWrapper(object rtvTex2dBindable)
        {
            if (!rtvTex2dBindable.GetType().IsAssignableTo(ViewBindableAccessor.Type_IRtvBindable))
                throw new ArgumentException();
            _rtvBindable = rtvTex2dBindable;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
