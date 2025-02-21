using SharpDX.Direct3D11;

namespace ProjectEclipse.SSGI.Common.Interfaces
{
    public interface IShaderCompiler
    {
        VertexShader CompileVertex(Device device, string id, string entryPoint);
        PixelShader CompilePixel(Device device, string id, string entryPoint);
        ComputeShader CompileCompute(Device device, string id, string entryPoint);
    }
}
