using ProjectEclipse.SSGI.Common.Impl;
using ProjectEclipse.SSGI.Common.Interfaces;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage.Render11.Resources;
using VRageMath;
using Device = SharpDX.Direct3D11.Device;
using IConstantBuffer = ProjectEclipse.SSGI.Common.Interfaces.IConstantBuffer;

namespace ProjectEclipse.SSGI.Common
{
    public static class ResourceUtils
    {
        public static IConstantBuffer CreateConstantBuffer(this Device device, string debugName, int sizeInBytes, ResourceUsage usage)
        {
            return new ConstantBufferImpl(device, sizeInBytes, usage);
        }

        public static IBufferSrvUav CreateBufferSrvUav(this Device device, string debugName, int length, int strideInBytes, ResourceUsage usage = ResourceUsage.Default)
        {
            return new BufferSrvUavImpl(device, new BufferDescription
            {
                SizeInBytes = length * strideInBytes,
                Usage = usage,
                BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags = usage == ResourceUsage.Dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.BufferStructured,
                StructureByteStride = strideInBytes,
            });
        }

        public static IRtvTexture CreateTexture2DSrvRtv(this Device device, string debugName, Vector2I size, int mipLevels, Format format, ResourceOptionFlags options = ResourceOptionFlags.None) =>
            CreateTexture2DSrvRtv(device, debugName, size.X, size.Y, mipLevels, format, options);

        public static IRtvTexture CreateTexture2DSrvRtv(this Device device, string debugName, int width, int height, int mipLevels, Format format, ResourceOptionFlags options = ResourceOptionFlags.None)
        {
            return new Texture2DSrvRtvImpl(device, new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = mipLevels,
                ArraySize = 1,
                Format = format,
                SampleDescription = new SampleDescription
                {
                    Count = 1,
                    Quality = 0,
                },
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = options,
            });
        }

        public static IUavTexture CreateTexture2DSrvRtvUav(this Device device, string debugName, Vector2I size, int mipLevels, Format format, ResourceOptionFlags options = ResourceOptionFlags.None) =>
            CreateTexture2DSrvRtvUav(device, debugName, size.X, size.Y, mipLevels, format, options);

        public static IUavTexture CreateTexture2DSrvRtvUav(this Device device, string debugName, int width, int height, int mipLevels, Format format, ResourceOptionFlags options = ResourceOptionFlags.None)
        {
            return new Texture2DSrvRtvUavImpl(device, new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = mipLevels,
                ArraySize = 1,
                Format = format,
                SampleDescription = new SampleDescription
                {
                    Count = 1,
                    Quality = 0,
                },
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = options,
            });
        }

        public static BufferMapping MapDiscard(this DeviceContext context, IConstantBuffer cbuffer)
        {
            return new BufferMapping(context, cbuffer.Buffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
        }
    }
}
