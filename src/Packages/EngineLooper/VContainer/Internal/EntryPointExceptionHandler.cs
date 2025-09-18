// Copyright (c) 2020 hadashiA
// Licensed under the MIT License.
//
// The original source code is available on GitHub.
// https://github.com/hadashiA/VContainer/blob/1.13.2/VContainer/Assets/VContainer/Runtime/Unity/EntryPointExceptionHandler.cs

using System;

// namespace VContainer.Unity
namespace EngineLooper.VContainer.Internal
{
    sealed class EntryPointExceptionHandler
    {
        readonly Action<Exception> handler;

        public EntryPointExceptionHandler(Action<Exception> handler)
        {
            this.handler = handler;
        }

        public void Publish(Exception ex)
        {
            handler.Invoke(ex);
        }
    }
}