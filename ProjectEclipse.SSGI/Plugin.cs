using HarmonyLib;
using ProjectEclipse.SSGI.Config;
using ProjectEclipse.SSGI.Gui;
using Sandbox.Graphics.GUI;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using ProjectEclipse.SSGI.Common;
using ProjectEclipse.SSGI.Common.Interfaces;
using VRage.FileSystem;
using VRage.Plugins;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageRender;

[assembly: IgnoresAccessChecksTo("VRage.Render11")]

namespace ProjectEclipse.SSGI
{
    internal class Plugin : IPlugin
    {
        public static string Id { get; } = "EclipseEngine.SSGI";

        public static SSGIConfig Config { get; private set; }
        public static SSGIRenderPass Renderer => _renderer ?? (_renderer = CreateRenderer());

        public static IShaderCompiler ShaderCompiler { get; private set; }
        public static MyBorrowedRwTextureManager ResourcePool { get; private set; }
        public static SamplerStates SamplerStates { get; private set; }
        public static RenderUtils RenderUtils { get; private set; }

        private static SSGIRenderPass _renderer;
        private Harmony _harmony;

        public void Init(object gameInstance)
        {
            _harmony = new Harmony(Id);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            Config = new SSGIConfig(Path.Combine(MyFileSystem.UserDataPath, "Storage", "EclipseEngine", "ssgi.cfg"));

            ResourcePool = MyManagers.RwTexturesPool;
            ShaderCompiler = new FileShaderCompiler(Path.Combine(MyFileSystem.ShadersBasePath, "Shaders", "ProjectEclipse"));
            SamplerStates = new SamplerStates(MyRender11.DeviceInstance);
            RenderUtils = new RenderUtils(MyRender11.DeviceInstance, ShaderCompiler, SamplerStates);
        }

        public void LoadAssets(string assetFolderPath)
        {

        }

        public void OpenConfigDialog()
        {
            MyGuiSandbox.AddScreen(new GuiScreenConfig(Config));
        }

        private static SSGIRenderPass CreateRenderer()
        {
            return new SSGIRenderPass(MyRender11.DeviceInstance, Config, ResourcePool, ShaderCompiler, RenderUtils, SamplerStates, MyRender11.ViewportResolution);
        }

        public void Update()
        {

        }

        public void Dispose()
        {
            Config.Save();

            _renderer?.Dispose();
            RenderUtils.Dispose();
            SamplerStates.Dispose();
        }
    }
}
