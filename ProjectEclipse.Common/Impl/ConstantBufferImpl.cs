using ProjectEclipse.Common.Interfaces;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace ProjectEclipse.Common.Impl
{
    public readonly struct ConstantBufferImpl : IConstantBuffer
    {
        public Buffer Buffer { get; }

        public ConstantBufferImpl(Device device, int sizeInBytes, ResourceUsage usage)
        {
            Buffer = new Buffer(device, new BufferDescription
            {
                SizeInBytes = sizeInBytes,
                Usage = usage,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = usage == ResourceUsage.Dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0,
            });
        }

        public void Dispose()
        {
            Buffer.Dispose();
        }
    }
}
