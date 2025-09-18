using System;
using Vector2 = System.Numerics.Vector2;

namespace RuntimeNodeGraph.Samples.MathCalculator
{
    public enum MathOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }
    
    [Serializable]
    public sealed class MathOperationNode : NodeData
    {
        public MathOperation Operation { get; set; }
        
        public MathOperationNode(MathOperation operation, Vector2 normalizedPosition)
        {
            Id = Guid.NewGuid().ToString();
            Operation = operation;
            NormalizedPosition = normalizedPosition;
            
            // 演算タイプに応じて名前を設定
            Name = operation switch
            {
                MathOperation.Add => "Add (+)",
                MathOperation.Subtract => "Subtract (-)",
                MathOperation.Multiply => "Multiply (×)",
                MathOperation.Divide => "Divide (÷)",
                _ => "Math Operation"
            };
        }
    }
}
