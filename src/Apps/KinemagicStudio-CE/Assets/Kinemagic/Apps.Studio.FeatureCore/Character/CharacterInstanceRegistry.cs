using System.Collections.Generic;
using Kinemagic.Apps.Studio.Contracts.Character;
using R3;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class CharacterInstanceRegistry : ICharacterInstanceRegistry
    {
        private readonly uint _maxLocalInstanceId = 100;

        private readonly Dictionary<InstanceId, VrmCharacter> _characterInstances = new();
        private readonly SequentialIdPool _localInstanceIdPool = new(1);

        private readonly Subject<CharacterInstanceInfo> _instanceAdded = new();
        public Observable<CharacterInstanceInfo> Added => _instanceAdded;

        private readonly Subject<CharacterInstanceInfo> _instanceRemoved = new();
        public Observable<CharacterInstanceInfo> Removed => _instanceRemoved;

        public void Dispose()
        {
            _instanceAdded.Dispose();
            _instanceRemoved.Dispose();

            foreach (var instance in _characterInstances.Values)
            {
                instance.Dispose();
            }
            _characterInstances.Clear();
        }

        public bool TryAddLocalInstance(VrmCharacter character, out InstanceId instanceId)
        {
            var nextId = _localInstanceIdPool.GetNextId(reuseReleasedIds: true);
            if (nextId > _maxLocalInstanceId)
            {
                _localInstanceIdPool.ReleaseId(nextId);
                instanceId = new InstanceId(0);
                return false;
            }

            instanceId = new InstanceId(nextId);
            character.InstanceId = instanceId;

            _characterInstances[instanceId] = character;

            _instanceAdded.OnNext(new CharacterInstanceInfo(instanceId, character.Name));
            return true;
        }

        public void Destroy(InstanceId instanceId)
        {
            Remove(instanceId, out var removedCharacter);
            removedCharacter?.Dispose();
        }

        public void Remove(InstanceId instanceId, out VrmCharacter removedInstance)
        {
            if (!_characterInstances.ContainsKey(instanceId))
            {
                removedInstance = null;
                return;
            }

            if (instanceId.Value <= _maxLocalInstanceId)
            {
                _localInstanceIdPool.ReleaseId(instanceId.Value);
            }

            _characterInstances.Remove(instanceId, out removedInstance);
            _instanceRemoved.OnNext(new CharacterInstanceInfo(instanceId, removedInstance.Name));
        }
    }
}
