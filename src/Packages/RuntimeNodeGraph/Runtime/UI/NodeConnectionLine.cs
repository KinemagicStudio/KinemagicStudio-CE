using UnityEngine;

namespace RuntimeNodeGraph.UI
{
    public sealed class NodeConnectionLine
    {
        public Color Color { get; set; } = Color.white;
        public Vector2 Left { get; private set; }
        public Vector2 Right { get; private set; }
        public Vector2 LeftControlPoint { get; private set; }
        public Vector2 RightControlPoint { get; private set; }

        public NodeConnectionLine(Vector2 left, Vector2 right)
        {
            Left = left;
            Right = right;
            CalculateControlPoints();
        }

        public void UpdatePoints(Vector2 left, Vector2 right)
        {
            Left = left;
            Right = right;
            CalculateControlPoints();
        }

        public void UpdateLeftPoint(Vector2 point)
        {
            Left = point;
            CalculateControlPoints();
        }

        public void UpdateRightPoint(Vector2 point)
        {
            Right = point;
            CalculateControlPoints();
        }

        private void CalculateControlPoints()
        {
            var tangentLength = Vector2.Distance(Left, Right) * 0.5f;
            LeftControlPoint = Left + Vector2.right * tangentLength;
            RightControlPoint = Right - Vector2.right * tangentLength;
        }
    }
}
