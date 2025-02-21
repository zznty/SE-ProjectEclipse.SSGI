﻿using ProjectEclipse.SSGI.Config;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Runtime.InteropServices;
using ProjectEclipse.SSGI.Common;
using ProjectEclipse.SSGI.Common.Interfaces;
using VRage.Render11.Resources;
using VRage.Render11.Resources.Textures;
using VRageMath;
using VRageRender;
using Device = SharpDX.Direct3D11.Device;
using IConstantBuffer = ProjectEclipse.SSGI.Common.Interfaces.IConstantBuffer;

namespace ProjectEclipse.SSGI
{
    public class SSGIRenderPass : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct PackedReservoir
        {
            public static readonly unsafe int Size = sizeof(PackedReservoir);

            Vector3 CreatedPos;
            uint CreatedNormalOctHalf; // 2x float16 oct-encoded normal
            Vector3 LightPos;
            uint LightNormalOctHalf;
            Vector2I LightRadiancePacked;
            uint M_Age;
            float AvgWeight;
        }

        struct Constants
        {
            public static readonly unsafe int Size = sizeof(Constants);

            public uint FrameIndex;
            public Vector2I ScreenSize;
            public uint RandomSeed;

            public Vector3 CameraDelta;
            private uint _padding1;

            public uint MaxTraceIterations;
            public uint RaysPerPixel;
            public float IndirectLightMulti;
            private uint _padding3;

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

        struct Constants2
        {
            public static readonly unsafe int Size = sizeof(Constants2);

            public Vector2 GaussianDir;
            public float MipLevel;
        }

        // shaders
        private ComputeShader _csRestirInitial;
        private ComputeShader _csRestirSpatioTemporal;
        private ComputeShader _csRestirShading;
        private ComputeShader _csRestirComposite;
        private PixelShader _psBlur;

        // resources
        private IConstantBuffer _cbuffer;
        private IRtvTexture _prevDepthBuffer;
        private IBufferSrvUav _prevReservoirs;
        private IBufferSrvUav _candidateReservoirs;
        private IBufferSrvUav _temporalReservoirs;
        private IBufferSrvUav _spatialReservoirs;
        private IRtvTexture _filteredFrame;

        // dependencies
        private HierarchicalDepthBufferGenerator _hzbGenerator;
        private SVGFDenoiser _diffuseDenoiser;
        private SVGFDenoiser _specularDenoiser;
        private Random _rand;

        private Vector3D _prevCameraPos = Vector3D.Zero;
        private Matrix _prevViewMatrix = Matrix.Identity;
        private Matrix _prevProjMatrix = Matrix.Identity;
        private Matrix _prevViewProjMatrix = Matrix.Identity;
        private Matrix _prevInvProjMatrix = Matrix.Identity;
        private Matrix _prevInvViewProjMatrix = Matrix.Identity;

        private bool _disposed = false;

        private readonly Device _device;
        private readonly SSGIConfig _config;
        private readonly MyBorrowedRwTextureManager _resourcePool;
        private readonly IShaderCompiler _shaderCompiler;
        private readonly RenderUtils _renderUtils;
        private readonly SamplerStates _samplerStates;
        private readonly Vector2I _screenSize;

