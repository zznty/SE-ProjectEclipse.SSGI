using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEclipse.Common.Interfaces
{
    public interface IBufferSrvUav : IBufferSrv, IDisposable
    {
        UnorderedAccessView Uav { get; }
    }
}
