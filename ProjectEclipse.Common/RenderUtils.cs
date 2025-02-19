using ProjectEclipse.Common.Interfaces;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using System;
using VRageMath;

namespace ProjectEclipse.Common
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

        public RenderUtils(Device device, IShaderCompiler shaderCompiler)
        {
            _vsFullscreenTri = shaderCompiler.CompileVertex(device, "fullscreen_tri.hlsl", "vs");
            _dsIgnoreDepthStencil = new DepthStencilState(device, new DepthStencilStateDescription
            {
                DepthComparison = Comparison.Greater,
                DepthWriteMask = DepthWriteMask.Zero,
                IsDepthEnabled = false,
                IsStencilEnabled = false
            });
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
        public void CopyToRT(DeviceContext rc, ITexture2DSrv source, ITexture2DSrvRtv target)
        {
            if (source.Size != target.Size)
            {
                throw new NotImplementedException();
            }

            if (source.Srv.Description.Texture2D.MostDetailedMip != target.Rtv.Description.Texture2D.MipSlice)
            {
                throw new NotImplementedException();
            }

            int subresourceIndex = target.Rtv.Description.Texture2D.MipSlice;

            if (source.Format == target.Format)
            {
                rc.CopySubresourceRegion(source.Texture, subresourceIndex, null, target.Texture, subresourceIndex);
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
