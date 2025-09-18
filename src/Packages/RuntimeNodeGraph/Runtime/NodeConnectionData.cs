using System;
using System.Collections.Generic;

namespace RuntimeNodeGraph
{
    [Serializable]
    public class ConnectionData
    {
        public string Id;
        public string OutputNodeId;
        public string OutputPortId;
        public string InputNodeId;
        public string InputPortId;
    }
}
