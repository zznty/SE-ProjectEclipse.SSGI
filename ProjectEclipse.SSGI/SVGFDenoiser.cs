using ProjectEclipse.Backend.Reflection;
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
    public class SVGFDenoiser : IDisposable
    {
        struct TemporalConstants
        {
            public static readonly unsafe int Size = sizeof(TemporalConstants);

            public Vector2 ScreenSize;
            public float MaxHistoryLength;
            public float InvViewDistance;

            public Vector3 CameraDelta;
            private float _padding2;

            public Matrix ViewMatrix;
            public Matrix ProjMatrix;
            public Matrix ViewProjMatrix;
            public Matrix InvProjMatrix;
            public Matrix InvViewProjMatrix;

            public Matrix PrevViewMatrix;
            public Matrix PrevProjMatrix;
            public Matrix PrevViewProjMatrix;
            public Matrix PrevInvProjMatrix;
            public Matrix PrevInvViewProjMatrix;
        }

        struct FilterMomentsConstants
        {
            public static readonly unsafe int Size = sizeof(FilterMomentsConstants);

            public Vector2I ScreenSize;
            public float InvViewDistance;
            private uint _padding1;

            public Matrix ProjMatrix;
        }

        struct AtrousConstants
        {
            public static readonly unsafe int Size = sizeof(AtrousConstants);

            public Vector2I ScreenSize;
            public int StepSize;
            public float InvViewDistance;

            public Matrix ProjMatrix;
        }

        public float TemporalBlendWeight { get; set; }

        private PixelShader _psTemporal;
        private PixelShader _psFilterMoments;
        private PixelShader _psAtrous;

        private IConstantBuffer _cbuffer;
        private ITexture2DSrvRtv _temporalHistory;
        private ITexture2DSrvRtv _buffer1; // x = history length, y = first moment, z = second moment, w = depth derivative (max(abs(ddx), abs(ddy)))

        private Vector3D _prevCameraPos = Vector3D.Zero;
        private Matrix _prevViewMatrix = Matrix.Identity;
        private Matrix _prevProjMatrix = Matrix.Identity;
        private Matrix _prevViewProjMatrix = Matrix.Identity;
        private Matrix _prevInvProjMatrix = Matrix.Identity;
        private Matrix _prevInvViewProjMatrix = Matrix.Identity;

        private bool _disposed = false;

        private readonly Device _device;
        private readonly IResourcePool _resourcePool;
        private readonly RenderUtils _renderUtils;
        private readonly IShaderCompiler _shaderCompiler;
        private readonly SamplerStates _samplerStates;
        private readonly Vector2I _screenSize;

        public SVGFDenoiser(Device device, IResourcePool resourcePool, RenderUtils renderUtils, IShaderCompiler shaderCompiler, SamplerStates samplerStates, Vector2I screenSize)
        {
            _device = device;
            _resourcePool = resourcePool;
            _renderUtils = renderUtils;
            _shaderCompiler = shaderCompiler;
            _samplerStates = samplerStates;
            _screenSize = screenSize;

            int alignedCBSize = MathHelper.Align(Math.Max(TemporalConstants.Size, Math.Max(FilterMomentsConstants.Size, AtrousConstants.Size)), 16);
            _cbuffer = device.CreateConstantBuffer("svgf_cbuffer", alignedCBSize, ResourceUsage.Dynamic);
            _temporalHistory = device.CreateTexture2DSrvRtv("svgf_history", screenSize, 1, Format.R16G16B16A16_Float);
            _buffer1 = device.CreateTexture2DSrvRtv("svgf_buffer1", screenSize, 1, Format.R16G16B16A16_Float);

            ReloadShaders();
        }

        private void UpdateTemporalCB(DeviceContext rc)
        {
            var envMatrices = MyRender11Accessor.GetEnvironmentMatrices();

            var data = new TemporalConstants
            {
                ScreenSize = _screenSize,
                MaxHistoryLength = 1.0f / (1.0f - MathHelper.Clamp(TemporalBlendWeight, 0.0001f, 1.0f)),
                InvViewDistance = 1.0f / envMatrices.FarClipping,

                CameraDelta = envMatrices.CameraPosition - _prevCameraPos,

                ViewMatrix = Matrix.Transpose(envMatrices.ViewAt0),
                ProjMatrix = Matrix.Transpose(envMatrices.Projection),
                ViewProjMatrix = Matrix.Transpose(envMatrices.ViewProjectionAt0),
                InvProjMatrix = Matrix.Transpose(envMatrices.InvProjection),
                InvViewProjMatrix = Matrix.Transpose(envMatrices.InvViewProjectionAt0),

                PrevViewMatrix = Matrix.Transpose(_prevViewMatrix),
                PrevProjMatrix = Matrix.Transpose(_prevProjMatrix),
                PrevViewProjMatrix = Matrix.Transpose(_prevViewProjMatrix),
                PrevInvProjMatrix = Matrix.Transpose(_prevInvProjMatrix),
                PrevInvViewProjMatrix = Matrix.Transpose(_prevInvViewProjMatrix),
            };

            using (var mapping = rc.MapDiscard(_cbuffer))
            {
                mapping.Write(ref data);
            }
        }

        private void UpdateFilterMomentsCB(DeviceContext rc)
        {
            var envMatrices = MyRender11Accessor.GetEnvironmentMatrices();

            var data = new FilterMomentsConstants
            {
                ScreenSize = _screenSize,
                InvViewDistance = 1.0f / envMatrices.FarClipping,
                ProjMatrix = Matrix.Transpose(envMatrices.Projection),
            };

            using (var mapping = rc.MapDiscard(_cbuffer))
            {
                mapping.Write(ref data);
            }
        }

        private void UpdateAtrousCB(DeviceContext rc, int stepSize)
        {
            var envMatrices = MyRender11Accessor.GetEnvironmentMatrices();

            var data = new AtrousConstants
            {
                ScreenSize = _screenSize,
                StepSize = stepSize,
                InvViewDistance = 1.0f / envMatrices.FarClipping,
                ProjMatrix = Matrix.Transpose(envMatrices.Projection),
            };

            using (var mapping = rc.MapDiscard(_cbuffer))
            {
                mapping.Write(ref data);
            }
        }

        private void TemporalPass(DeviceContext rc, ITexture2DSrv currentFrame, ITexture2DSrvRtv outputRtv, ITexture2DSrv depthBuffer, ITexture2DSrv reflectionDepthBuffer, ITexture2DSrv prevDepthBuffer, ITexture2DSrv gbuffer1)
        {
            var newBuffer1 = _resourcePool.BorrowTexture2DSrvRtv("svgf_buffer1_temp", _buffer1.Size, _buffer1.Format);

            UpdateTemporalCB(rc);

            rc.PixelShader.Set(_psTemporal);
            rc.PixelShader.SetSamplers(1, _samplerStates.Point, _samplerStates.Linear);
            rc.PixelShader.SetConstantBuffer(0, _cbuffer.Buffer);
            rc.PixelShader.SetShaderResources(0, currentFrame.Srv, _temporalHistory.Srv, depthBuffer.Srv, prevDepthBuffer.Srv, _buffer1.Srv, reflectionDepthBuffer?.Srv, gbuffer1.Srv);
            rc.OutputMerger.SetTargets(outputRtv.Rtv, newBuffer1.Rtv);

            _renderUtils.DrawFullscreenPass(rc, _screenSize);
            rc.OutputMerger.SetTargets();

            _renderUtils.CopyToRT(rc, newBuffer1, _buffer1);
            rc.OutputMerger.SetTargets();

            newBuffer1.Return();
        }

        private void FilterMomentsPass(DeviceContext rc, ITexture2DSrv input, ITexture2DSrvRtv output, ITexture2DSrv depthBuffer, ITexture2DSrv gbuffer1)
        {
            UpdateFilterMomentsCB(rc);

            rc.PixelShader.Set(_psFilterMoments);
            rc.PixelShader.SetConstantBuffer(0, _cbuffer.Buffer);
            rc.PixelShader.SetShaderResources(0, input.Srv, _buffer1.Srv, depthBuffer.Srv, gbuffer1.Srv);
            rc.OutputMerger.SetTargets(output.Rtv);

            _renderUtils.DrawFullscreenPass(rc, _screenSize);
            rc.OutputMerger.SetTargets();
        }

        private void AtrousPass(DeviceContext rc, ITexture2DSrv temporallyDenoisedFrame, ITexture2DSrv depthBuffer, ITexture2DSrv gbuffer1, ITexture2DSrvRtv outputRtv, int iterations)
        {
            if (iterations == 0)
            {
                _renderUtils.CopyToRT(rc, temporallyDenoisedFrame, outputRtv);
                rc.OutputMerger.SetTargets();

                _renderUtils.CopyToRT(rc, temporallyDenoisedFrame, _temporalHistory);
                rc.OutputMerger.SetTargets();
            }
            else
            {
                var tempRtv = _resourcePool.BorrowTexture2DSrvRtv("svgf_atrous_temp", _screenSize, Format.R16G16B16A16_Float);
                var tempRtv2 = _resourcePool.BorrowTexture2DSrvRtv("svgf_atrous_temp2", _screenSize, Format.R16G16B16A16_Float);

                rc.PixelShader.Set(_psAtrous);
                rc.PixelShader.SetSamplers(1, _samplerStates.Point, _samplerStates.Linear);
                rc.PixelShader.SetConstantBuffer(0, _cbuffer.Buffer);
                rc.PixelShader.SetShaderResources(1, depthBuffer.Srv, gbuffer1.Srv, _buffer1.Srv);

                var input = tempRtv;
                var output = tempRtv2;

                for (int i = 0; i < iterations; i++)
                {
                    (input, output) = (output, input);

                    UpdateAtrousCB(rc, 1 << i);

                    // https://x.com/NateMorrical/status/1180302300549500928
                    //UpdateAtrousCB(rc, 1 << (iterations - i - 1));

                    rc.PixelShader.SetShaderResource(0, i == 0 ? temporallyDenoisedFrame.Srv : input.Srv);
                    rc.OutputMerger.SetTargets(output.Rtv);

                    _renderUtils.DrawFullscreenPass(rc, _screenSize);
                    rc.OutputMerger.SetTargets();
                    rc.PixelShader.SetShaderResource(0, null);
                }

                _renderUtils.CopyToRT(rc, output, outputRtv);
                rc.OutputMerger.SetTargets();

                _renderUtils.CopyToRT(rc, output, _temporalHistory);
                rc.OutputMerger.SetTargets();

                tempRtv.Return();
                tempRtv2.Return();
            }
        }

        public void Run(DeviceContext rc, ITexture2DSrvRtv inputOutput, ITexture2DSrv depthBuffer, ITexture2DSrv reflectionDepthBuffer, ITexture2DSrv prevDepthBuffer, ITexture2DSrv gbuffer1, int iterations)
        {
            var tempRtv = _resourcePool.BorrowTexture2DSrvRtv("svgf_temp1", _screenSize, _temporalHistory.Format);
            TemporalPass(rc, inputOutput, tempRtv, depthBuffer, reflectionDepthBuffer, prevDepthBuffer, gbuffer1);

            var tempRtv2 = _resourcePool.BorrowTexture2DSrvRtv("svgf_temp2", _screenSize, _temporalHistory.Format);
            FilterMomentsPass(rc, tempRtv, tempRtv2, depthBuffer, gbuffer1);

            AtrousPass(rc, tempRtv2, depthBuffer, gbuffer1, inputOutput, iterations);

            tempRtv.Return();
            tempRtv2.Return();
            
            var envMatrices = MyRender11Accessor.GetEnvironmentMatrices();
            _prevCameraPos = envMatrices.CameraPosition;
            _prevViewMatrix = envMatrices.ViewAt0;
            _prevProjMatrix = envMatrices.Projection;
            _prevViewProjMatrix = envMatrices.ViewProjectionAt0;
            _prevInvProjMatrix = envMatrices.InvProjection;
            _prevInvViewProjMatrix = envMatrices.InvViewProjectionAt0;
        }

        private void DisposeShaders()
        {
            _psTemporal?.Dispose();
            _psFilterMoments?.Dispose();
            _psAtrous?.Dispose();

            _psTemporal = null;
            _psFilterMoments = null;
            _psAtrous = null;
        }

        public void ReloadShaders()
        {
            DisposeShaders();

            _psTemporal      = _shaderCompiler.CompilePixel(_device, "SVGF/temporal.hlsl", "main");
            _psFilterMoments = _shaderCompiler.CompilePixel(_device, "SVGF/filter_moments.hlsl", "main");
            _psAtrous        = _shaderCompiler.CompilePixel(_device, "SVGF/atrous.hlsl", "main");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                _disposed = true;
                DisposeShaders();
                _cbuffer.Dispose();
                _temporalHistory.Dispose();
                _buffer1.Dispose();
            }
        }
    }
}