        public SSGIRenderPass(Device device, SSGIConfig config, MyBorrowedRwTextureManager resourcePool, IShaderCompiler shaderCompiler, RenderUtils renderUtils, SamplerStates samplerStates, Vector2I screenSize)
        {
            _device = device;
            _config = config;
            _resourcePool = resourcePool;
            _shaderCompiler = shaderCompiler;
            _renderUtils = renderUtils;
            _samplerStates = samplerStates;
            _screenSize = screenSize;
            
            var alignedCBSize = MathHelper.Align(Constants.Size, 16);
            _cbuffer = device.CreateConstantBuffer("ssgi_cbuffer", alignedCBSize, ResourceUsage.Dynamic);
            _prevDepthBuffer = device.CreateTexture2DSrvRtv("ssgi_depth_prev", screenSize, 1, Format.R32_Float);
            _prevReservoirs      = device.CreateBufferSrvUav("ssgi_restir_reservoirs_prev",      screenSize.X * screenSize.Y, PackedReservoir.Size);
            _candidateReservoirs = device.CreateBufferSrvUav("ssgi_restir_reservoirs_candidate", screenSize.X * screenSize.Y, PackedReservoir.Size);
            _temporalReservoirs  = device.CreateBufferSrvUav("ssgi_restir_reservoirs_temporal",  screenSize.X * screenSize.Y, PackedReservoir.Size);
            _spatialReservoirs   = device.CreateBufferSrvUav("ssgi_restir_reservoirs_spatial",   screenSize.X * screenSize.Y, PackedReservoir.Size);
            _filteredFrame = device.CreateTexture2DSrvRtv("ssgi_filtered_input", screenSize, 7, Format.R16G16B16A16_Float, ResourceOptionFlags.GenerateMipMaps);

            _hzbGenerator = new HierarchicalDepthBufferGenerator(device, resourcePool, _renderUtils, shaderCompiler, 9, Format.R32_Float, screenSize);
            _diffuseDenoiser = new SVGFDenoiser(device, resourcePool, _renderUtils, shaderCompiler, _samplerStates, screenSize);
            _specularDenoiser = new SVGFDenoiser(device, resourcePool, _renderUtils, shaderCompiler, _samplerStates, screenSize);
            _rand = new Random();

            ReloadShaders();
        }

        public static uint NextUint(Random rand)
        {
            return (uint)rand.Next(1 << 16) << 16 | (uint)rand.Next(1 << 16);
        }

        private void UpdateCB(DeviceContext rc, MyEnvironmentMatrices envMatrices, uint frameIndex)
        {
            var constants = new Constants
            {
                FrameIndex = frameIndex,
                ScreenSize = _screenSize,
                RandomSeed = NextUint(_rand),

                CameraDelta = envMatrices.CameraPosition - _prevCameraPos,

                MaxTraceIterations = (uint)Math.Max(1, _config.Data.MaxTraceIterations),
                RaysPerPixel = (uint)Math.Max(1, _config.Data.RaysPerPixel),
                IndirectLightMulti = Math.Max(0, _config.Data.IndirectLightMulti),

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
                mapping.Write(ref constants);
            }
        }

        private void UpdateCB2(DeviceContext rc, Vector2 gaussianDir, float mipLevel)
        {
            var constants = new Constants2
            {
                GaussianDir = gaussianDir,
                MipLevel = mipLevel,
            };

            using (var mapping = rc.MapDiscard(_cbuffer))
            {
                mapping.Write(ref constants);
            }
        }

