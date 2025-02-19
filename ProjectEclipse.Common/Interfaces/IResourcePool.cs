using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEclipse.Common.Interfaces
{
    public interface IResourcePool
    {
        IBorrowedTexture2DSrvRtv BorrowTexture2DSrvRtv(string debugName, int width, int height, Format format);
        IBorrowedTexture2DSrvRtvUav BorrowTexture2DSrvRtvUav(string debugName, int width, int height, Format format);
    }
}
