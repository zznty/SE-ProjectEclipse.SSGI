using HarmonyLib;
using ProjectEclipse.Backend.Reflection;
using ProjectEclipse.Backend.Reflection.Wrappers;
using ProjectEclipse.Common;
using ProjectEclipse.Common.Interfaces;
using ProjectEclipse.SSGI.Config;
using ProjectEclipse.SSGI.Gui;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.FileSystem;
using VRage.Plugins;

namespace ProjectEclipse.SSGI
{
    internal class Plugin : IPlugin
    {
        public static string Id { get; } = "EclipseEngine.SSGI";

        public static SSGIConfig Config { get; private set; }
        public static SSGIRenderPass Renderer => _renderer ?? (_renderer = CreateRenderer());

        public static IShaderCompiler ShaderCompiler { get; private set; }
        public static IResourcePool ResourcePool { get; private set; }
        public static SamplerStates SamplerStates { get; private set; }
        public static RenderUtils RenderUtils { get; private set; }

        private static SSGIRenderPass _renderer;
        private Harmony _harmony;

        public void Init(object gameInstance)
        {
            _harmony = new Harmony(Id);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            Config = new SSGIConfig(Path.Combine(MyFileSystem.UserDataPath, "Storage", "EclipseEngine", "ssgi.cfg"));

            ShaderCompiler = new FileShaderCompiler(Path.Combine(MyFileSystem.ShadersBasePath, "Shaders", "ProjectEclipse"));
            ResourcePool = new BorrowedRwTextureManagerWrapper();
            SamplerStates = new SamplerStates(MyRender11Accessor.GetDeviceInstance());
            RenderUtils = new RenderUtils(MyRender11Accessor.GetDeviceInstance(), ShaderCompiler, SamplerStates);
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
            return new SSGIRenderPass(MyRender11Accessor.GetDeviceInstance(), Config, ResourcePool, ShaderCompiler, RenderUtils, SamplerStates, MyRender11Accessor.GetViewportResolution());
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