        private void GenerateFrameMipMaps(DeviceContext rc, ISrvTexture frameBuffer)
        {
            if (_filteredFrame.MipLevels == 1)
            {
                _renderUtils.CopyToRT(rc, frameBuffer, _filteredFrame);
            }
            else if (_config.Data.UseGaussianPrefiltering)
            {
                var dir = new Vector2[_filteredFrame.MipLevels];
                dir[0] = (Vector2)frameBuffer.Size;
                for (var i = 1; i < dir.Length; i++)
                {
                    dir[i] = dir[i - 1] / 2f;
                }

                var mipTex0 = _resourcePool.BorrowRtv("ssgi_mip_0_temp", _screenSize.X, _screenSize.Y, Format.R16G16B16A16_Float);
                var mipTex1 = _resourcePool.BorrowRtv("ssgi_mip_1_temp", _screenSize.X, _screenSize.Y, Format.R16G16B16A16_Float);

                for (var i = 0; i < dir.Length; i++)
                {
                    rc.PixelShader.Set(_psBlur);
                    rc.PixelShader.SetSampler(1, _samplerStates.Point);
                    rc.PixelShader.SetSampler(2, _samplerStates.Linear);

                    UpdateCB2(rc, new Vector2(1f / dir[i].X, 0), i);
                    rc.PixelShader.SetConstantBuffer(0, _cbuffer.Buffer);

                    rc.OutputMerger.SetRenderTargets(mipTex1.Rtv);
                    rc.PixelShader.SetShaderResource(0, (i == 0 ? frameBuffer : mipTex0).Srv);
                    _renderUtils.DrawFullscreenPass(rc, mipTex1.Size);

                    UpdateCB2(rc, new Vector2(0, 1f / dir[i].Y), i);
                    rc.OutputMerger.SetRenderTargets(mipTex0.Rtv);
                    rc.PixelShader.SetShaderResource(0, mipTex1.Srv);
                    _renderUtils.DrawFullscreenPass(rc, mipTex0.Size);

                    // don't filter mip 0
                    if (i == 0)
                    {
                        _renderUtils.CopyToRT(rc, frameBuffer, _filteredFrame);
                    }
                    else
                    {
                        using (var outputMip = new Texture2DMipSrvRtv(_filteredFrame.Rtv.Resource.QueryInterface<Texture2D>(), i, _filteredFrame.Format))
                        {
                            _renderUtils.CopyToRT(rc, mipTex0, outputMip);
                        }
                    }
                }

                rc.OutputMerger.SetTargets(); // flush mip render commands

                mipTex0.Release();
                mipTex1.Release();
            }
            else
            {
                _renderUtils.CopyToRT(rc, frameBuffer, _filteredFrame);
                rc.OutputMerger.SetTargets();
                rc.GenerateMips(_filteredFrame.Srv);
                rc.OutputMerger.SetTargets();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rc"></param>
        /// <param name="frameBuffer"></param>
        /// <param name="depthBuffer">Raw depth buffer srv.</param>
        /// <param name="gbuffer0"></param>
        /// <param name="gbuffer1"></param>
        /// <param name="gbuffer2"></param>
        /// <param name="renderTarget"></param>
        public void Draw(DeviceContext rc, ISrvTexture frameBuffer, ISrvTexture depthBuffer, ISrvTexture gbuffer0, ISrvTexture gbuffer1, ISrvTexture gbuffer2, IUavTexture renderTarget)
        {
            var envMatrices = MyRender11.Environment.Matrices;
            var frameIndex = MyRender11.GameplayFrameCounter;

            var inputFrame = frameBuffer;
            if (_config.Data.EnableInputPrefiltering)
            {
                GenerateFrameMipMaps(rc, frameBuffer);
                inputFrame = _filteredFrame;
            }

            _hzbGenerator.Generate(rc, depthBuffer);

            var diffuseIrradiance = _resourcePool.BorrowUav("ssgi_irradiance_diffuse", _screenSize.X, _screenSize.Y, Format.R16G16B16A16_Float);
            var specularIrradiance = _resourcePool.BorrowUav("ssgi_irradiance_specular", _screenSize.X, _screenSize.Y, Format.R16G16B16A16_Float);
            var rayExtendedDepthBuffer = _resourcePool.BorrowUav("ssgi_ray_extended_depth", _screenSize.X, _screenSize.Y, Format.R32_Float);

            UpdateCB(rc, envMatrices, (uint)frameIndex);

            rc.ComputeShader.SetSamplers(1, _samplerStates.Point, _samplerStates.Linear);
            rc.ComputeShader.SetConstantBuffer(1, _cbuffer.Buffer);
            rc.ComputeShader.SetShaderResources(0, depthBuffer.Srv, gbuffer0.Srv, gbuffer1.Srv, gbuffer2.Srv, _hzbGenerator.HzbSrv.Srv, inputFrame.Srv);
            rc.ComputeShader.SetUnorderedAccessViews(0, diffuseIrradiance.Uav, specularIrradiance.Uav, _prevReservoirs.Uav, _candidateReservoirs.Uav, _temporalReservoirs.Uav, _spatialReservoirs.Uav, rayExtendedDepthBuffer.Uav);

            InitialPass();
            ResamplingPass();
            ShadingPass();

            rc.ComputeShader.SetUnorderedAccessViews(0, null, null, null, null, null, null, null);

            if (_config.Data.Svgf_Enabled)
            {
                _diffuseDenoiser.TemporalBlendWeight = _config.Data.Svgf_DiffuseTemporalWeight;
                _specularDenoiser.TemporalBlendWeight = _config.Data.Svgf_SpecularTemporalWeight;

                _diffuseDenoiser.Run(rc, diffuseIrradiance, depthBuffer, null, _prevDepthBuffer, gbuffer1, _config.Data.Svgf_DiffuseAtrousIterations);
                _specularDenoiser.Run(rc, specularIrradiance, depthBuffer, rayExtendedDepthBuffer, _prevDepthBuffer, gbuffer1, _config.Data.Svgf_SpecularAtrousIterations);
            }

            CompositePass();

            _renderUtils.CopyToRT(rc, depthBuffer, _prevDepthBuffer);
            rc.OutputMerger.SetTargets();

            diffuseIrradiance.Release();
            specularIrradiance.Release();
            rayExtendedDepthBuffer.Release();

            _prevCameraPos = envMatrices.CameraPosition;
            _prevViewMatrix = envMatrices.ViewAt0;
            _prevProjMatrix = envMatrices.Projection;
            _prevViewProjMatrix = envMatrices.ViewProjectionAt0;
            _prevInvProjMatrix = envMatrices.InvProjection;
            _prevInvViewProjMatrix = envMatrices.InvViewProjectionAt0;

            void InitialPass()
            {
                rc.ComputeShader.Set(_csRestirInitial);
                rc.Dispatch(_screenSize.X / 8, _screenSize.Y / 8, 1);
            }
            void ResamplingPass()
            {
                rc.ComputeShader.Set(_csRestirSpatioTemporal);
                rc.Dispatch(_screenSize.X / 8, _screenSize.Y / 8, 1);
            }
            void ShadingPass()
            {
                rc.ComputeShader.Set(_csRestirShading);
                rc.Dispatch(_screenSize.X / 8, _screenSize.Y / 8, 1);
            }
            void CompositePass()
            {
                // maybe use a pixel shader instead?
                rc.ComputeShader.Set(_csRestirComposite);
                rc.ComputeShader.SetShaderResources(6, diffuseIrradiance.Srv, specularIrradiance.Srv);
                rc.ComputeShader.SetUnorderedAccessView(0, renderTarget.Uav);

                rc.Dispatch(_screenSize.X / 8, _screenSize.Y / 8, 1);

                rc.ComputeShader.SetUnorderedAccessView(0, null);
            }
        }

        private void DisposeShaders()
        {
            _csRestirInitial?.Dispose();
            _csRestirSpatioTemporal?.Dispose();
            _csRestirShading?.Dispose();
            _csRestirComposite?.Dispose();
            _psBlur?.Dispose();

            _csRestirInitial = null;
            _csRestirSpatioTemporal = null;
            _csRestirShading = null;
            _csRestirComposite = null;
            _psBlur = null;
        }

        public void ReloadShaders()
        {
            DisposeShaders();

            _csRestirInitial        = _shaderCompiler.CompileCompute(_device, "SSR/restir_initial.hlsl", "cs");
            _csRestirSpatioTemporal = _shaderCompiler.CompileCompute(_device, "SSR/restir_resampling.hlsl", "cs");
            _csRestirShading        = _shaderCompiler.CompileCompute(_device, "SSR/restir_shading.hlsl", "cs");
            _csRestirComposite      = _shaderCompiler.CompileCompute(_device, "SSR/restir_composite.hlsl", "cs");
            _psBlur = _shaderCompiler.CompilePixel(_device, "SSR/blur.hlsl", "MipBlur");

            _hzbGenerator.ReloadShaders();
            _diffuseDenoiser.ReloadShaders();
            _specularDenoiser.ReloadShaders();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                DisposeShaders();

                _cbuffer.Dispose();
                _prevReservoirs.Dispose();
                _candidateReservoirs.Dispose();
                _temporalReservoirs.Dispose();
                _spatialReservoirs.Dispose();

                _hzbGenerator.Dispose();
                _diffuseDenoiser.Dispose();
                _specularDenoiser.Dispose();
            }
        }
    }
}
