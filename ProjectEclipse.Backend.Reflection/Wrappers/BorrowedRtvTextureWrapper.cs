using ProjectEclipse.Common;
using ProjectEclipse.Common.Interfaces;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using VRageMath;

namespace ProjectEclipse.Backend.Reflection.Wrappers
{
    internal readonly struct BorrowedRtvTextureWrapper : IBorrowedTexture2DSrvRtv
    {
        public Texture2D Texture { get; }
        public ShaderResourceView Srv => BorrowedTextureAccessor.GetSrv(_borrowedRtvTexture);
        public RenderTargetView Rtv => BorrowedTextureAccessor.GetRtv(_borrowedRtvTexture);
        public Vector2I Size => BorrowedTextureAccessor.GetSize(_borrowedRtvTexture);
        public Format Format => BorrowedTextureAccessor.GetFormat(_borrowedRtvTexture);

        private readonly object _borrowedRtvTexture;

        public BorrowedRtvTextureWrapper(object borrowedRtvTextureInstance)
        {
            if (!borrowedRtvTextureInstance.GetType().IsAssignableTo(BorrowedTextureAccessor.Type_IBorrowedRtvTexture))
                throw new ArgumentException();
            _borrowedRtvTexture = borrowedRtvTextureInstance;
            Texture = (Texture2D)BorrowedTextureAccessor.GetResource(_borrowedRtvTexture);
        }

        public void Return() => BorrowedTextureAccessor.Release(_borrowedRtvTexture);
        public void Dispose() => Return();
    }
}
