using System;
using UnityEngine;

namespace RuntimeNodeGraph.Samples.MathCalculator
{
    [Serializable]
    public sealed class NumberNode : NodeData
    {
        public float Value { get; set; }
        
        public NumberNode()
        {
            Id = Guid.NewGuid().ToString();
            Name = "Number";
        }
        
        // public NumberNode(string id, float value, Vector2 position) : this()
        // {
        //     Id = id;
        //     Value = value;
        //     Position = position;
        //     Data = value;
        // }
    }
}
