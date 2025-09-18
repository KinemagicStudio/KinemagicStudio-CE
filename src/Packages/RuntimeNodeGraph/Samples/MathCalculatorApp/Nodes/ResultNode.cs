using System;
using Vector2 = System.Numerics.Vector2;

namespace RuntimeNodeGraph.Samples.MathCalculator
{
    [Serializable]
    public sealed class ResultNode : NodeData
    {
        public float? Result { get; set; }
        
        public ResultNode()
        {
            Name = "Result";
        }
        
        public ResultNode(string id, Vector2 normalizedPosition) : this()
        {
            Id = id;
            NormalizedPosition = normalizedPosition;
        }
    }
}
