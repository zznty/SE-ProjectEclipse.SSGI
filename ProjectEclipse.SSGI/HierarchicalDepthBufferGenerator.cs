using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using ProjectEclipse.SSGI.Common;
using ProjectEclipse.SSGI.Common.Interfaces;
using VRage.Render11.Resources;
using VRageMath;
using Device = SharpDX.Direct3D11.Device;
using IConstantBuffer = ProjectEclipse.SSGI.Common.Interfaces.IConstantBuffer;

namespace ProjectEclipse.SSGI
{
    public class HierarchicalDepthBufferGenerator : IDisposable
    {
        private struct Constants
        {
            public static readonly unsafe int Size = sizeof(Constants);

            public Vector2I SourceMipResolution;
        }

        const int NUM_THREADS_XY = 16;

        public ISrvTexture HzbSrv => _hzbUav;

        private ComputeShader _cs;

        private IConstantBuffer _cbuffer;
        private IUavTexture _hzbUav;
        private Texture2DMipSrvRtvUav[] _mips;

        private bool _disposed = false;

        private readonly Device _device;
        private readonly MyBorrowedRwTextureManager _resourcePool;
        private readonly RenderUtils _renderUtils;
        private readonly IShaderCompiler _shaderCompiler;
        private readonly Vector2I _screenSize;

        public HierarchicalDepthBufferGenerator(Device device, MyBorrowedRwTextureManager resourcePool, RenderUtils renderUtils, IShaderCompiler shaderCompiler, int mipLevels, Format hzbFormat, Vector2I screenSize)
        {
            _device = device;
            _resourcePool = resourcePool;
            _renderUtils = renderUtils;
            _shaderCompiler = shaderCompiler;
            _screenSize = screenSize;

            var alignedCBSize = MathHelper.Align(Constants.Size, 16);
            _cbuffer = device.CreateConstantBuffer("ssgi_hzb_cbuffer", alignedCBSize, ResourceUsage.Dynamic);

            InitHzb(mipLevels, hzbFormat);
            ReloadShaders();
        }

        private void InitHzb(int mipLevels, Format hzbFormat)
        {
            _hzbUav = _device.CreateTexture2DSrvRtvUav("ssgi_hzb", _screenSize.X, _screenSize.Y, mipLevels, hzbFormat);

            _mips = new Texture2DMipSrvRtvUav[mipLevels];
            for (var i = 0; i < mipLevels; i++)
            {
                _mips[i] = new Texture2DMipSrvRtvUav(_hzbUav.Resource.QueryInterface<Texture2D>(), i, hzbFormat);
            }
        }

        private void UpdateCBuffer(DeviceContext rc, Vector2I sourceMipResolution)
        {
            var data = new Constants
            {
                SourceMipResolution = sourceMipResolution,
            };

            using (var mapping = rc.MapDiscard(_cbuffer))
            {
                mapping.Write(ref data);
            }
        }

        public void Generate(DeviceContext rc, ISrvTexture depthBuffer)
        {
            // copy mip 0
            _renderUtils.CopyToRT(rc, depthBuffer, _mips[0]);
            rc.OutputMerger.ResetTargets();

            rc.ComputeShader.Set(_cs);

            for (var i = 1; i < _mips.Length; i++)
            {
                var sourceMip = _mips[i - 1];
                var targetMip1 = _mips[i];

                UpdateCBuffer(rc, sourceMip.Size);

                rc.ComputeShader.SetConstantBuffer(0, _cbuffer.Buffer);
                rc.ComputeShader.SetShaderResource(0, sourceMip.Srv);
                rc.ComputeShader.SetUnorderedAccessView(0, targetMip1.Uav);

                var dispatchX = (targetMip1.Size.X - 1) / NUM_THREADS_XY + 1;
                var dispatchY = (targetMip1.Size.Y - 1) / NUM_THREADS_XY + 1;

                rc.Dispatch(dispatchX, dispatchY, 1);

                rc.ComputeShader.SetUnorderedAccessView(0, null); // causes the gpu to flush the CS writes so the texture is up-to-date when used as an srv
            }
        }

        private void DisposeShaders()
        {
            _cs?.Dispose();
            _cs = null;
        }

        public void ReloadShaders()
        {
            DisposeShaders();

            _cs = _shaderCompiler.CompileCompute(_device, "HZB/hzb.hlsl", "cs_stretch_swizzle");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                _disposed = true;
                DisposeShaders();
                _cbuffer.Dispose();
                _mips.DisposeAll();
            }
        }
    }
}
