using System;
using CinematicSequencer;
using CinematicSequencer.UI;
using EngineLooper;
using R3;

namespace Kinemagic.Apps.Studio.UI.CinematicSequencer
{
    public sealed class CinematicSequencerPresenter : IDisposable, IInitializable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly UIViewContext _context;
        private readonly CinematicSequencerView _view;
        private readonly TimelinePresenter _sequenceEditorPresenter;
        private readonly KeyframeAnimationEditorPresenter _animationEditorPresenter;
        private readonly KeyframeAnimationEditor _keyframeAnimationEditor;

        public CinematicSequencerPresenter(
            UIViewContext context,
            CinematicSequencerView view,
            TimelinePresenter sequenceEditorPresenter,
            KeyframeAnimationEditorPresenter animationEditorPresenter,
            KeyframeAnimationEditor keyframeAnimationEditor)
        {
            _context = context;
            _view = view;
            _sequenceEditorPresenter = sequenceEditorPresenter;
            _animationEditorPresenter = animationEditorPresenter;
            _keyframeAnimationEditor = keyframeAnimationEditor;
        }

        public void Dispose()
        {
            _keyframeAnimationEditor.OnLoaded -= OnAnimationEditorLoaded;
            _keyframeAnimationEditor.OnUnloaded -= OnAnimationEditorUnloaded;
            _disposables.Dispose();
        }

        public void Initialize()
        {
            _keyframeAnimationEditor.OnLoaded += OnAnimationEditorLoaded;
            _keyframeAnimationEditor.OnUnloaded += OnAnimationEditorUnloaded;

            _context.CurrentPage
                .Skip(1)
                .Subscribe(pageType =>
                {
                    var isVisible = pageType == UIPageType.CinematicSequencer;
                    if (isVisible)
                    {
                        if (_keyframeAnimationEditor.IsActive)
                        {
                            _view.ShowSequenceEditor();
                            _view.ShowAnimationEditor();
                        }
                        else
                        {
                            _view.ShowSequenceEditor();
                        }
                    }
                    else
                    {
                        _view.Hide();
                    }
                })
                .AddTo(_disposables);

            _sequenceEditorPresenter.Initialize();
            // _animationEditorPresenter.Initialize();
        }

        private void OnAnimationEditorLoaded()
        {
            _view.ShowAnimationEditor();
        }

        private void OnAnimationEditorUnloaded()
        {
            _view.ShowSequenceEditor();
        }
    }
}
