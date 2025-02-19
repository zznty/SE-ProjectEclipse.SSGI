using ProjectEclipse.Common;
using ProjectEclipse.Common.Interfaces;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using Device = SharpDX.Direct3D11.Device;

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

        public ITexture2DSrv HzbSrv => _hzbUav;

        private ComputeShader _cs;

        private IConstantBuffer _cbuffer;
        private ITexture2DSrvRtvUav _hzbUav;
        private Texture2DMipSrvRtvUav[] _mips;

        private bool _disposed = false;

        private readonly Device _device;
        private readonly IResourcePool _resourcePool;
        private readonly RenderUtils _renderUtils;
        private readonly IShaderCompiler _shaderCompiler;
        private readonly Vector2I _screenSize;

        public HierarchicalDepthBufferGenerator(Device device, IResourcePool resourcePool, RenderUtils renderUtils, IShaderCompiler shaderCompiler, int mipLevels, Format hzbFormat, Vector2I screenSize)
        {
            _device = device;
            _resourcePool = resourcePool;
            _renderUtils = renderUtils;
            _shaderCompiler = shaderCompiler;
            _screenSize = screenSize;

            int alignedCBSize = MathHelper.Align(Constants.Size, 16);
            _cbuffer = device.CreateConstantBuffer("ssgi_hzb_cbuffer", alignedCBSize, ResourceUsage.Dynamic);

            InitHzb(mipLevels, hzbFormat);
            ReloadShaders();
        }

        private void InitHzb(int mipLevels, Format hzbFormat)
        {
            _hzbUav = _device.CreateTexture2DSrvRtvUav("ssgi_hzb", _screenSize.X, _screenSize.Y, mipLevels, hzbFormat);

            _mips = new Texture2DMipSrvRtvUav[mipLevels];
            for (int i = 0; i < mipLevels; i++)
            {
                _mips[i] = new Texture2DMipSrvRtvUav(_hzbUav.Texture, i, hzbFormat);
            }
        }

        private void UpdateCBuffer(DeviceContext rc, Vector2I sourceMipResolution)
        {
            Constants data = new Constants
            {
                SourceMipResolution = sourceMipResolution,
            };

            using (var mapping = rc.MapDiscard(_cbuffer))
            {
                mapping.Write(ref data);
            }
        }

        public void Generate(DeviceContext rc, ITexture2DSrv depthBuffer)
        {
            // copy mip 0
            _renderUtils.CopyToRT(rc, depthBuffer, _mips[0]);
            rc.OutputMerger.ResetTargets();

            rc.ComputeShader.Set(_cs);

            for (int i = 1; i < _mips.Length; i++)
            {
                var sourceMip = _mips[i - 1];
                var targetMip1 = _mips[i];

                UpdateCBuffer(rc, sourceMip.Size);

                rc.ComputeShader.SetConstantBuffer(0, _cbuffer.Buffer);
                rc.ComputeShader.SetShaderResource(0, sourceMip.Srv);
                rc.ComputeShader.SetUnorderedAccessView(0, targetMip1.Uav);

                int dispatchX = (targetMip1.Size.X - 1) / NUM_THREADS_XY + 1;
                int dispatchY = (targetMip1.Size.Y - 1) / NUM_THREADS_XY + 1;

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
                _hzbUav.Dispose();
                _mips.DisposeAll();
            }
        }
    }
}
