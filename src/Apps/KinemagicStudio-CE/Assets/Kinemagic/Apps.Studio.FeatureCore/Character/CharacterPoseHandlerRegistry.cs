using System;
using System.Collections.Generic;
using Kinemagic.Apps.Studio.Contracts.Character;
using Kinemagic.Apps.Studio.Contracts.Motion;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class CharacterPoseHandlerRegistry : IDisposable
    {
        private readonly Dictionary<InstanceId, HumanoidPoseHandler> _poseHandlers = new();
        private readonly Dictionary<InstanceId, FaceExpressionHandler> _faceExpressionHandlers = new();
        private readonly Dictionary<InstanceId, EyeTrackingHandler> _eyeTrackingHandlers = new();

        private readonly Dictionary<DataSourceId, List<InstanceId>> _bodyTrackingActorIds = new();
        private readonly Dictionary<DataSourceId, List<InstanceId>> _fingerTrackingActorIds = new();
        private readonly Dictionary<DataSourceId, List<InstanceId>> _faceTrackingActorIds = new();
        private readonly Dictionary<DataSourceId, List<InstanceId>> _eyeTrackingActorIds = new();

        public IEnumerable<KeyValuePair<InstanceId, HumanoidPoseHandler>> PoseHandlers => _poseHandlers;

        public void Dispose()
        {
            _poseHandlers.Clear();
            _faceExpressionHandlers.Clear();
            _eyeTrackingHandlers.Clear();

            _bodyTrackingActorIds.Clear();
            _fingerTrackingActorIds.Clear();
            _faceTrackingActorIds.Clear();
            _eyeTrackingActorIds.Clear();
        }

        public bool TryAdd(VrmCharacter character)
        {
            var instanceId = character.InstanceId;

            if (_poseHandlers.ContainsKey(instanceId))
            {
                return false;
            }

            _poseHandlers[instanceId] = new HumanoidPoseHandler(character);
            _faceExpressionHandlers[instanceId] = new FaceExpressionHandler(character.RootTransform);
            _eyeTrackingHandlers[instanceId] = new EyeTrackingHandler(character.RootTransform);
            return true;
        }

        public void Remove(InstanceId characterInstanceId)
        {
            _poseHandlers.Remove(characterInstanceId);
            _faceExpressionHandlers.Remove(characterInstanceId);
            _eyeTrackingHandlers.Remove(characterInstanceId);

            foreach (var dataSourceId in _bodyTrackingActorIds.Keys)
            {
                _bodyTrackingActorIds[dataSourceId].Remove(characterInstanceId);
            }
            foreach (var dataSourceId in _fingerTrackingActorIds.Keys)
            {
                _fingerTrackingActorIds[dataSourceId].Remove(characterInstanceId);
            }
            foreach (var dataSourceId in _faceTrackingActorIds.Keys)
            {
                _faceTrackingActorIds[dataSourceId].Remove(characterInstanceId);
            }
            foreach (var dataSourceId in _eyeTrackingActorIds.Keys)
            {
                _eyeTrackingActorIds[dataSourceId].Remove(characterInstanceId);
            }
        }

        public bool TryGetHumanoidPoseHandler(InstanceId characterInstanceId, out HumanoidPoseHandler characterPoseHandler)
        {
            return _poseHandlers.TryGetValue(characterInstanceId, out characterPoseHandler);
        }

        // TODO: Avoid memory allocation
        public bool TryGetBodyTrackingTargetPoseHandlers(DataSourceId dataSourceId, out HumanoidPoseHandler[] characterPoseHandlers)
        {
            if (_bodyTrackingActorIds.TryGetValue(dataSourceId, out var characterInstanceIds))
            {
                characterPoseHandlers = new HumanoidPoseHandler[characterInstanceIds.Count];
                for (var i = 0; i < characterInstanceIds.Count; i++)
                {
                    if (_poseHandlers.TryGetValue(characterInstanceIds[i], out var poseHandler))
                    {
                        characterPoseHandlers[i] = poseHandler;
                    }
                }
                return true;
            }

            characterPoseHandlers = null;
            return false;
        }

        // TODO: Avoid memory allocation
        public bool TryGetFingerTrackingTargetPoseHandlers(DataSourceId dataSourceId, out HumanoidPoseHandler[] characterPoseHandlers)
        {
            if (_fingerTrackingActorIds.TryGetValue(dataSourceId, out var characterInstanceIds))
            {
                characterPoseHandlers = new HumanoidPoseHandler[characterInstanceIds.Count];
                for (var i = 0; i < characterInstanceIds.Count; i++)
                {
                    if (_poseHandlers.TryGetValue(characterInstanceIds[i], out var poseHandler))
                    {
                        characterPoseHandlers[i] = poseHandler;
                    }
                }
                return true;
            }

            characterPoseHandlers = null;
            return false;
        }

        // TODO: Avoid memory allocation
        public bool TryGetFaceExpressionHandlers(DataSourceId dataSourceId, out FaceExpressionHandler[] faceExpressionHandlers)
        {
            if (_faceTrackingActorIds.TryGetValue(dataSourceId, out var characterInstanceIds))
            {
                faceExpressionHandlers = new FaceExpressionHandler[characterInstanceIds.Count];
                for (var i = 0; i < characterInstanceIds.Count; i++)
                {
                    if (_faceExpressionHandlers.TryGetValue(characterInstanceIds[i], out var faceExpressionHandler))
                    {
                        faceExpressionHandlers[i] = faceExpressionHandler;
                    }
                }
                return true;
            }

            faceExpressionHandlers = null;
            return false;
        }

        public bool TryGetEyeTrackingHandlers(DataSourceId dataSourceId, out EyeTrackingHandler[] eyeTrackingHandlers)
        {
            if (_eyeTrackingActorIds.TryGetValue(dataSourceId, out var characterInstanceIds))
            {
                eyeTrackingHandlers = new EyeTrackingHandler[characterInstanceIds.Count];
                for (var i = 0; i < characterInstanceIds.Count; i++)
                {
                    if (_eyeTrackingHandlers.TryGetValue(characterInstanceIds[i], out var eyeTrackingHandler))
                    {
                        eyeTrackingHandlers[i] = eyeTrackingHandler;
                    }
                }
                return true;
            }

            eyeTrackingHandlers = null;
            return false;
        }

        public bool TryAddDataSourceMapping(InstanceId characterInstanceId, DataSourceId dataSourceId, MotionDataType motionDataType)
        {
            return motionDataType switch
            {
                MotionDataType.BodyTracking => TryAddDataSourceMapping(_bodyTrackingActorIds, dataSourceId, characterInstanceId),
                MotionDataType.FingerTracking => TryAddDataSourceMapping(_fingerTrackingActorIds, dataSourceId, characterInstanceId),
                MotionDataType.FaceTracking => TryAddDataSourceMapping(_faceTrackingActorIds, dataSourceId, characterInstanceId),
                MotionDataType.EyeTracking => TryAddDataSourceMapping(_eyeTrackingActorIds, dataSourceId, characterInstanceId),
                _ => false
            };
        }

        public bool TryRemoveDataSourceMapping(InstanceId characterInstanceId, DataSourceId dataSourceId, MotionDataType motionDataType)
        {
            return motionDataType switch
            {
                MotionDataType.BodyTracking => TryRemoveDataSourceMapping(_bodyTrackingActorIds, dataSourceId, characterInstanceId),
                MotionDataType.FingerTracking => TryRemoveDataSourceMapping(_fingerTrackingActorIds, dataSourceId, characterInstanceId),
                MotionDataType.FaceTracking => TryRemoveDataSourceMapping(_faceTrackingActorIds, dataSourceId, characterInstanceId),
                MotionDataType.EyeTracking => TryRemoveDataSourceMapping(_eyeTrackingActorIds, dataSourceId, characterInstanceId),
                _ => false
            };
        }

        public void RemoveAllDataSourceMappings(DataSourceId dataSourceId, InstanceId characterInstanceId)
        {
            TryRemoveDataSourceMapping(_bodyTrackingActorIds, dataSourceId, characterInstanceId);
            TryRemoveDataSourceMapping(_fingerTrackingActorIds, dataSourceId, characterInstanceId);
            TryRemoveDataSourceMapping(_faceTrackingActorIds, dataSourceId, characterInstanceId);
            TryRemoveDataSourceMapping(_eyeTrackingActorIds, dataSourceId, characterInstanceId);
        }

        private bool TryAddDataSourceMapping(Dictionary<DataSourceId, List<InstanceId>> targetActorIds, DataSourceId dataSourceId, InstanceId characterInstanceId)
        {
            if (targetActorIds.ContainsKey(dataSourceId))
            {
                if (!targetActorIds[dataSourceId].Contains(characterInstanceId))
                {
                    targetActorIds[dataSourceId].Add(characterInstanceId);
                    targetActorIds[dataSourceId].Sort();
                    return true;
                }
                return false;
            }
            else
            {
                targetActorIds.Add(dataSourceId, new List<InstanceId> { characterInstanceId });
                return true;
            }
        }

        private bool TryRemoveDataSourceMapping(Dictionary<DataSourceId, List<InstanceId>> targetActorIds, DataSourceId dataSourceId, InstanceId characterInstanceId)
        {
            if (targetActorIds.TryGetValue(dataSourceId, out var characterIds))
            {
                return characterIds.Remove(characterInstanceId);
            }
            return false;
        }
    }
}