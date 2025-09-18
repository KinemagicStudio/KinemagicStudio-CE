using System;
using System.Collections.Generic;
using System.Numerics;
using EngineLooper;
using Kinemagic.Apps.Studio.Contracts.Motion;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;
using RuntimeNodeGraph;

namespace Kinemagic.Apps.Studio.UI.MotionCapture
{
    public sealed class MotionCaptureNodeGraphEditor : NodeGraphEditor, IDisposable, IInitializable
    {
        public const string DataSourceNodeIdPrefix = "datasource-";
        public const string CharacterNodeIdPrefix = "character-";

        private readonly Dictionary<string, (uint characterId, MotionDataType motionDataType, DataSourceId dataSourceId)> _connectionMappings = new();

        public Vector2 RandomPositionMin = new(100, 100);
        public Vector2 RandomPositionMax = new(800, 500);
        public event Action<uint, MotionDataType, DataSourceId> OnMappingCreated;
        public event Action<uint, MotionDataType, DataSourceId> OnMappingRemoved;

        public void Initialize()
        {
            ConnectionAdded += OnConnectionAdded;
            ConnectionRemoved += OnConnectionRemoved;
        }

        public void Dispose()
        {
            ConnectionAdded -= OnConnectionAdded;
            ConnectionRemoved -= OnConnectionRemoved;
            _connectionMappings.Clear();
        }

        public void CreateDataSourceNode(int dataSourceId, MotionDataSourceType dataSourceType, string address, int port)
        {
            var node = new DataSourceNode($"{DataSourceNodeIdPrefix}{dataSourceId}")
            {
                Name = $"{dataSourceType} [{dataSourceId}]",
                DataSourceId = dataSourceId,
                DataSourceType = dataSourceType,
                ServerAddress = address,
                Port = port,
                NormalizedPosition = GetRandomPosition()
            };
            node.UpdateOutputPorts();
            AddNode(node);
        }

        public void CreateCharacterNode(uint instanceId, string characterName)
        {
            var node = new CharacterNode($"{CharacterNodeIdPrefix}{instanceId}")
            {
                Name = characterName,
                InstanceId = instanceId,
                CharacterName = characterName,
                NormalizedPosition = GetRandomPosition()
            };
            AddNode(node);
        }

        private void OnConnectionAdded(ConnectionData connection)
        {
            var outputNode = Graph.Find(connection.OutputNodeId);
            var inputNode = Graph.Find(connection.InputNodeId);

            if (outputNode is DataSourceNode dataSourceNode && inputNode is CharacterNode characterNode)
            {
                var motionDataType = MotionDataType.Unknown;
                if (connection.OutputPortId.Contains(MotionDataTypeNames.BodyTracking)) motionDataType = MotionDataType.BodyTracking;
                if (connection.OutputPortId.Contains(MotionDataTypeNames.FingerTracking)) motionDataType = MotionDataType.FingerTracking;
                if (connection.OutputPortId.Contains(MotionDataTypeNames.FaceTracking)) motionDataType = MotionDataType.FaceTracking;
                if (connection.OutputPortId.Contains(MotionDataTypeNames.EyeTracking)) motionDataType = MotionDataType.EyeTracking;

                _connectionMappings[connection.Id] = (characterNode.InstanceId, motionDataType, new DataSourceId(dataSourceNode.DataSourceId));
                OnMappingCreated?.Invoke(characterNode.InstanceId, motionDataType, new DataSourceId(dataSourceNode.DataSourceId));
            }
        }

        private void OnConnectionRemoved(string connectionId)
        {
            if (_connectionMappings.TryGetValue(connectionId, out var mapping))
            {
                OnMappingRemoved?.Invoke(mapping.characterId, mapping.motionDataType, mapping.dataSourceId);
                _connectionMappings.Remove(connectionId);
            }
        }

        private Vector2 GetRandomPosition()
        {
            return new Vector2(
                UnityEngine.Random.Range(RandomPositionMin.X, RandomPositionMax.X),
                UnityEngine.Random.Range(RandomPositionMin.Y, RandomPositionMax.Y)
            );
        }
    }
}
