using System;
using System.Collections.Generic;
using RuntimeNodeGraph;

// TODO: Review
namespace Kinemagic.Apps.Studio.UI.MotionCapture
{
    [Serializable]
    public class CharacterNode : NodeData
    {
        public uint InstanceId { get; set; }
        public string CharacterName { get; set; }

        public CharacterNode(string id)
        {
            Id = id;
            Name = "Character";
            OutputPorts = new List<PortData>();
            InputPorts = new List<PortData>
            {
                new PortData
                {
                    Id = $"{Id}_{MotionDataTypeNames.BodyTracking}",
                    DisplayName = "Body Tracking",
                    Direction = PortDirection.Input,
                    PortType = MotionDataTypeNames.BodyTracking,
                },
                new PortData
                {
                    Id = $"{Id}_{MotionDataTypeNames.FingerTracking}",
                    DisplayName = "Finger Tracking",
                    Direction = PortDirection.Input,
                    PortType = MotionDataTypeNames.FingerTracking,
                },
                new PortData
                {
                    Id = $"{Id}_{MotionDataTypeNames.FaceTracking}",
                    DisplayName = "Face Tracking",
                    Direction = PortDirection.Input,
                    PortType = MotionDataTypeNames.FaceTracking,
                },
                new PortData
                {
                    Id = $"{Id}_{MotionDataTypeNames.EyeTracking}",
                    DisplayName = "Eye Tracking",
                    Direction = PortDirection.Input,
                    PortType = MotionDataTypeNames.EyeTracking,
                }
            };
        }
    }
}
