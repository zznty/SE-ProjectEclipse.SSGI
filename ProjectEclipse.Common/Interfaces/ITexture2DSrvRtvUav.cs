using SharpDX.Direct3D11;

namespace ProjectEclipse.Common.Interfaces
{
    public interface ITexture2DSrvRtvUav : ITexture2DSrvRtv
    {
        UnorderedAccessView Uav { get; }
    }
}
