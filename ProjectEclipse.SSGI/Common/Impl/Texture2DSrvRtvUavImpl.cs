using System;
using ProjectEclipse.SSGI.Common.Interfaces;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage.Render11.Resources;
using VRageMath;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;

namespace ProjectEclipse.SSGI.Common.Impl
{
    internal struct Texture2DSrvRtvUavImpl : IUavTexture, IDisposable
    {
        public Texture2D Texture { get; }
        public ShaderResourceView Srv { get; }
        public RenderTargetView Rtv { get; }
        public UnorderedAccessView Uav { get; }
        public string Name { get; }
        public Resource Resource => Texture;
        public Vector3I Size3 => new(Size, 0);
        public Vector2I Size { get; }
        public Format Format { get; }
        public int MipLevels { get; }
        public event Action<ITexture>? OnFormatChanged;

        public Texture2DSrvRtvUavImpl(Device device, Texture2DDescription textureDesc)
        {
            Texture = new Texture2D(device, textureDesc);
            Srv = new ShaderResourceView(device, Texture);
            Rtv = new RenderTargetView(device, Texture);
            Uav = new UnorderedAccessView(device, Texture);
            Size = new Vector2I(textureDesc.Width, textureDesc.Height);
            Format = textureDesc.Format;
            MipLevels = textureDesc.MipLevels;
        }

        public void Dispose()
        {
            Texture.Dispose();
            Srv.Dispose();
            Rtv.Dispose();
            Uav.Dispose();
        }
    }
}
