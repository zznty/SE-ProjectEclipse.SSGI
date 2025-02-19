using SharpDX.Direct3D11;

namespace ProjectEclipse.Common.Interfaces
{
    public interface ITexture2DSrvRtv : ITexture2DSrv
    {
        RenderTargetView Rtv { get; }
    }
}
