using ProjectEclipse.Common;
using ProjectEclipse.Common.Interfaces;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using VRageMath;

namespace ProjectEclipse.Backend.Reflection.Wrappers
{
    internal readonly struct BorrowedUavTextureWrapper : IBorrowedTexture2DSrvRtvUav
    {
        public Texture2D Texture { get; }
        public ShaderResourceView Srv => BorrowedTextureAccessor.GetSrv(_borrowedUavTexture);
        public RenderTargetView Rtv => BorrowedTextureAccessor.GetRtv(_borrowedUavTexture);
        public UnorderedAccessView Uav => BorrowedTextureAccessor.GetUav(_borrowedUavTexture);
        public Vector2I Size => BorrowedTextureAccessor.GetSize(_borrowedUavTexture);
        public Format Format => BorrowedTextureAccessor.GetFormat(_borrowedUavTexture);

        private readonly object _borrowedUavTexture;

        public BorrowedUavTextureWrapper(object borrowedUavTextureInstance)
        {
            if (!borrowedUavTextureInstance.GetType().IsAssignableTo(BorrowedTextureAccessor.Type_IBorrowedUavTexture))
                throw new ArgumentException();
            _borrowedUavTexture = borrowedUavTextureInstance;
            Texture = (Texture2D)BorrowedTextureAccessor.GetResource(_borrowedUavTexture);
        }

        public void Return() => BorrowedTextureAccessor.Release(_borrowedUavTexture);
        public void Dispose() => Return();
    }
}
