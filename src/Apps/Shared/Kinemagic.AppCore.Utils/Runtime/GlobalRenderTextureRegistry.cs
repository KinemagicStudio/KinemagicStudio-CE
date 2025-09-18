namespace Kinemagic.AppCore.Utils
{
    public static class GlobalRenderTextureRegistry
    {
        static RenderTextureRegistry _textureRegistry;

        public static bool IsInitialized => _textureRegistry != null;

        public static void Set(RenderTextureRegistry textureRegistry)
        {
            _textureRegistry = textureRegistry;
        }

        public static RenderTextureRegistry Get()
        {
            return _textureRegistry;
        }
    }
}