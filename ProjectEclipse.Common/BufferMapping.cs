using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace ProjectEclipse.Common
{
    public struct BufferMapping : IDisposable
    {
        private readonly DeviceContext _context;
        private readonly Buffer _buffer;
        private readonly DataBox _dataBox;

        public BufferMapping(DeviceContext context, Buffer buffer, MapMode mode, MapFlags flags)
        {
            _context = context;
            _buffer = buffer;
            _dataBox = _context.MapSubresource(buffer, 0, mode, flags);
        }

        public void Write<T>(ref T data) where T : unmanaged
        {
            Utilities.Write(_dataBox.DataPointer, ref data);
        }

        public void Dispose()
        {
            _context.UnmapSubresource(_buffer, 0);
        }
    }
}
