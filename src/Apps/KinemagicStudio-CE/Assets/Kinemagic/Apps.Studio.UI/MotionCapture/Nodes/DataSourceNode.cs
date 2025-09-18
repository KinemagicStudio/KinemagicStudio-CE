using System;
using System.Collections.Generic;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;
using RuntimeNodeGraph;

namespace Kinemagic.Apps.Studio.UI.MotionCapture
{
    [Serializable]
    public class DataSourceNode : NodeData
    {
        public int DataSourceId { get; set; }
        public MotionDataSourceType DataSourceType { get; set; }
        public ProcessingStatus Status { get; set; } = ProcessingStatus.NotStarted;
        public string ServerAddress { get; set; }
        public int Port { get; set; }

        public DataSourceNode(string id)
        {
            Id = id;
            Name = "Data Source";
            InputPorts = new List<PortData>();
            OutputPorts = new List<PortData>();
        }

        public void UpdateOutputPorts()
        {
            OutputPorts.Clear();

            switch (DataSourceType)
            {
                case MotionDataSourceType.VMCProtocolTypeA:
                case MotionDataSourceType.VMCProtocolTypeB:
                    OutputPorts.Add(new PortData
                    {
                        Id = $"{Id}_{MotionDataTypeNames.BodyTracking}",
                        DisplayName = "Body Tracking",
                        Direction = PortDirection.Output,
                        PortType = MotionDataTypeNames.BodyTracking
                    });
                    OutputPorts.Add(new PortData
                    {
                        Id = $"{Id}_{MotionDataTypeNames.FingerTracking}",
                        DisplayName = "Finger Tracking",
                        Direction = PortDirection.Output,
                        PortType = MotionDataTypeNames.FingerTracking
                    });
                    OutputPorts.Add(new PortData
                    {
                        Id = $"{Id}_{MotionDataTypeNames.FaceTracking}",
                        DisplayName = "Face Tracking",
                        Direction = PortDirection.Output,
                        PortType = MotionDataTypeNames.FaceTracking
                    });
                    OutputPorts.Add(new PortData
                    {
                        Id = $"{Id}_{MotionDataTypeNames.EyeTracking}",
                        DisplayName = "Eye Tracking",
                        Direction = PortDirection.Output,
                        PortType = MotionDataTypeNames.EyeTracking
                    });
                    break;

                case MotionDataSourceType.iFacialMocap:
                case MotionDataSourceType.FaceMotion3d:
                    OutputPorts.Add(new PortData
                    {
                        Id = $"{Id}_{MotionDataTypeNames.FaceTracking}",
                        DisplayName = "Face Tracking",
                        Direction = PortDirection.Output,
                        PortType = MotionDataTypeNames.FaceTracking
                    });
                    OutputPorts.Add(new PortData
                    {
                        Id = $"{Id}_{MotionDataTypeNames.EyeTracking}",
                        DisplayName = "Eye Tracking",
                        Direction = PortDirection.Output,
                        PortType = MotionDataTypeNames.EyeTracking
                    });
                    break;

                case MotionDataSourceType.Mocopi:
                    OutputPorts.Add(new PortData
                    {
                        Id = $"{Id}_{MotionDataTypeNames.BodyTracking}",
                        DisplayName = "Body Tracking",
                        Direction = PortDirection.Output,
                        PortType = MotionDataTypeNames.BodyTracking
                    });
                    break;
            }
        }
    }
}
