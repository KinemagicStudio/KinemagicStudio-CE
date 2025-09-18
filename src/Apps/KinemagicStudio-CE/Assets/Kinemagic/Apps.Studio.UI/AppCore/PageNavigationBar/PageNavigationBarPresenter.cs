using System;
using EngineLooper;
using R3;

namespace Kinemagic.Apps.Studio.UI.AppCore
{
    public sealed class PageNavigationBarPresenter : IDisposable, IInitializable
    {
        private readonly PageNavigationBarView _view;
        private readonly UIViewContext _context;
        private readonly CompositeDisposable _disposables = new();

        public PageNavigationBarPresenter(PageNavigationBarView view, UIViewContext context)
        {
            _view = view;
            _context = context;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public void Initialize()
        {
            _view.ItemSelected
                .Subscribe(pageType =>
                {
                    _context.CurrentPage.Value = pageType;
                })
                .AddTo(_disposables);
        }
    }
}
