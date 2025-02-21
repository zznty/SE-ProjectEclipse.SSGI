using System;
using ProjectEclipse.SSGI.Common.Interfaces;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage.Render11.Resources;
using VRageMath;
using Resource = SharpDX.Direct3D11.Resource;

namespace ProjectEclipse.SSGI
{
    public class Texture2DMipSrvRtvUav : IUavTexture, IDisposable
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
        public int MipLevels => 1;
        public event Action<ITexture>? OnFormatChanged;

        public Texture2DMipSrvRtvUav(Texture2D texture, int mip, Format format)
        {
            if (texture.Dimension != ResourceDimension.Texture2D)
            {
                throw new ArgumentException($"{nameof(texture)} format must be {ResourceDimension.Texture2D}");
            }

            Texture = texture;
            Size = new Vector2I(Resource.CalculateMipSize(mip, texture.Description.Width), Resource.CalculateMipSize(mip, texture.Description.Height));
            Format = Texture.Description.Format;

            Srv = new ShaderResourceView(Texture.Device, Texture, new ShaderResourceViewDescription
            {
                Format = format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource
                {
                    MostDetailedMip = mip,
                    MipLevels = 1,
                },
            });

            Rtv = new RenderTargetView(Texture.Device, Texture, new RenderTargetViewDescription
            {
                Format = format,
                Dimension = RenderTargetViewDimension.Texture2D,
                Texture2D = new RenderTargetViewDescription.Texture2DResource
                {
                    MipSlice = mip,
                },
            });

            Uav = new UnorderedAccessView(Texture.Device, Texture, new UnorderedAccessViewDescription
            {
                Format = format,
                Dimension = UnorderedAccessViewDimension.Texture2D,
                Texture2D = new UnorderedAccessViewDescription.Texture2DResource
                {
                    MipSlice = mip,
                },
            });
        }

        public void Dispose()
        {
            Srv.Dispose();
            Rtv.Dispose();
            Uav.Dispose();
        }
    }
}
