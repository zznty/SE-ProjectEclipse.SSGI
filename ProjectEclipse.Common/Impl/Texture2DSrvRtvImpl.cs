using ProjectEclipse.Common.Interfaces;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRageMath;
using Device = SharpDX.Direct3D11.Device;

namespace ProjectEclipse.Common.Impl
{
    internal readonly struct Texture2DSrvRtvImpl : ITexture2DSrvRtv
    {
        public Texture2D Texture { get; }
        public ShaderResourceView Srv { get; }
        public RenderTargetView Rtv { get; }
        public Vector2I Size { get; }
        public Format Format { get; }

        public Texture2DSrvRtvImpl(Device device, Texture2DDescription textureDesc)
        {
            Texture = new Texture2D(device, textureDesc);
            Srv = new ShaderResourceView(device, Texture);
            Rtv = new RenderTargetView(device, Texture);
            Size = new Vector2I(Texture.Description.Width, Texture.Description.Height);
            Format = Texture.Description.Format;
        }

        public void Dispose()
        {
            Texture.Dispose();
            Srv.Dispose();
            Rtv.Dispose();
        }
    }
}
