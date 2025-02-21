using System;
using ProjectEclipse.SSGI.Common.Interfaces;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using VRage.Render11.Resources;
using VRageMath;

namespace ProjectEclipse.SSGI.Common
{
    public class RenderUtils : IDisposable
    {
        public struct MyViewport
        {
            public float OffsetX;
            public float OffsetY;
            public float Width;
            public float Height;

            public MyViewport(float width, float height)
            {
                OffsetX = 0f;
                OffsetY = 0f;
                Width = width;
                Height = height;
            }

            public MyViewport(Vector2I resolution)
            {
                OffsetX = 0f;
                OffsetY = 0f;
                Width = resolution.X;
                Height = resolution.Y;
            }

            public MyViewport(float x, float y, float width, float height)
            {
                OffsetX = x;
                OffsetY = y;
                Width = width;
                Height = height;
            }

            public RawViewportF ToRawViewportF(float minDepth = 0, float maxDepth = 1)
            {
                return new RawViewportF
                {
                    X = OffsetX,
                    Y = OffsetY,
                    Width = Width,
                    Height = Height,
                    MinDepth = minDepth,
                    MaxDepth = maxDepth,
                };
            }

            public static implicit operator MyViewport(Vector2I vec)
            {
                return new MyViewport(vec);
            }
        }

        private VertexShader _vsFullscreenTri;
        private DepthStencilState _dsIgnoreDepthStencil;
        private readonly SamplerStates _samplers;

        public RenderUtils(Device device, IShaderCompiler shaderCompiler, SamplerStates samplers)
        {
            _vsFullscreenTri = shaderCompiler.CompileVertex(device, "fullscreen_tri.hlsl", "vs");
            _dsIgnoreDepthStencil = new DepthStencilState(device, new DepthStencilStateDescription
            {
                DepthComparison = Comparison.Greater,
                DepthWriteMask = DepthWriteMask.Zero,
                IsDepthEnabled = false,
                IsStencilEnabled = false
            });
            _samplers = samplers;
        }

        public void DrawFullscreenPass(DeviceContext rc, MyViewport customViewport)
        {
            rc.Rasterizer.SetViewport(customViewport.ToRawViewportF());
            rc.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            rc.VertexShader.Set(_vsFullscreenTri);
            rc.Draw(3, 0);
        }

        /// <summary>
        /// Copy from source to target and convert formats if needed.
        /// </summary>
        /// <param name="rc"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void CopyToRT(DeviceContext rc, ISrvTexture source, IRtvTexture target)
        {
            if (source.Size != target.Size)
            {
                // worse version of MyCopyToRT.Run()
                rc.OutputMerger.SetBlendState(null);
                rc.InputAssembler.InputLayout = null;
                rc.PixelShader.Set(MyCopyToRTAccessor.GetCopyStretchPs());
                rc.PixelShader.SetSampler(2, _samplers.Linear);
                rc.OutputMerger.SetTargets(target.Rtv);
                rc.OutputMerger.SetDepthStencilState(_dsIgnoreDepthStencil);
                rc.PixelShader.SetShaderResource(0, source.Srv);
                DrawFullscreenPass(rc, new MyViewport(target.Size));
                return;
            }

            //if (source.Srv.Description.Texture2D.MostDetailedMip != target.Rtv.Description.Texture2D.MipSlice)
            //{
            //    throw new NotImplementedException();
            //}

            if (source.Format == target.Format)
            {
                var srcSubres = source.Srv.Description.Texture2D.MostDetailedMip;
                var destSubres = target.Rtv.Description.Texture2D.MipSlice;
                rc.CopySubresourceRegion(source.Srv.Resource, srcSubres, null, target.Rtv.Resource, destSubres);
            }
            else
            {
                // worse version of MyCopyToRT.Run()
                rc.OutputMerger.SetBlendState(null);
                rc.InputAssembler.InputLayout = null;
                rc.PixelShader.Set(MyCopyToRTAccessor.GetCopyPs());
                rc.OutputMerger.SetTargets(target.Rtv);
                rc.OutputMerger.SetDepthStencilState(_dsIgnoreDepthStencil);
                rc.PixelShader.SetShaderResource(0, source.Srv);
                DrawFullscreenPass(rc, new MyViewport(target.Size));
            }
        }

        public void Dispose()
        {
            _vsFullscreenTri.Dispose();
            _dsIgnoreDepthStencil.Dispose();
        }
    }
}
