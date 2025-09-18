using System;
using EngineLooper;
using R3;

namespace Kinemagic.Apps.Studio.UI.AppCore
{
    public sealed class HelpPagePresenter : IDisposable, IInitializable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly UIViewContext _context;
        private readonly LicenseView _licenseView;

        public HelpPagePresenter(UIViewContext context, LicenseView licenseView)
        {
            _context = context;
            _licenseView = licenseView;
        }

        public void Initialize()
        {
            _context.CurrentPage
                .Skip(1)
                .Subscribe(pageType =>
                {
                    if (pageType == UIPageType.Help)
                    {
                        _licenseView.Show();
                    }
                    else
                    {
                        _licenseView.Hide();
                    }
                })
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}