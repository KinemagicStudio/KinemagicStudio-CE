namespace Kinemagic.Apps.Studio.UI
{
    public static class GlobalContextProvider
    {
        static UIViewContext _uiViewContext;

        public static UIViewContext UIViewContext
        {
            get => _uiViewContext;
            set => _uiViewContext = value;
        }
    }
}