// Copyright (c) 2020 hadashiA
// Licensed under the MIT License.
//
// The original source code is available on GitHub.
// https://github.com/hadashiA/VContainer/blob/1.13.2/VContainer/Assets/VContainer/Runtime/Internal/CompositeDisposable.cs

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// namespace VContainer.Internal
namespace EngineLooper.VContainer.Internal
{
    sealed class CompositeDisposable : IDisposable
    {
        readonly Stack<IDisposable> disposables = new Stack<IDisposable>();

        public void Dispose()
        {
            IDisposable disposable;
            do
            {
                lock (disposables)
                {
                    disposable = disposables.Count > 0
                        ? disposables.Pop()
                        : null;
                }
                disposable?.Dispose();
            } while (disposable != null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(IDisposable disposable)
        {
            lock (disposables)
            {
                disposables.Push(disposable);
            }
        }
    }
}