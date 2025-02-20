using ProjectEclipse.Common;
using ProjectEclipse.Common.Interfaces;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using VRageMath;

namespace ProjectEclipse.Backend.Reflection.Wrappers
{
    public readonly struct UavTexture2DWrapper : ITexture2DSrvRtvUav
    {
        public Texture2D Texture => (Texture2D)ViewBindableAccessor.GetResource(_uavBindable);
        public ShaderResourceView Srv => ViewBindableAccessor.GetSrv(_uavBindable);
        public RenderTargetView Rtv => ViewBindableAccessor.GetRtv(_uavBindable);
        public UnorderedAccessView Uav => ViewBindableAccessor.GetUav(_uavBindable);
        public Vector2I Size => ViewBindableAccessor.GetSize(_uavBindable);
        public Format Format => Srv.Description.Format;
        public int MipLevels => Srv.Description.Texture2D.MipLevels;

        private readonly object _uavBindable;

        public UavTexture2DWrapper(object uavTex2dBindable)
        {
            if (!uavTex2dBindable.GetType().IsAssignableTo(ViewBindableAccessor.Type_IUavBindable))
                throw new ArgumentException();
            _uavBindable = uavTex2dBindable;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
