using System;
using SharpDX.Direct3D11;

namespace ProjectEclipse.SSGI.Common
{
    public class SamplerStates : IDisposable
    {
        public SamplerState Default { get; }
        public SamplerState Point { get; }
        public SamplerState Linear { get; }

        public SamplerStates(Device device)
        {
            Default = new SamplerState(device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                MaximumLod = float.MaxValue,
            });

            Point = new SamplerState(device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                BorderColor = new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 0),
                MaximumLod = float.MaxValue,
            });

            Linear = new SamplerState(device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                BorderColor = new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 0),
                MaximumLod = float.MaxValue,
            });
        }

        public void Dispose()
        {
            Default.Dispose();
            Point.Dispose();
            Linear.Dispose();
        }
    }
}
