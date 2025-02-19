using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace ProjectEclipse.Common.Interfaces
{
    public interface IConstantBuffer : IDisposable
    {
        Buffer Buffer { get; }
    }
}
