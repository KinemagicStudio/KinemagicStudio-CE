using System;
using R3;

namespace Kinemagic.Apps.Studio.Contracts.Character
{
    public interface ICharacterInstanceRegistry : IDisposable
    {
        Observable<CharacterInstanceInfo> Added { get; }
        Observable<CharacterInstanceInfo> Removed { get; }
    }
}
