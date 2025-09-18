using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Vector2 = UnityEngine.Vector2;

namespace RuntimeNodeGraph.UI
{
    public sealed class NodeConnectionLineRenderer
    {
        private readonly Dictionary<string, NodeConnectionLine> _connections = new();
        private readonly float _lineThickness;
        private readonly float _hitTestThickness;
        private readonly NodeConnectionLine _temporaryConnectionLine = new(Vector2.zero, Vector2.zero);
        
        private bool _isDrawingTemporary;
        private PortDirection _temporaryConnectionEndPortDirection;
        
        public NodeConnectionLineRenderer(float lineThickness = 3f, float hitTestThickness = 10f)
        {
            _lineThickness = lineThickness;
            _hitTestThickness = hitTestThickness;
        }

        public void Clear()
        {
            _connections.Clear();
            _isDrawingTemporary = false;
        }
                
        public void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            
            foreach (var connection in _connections.Values)
            {
                var p0 = connection.Left;
                var p1 = connection.LeftControlPoint;
                var p2 = connection.RightControlPoint;
                var p3 = connection.Right;
                DrawBezierCurve(painter, p0, p1, p2, p3, connection.Color, _lineThickness);
            }

            if (_isDrawingTemporary)
            {
                var p0 = _temporaryConnectionLine.Left;
                var p1 = _temporaryConnectionLine.LeftControlPoint;
                var p2 = _temporaryConnectionLine.RightControlPoint;
                var p3 = _temporaryConnectionLine.Right;
                DrawBezierCurve(painter, p0, p1, p2, p3, new Color(1f, 1f, 1f, 0.5f), _lineThickness);
            }
        }
        
        public void AddConnection(string id, Vector2 start, Vector2 end)
        {
            _connections[id] = new(start, end);
        }
        
        public void UpdateConnection(string id, Vector2 start, Vector2 end)
        {
            if (_connections.ContainsKey(id))
            {
                _connections[id].UpdatePoints(start, end);
            }
        }
        
        public void RemoveConnection(string id)
        {
            _connections.Remove(id);
        }
        
        public void StartTemporaryConnection(Vector2 start, PortDirection startPortDirection)
        {
            _temporaryConnectionEndPortDirection = startPortDirection == PortDirection.Input
                                                    ? PortDirection.Output
                                                    : PortDirection.Input;
            _isDrawingTemporary = true;
            _temporaryConnectionLine.UpdatePoints(start, start);
        }
        
        public void UpdateTemporaryConnection(Vector2 end)
        {
            if (!_isDrawingTemporary) return;

            if (_temporaryConnectionEndPortDirection == PortDirection.Output)
            {
                _temporaryConnectionLine.UpdateLeftPoint(end);
            }
            else if (_temporaryConnectionEndPortDirection == PortDirection.Input)
            {
                _temporaryConnectionLine.UpdateRightPoint(end);
            }
            else
            {
                _temporaryConnectionLine.UpdateRightPoint(end);
            }
        }
        
        public void EndTemporaryConnection()
        {
            _isDrawingTemporary = false;
        }
        
        public bool TryGetConnectionAtPoint(Vector2 point, out string connectionId)
        {
            foreach (var connection in _connections)
            {
                if (IsPointOnBezierCurve(point, _hitTestThickness,
                    connection.Value.Left, connection.Value.LeftControlPoint, connection.Value.RightControlPoint, connection.Value.Right))
                {
                    connectionId = connection.Key;
                    return true;
                }
            }
            
            connectionId = "";
            return false;
        }

        private static bool IsPointOnBezierCurve(Vector2 point, float threshold, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            const int segments = 20;
            for (var i = 0; i < segments; i++)
            {
                var t1 = i / (float)segments;
                var t2 = (i + 1) / (float)segments;

                var segmentPoint1 = CalculateBezierCurvePoint(t1, p0, p1, p2, p3);
                var segmentPoint2 = CalculateBezierCurvePoint(t2, p0, p1, p2, p3);

                var distance = DistanceToLineSegment(point, segmentPoint1, segmentPoint2);
                if (distance <= threshold)
                {
                    return true;
                }
            }
            return false;
        }
        
        private static float DistanceToLineSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var ap = point - a;
            var t = Mathf.Clamp01(Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab));
            var closest = a + t * ab;
            return Vector2.Distance(point, closest);
        }

        private static Vector2 CalculateBezierCurvePoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            var u = 1 - t;
            var t2 = t * t;
            var u2 = u * u;
            var u3 = u2 * u;
            var t3 = t2 * t;

            var p = u3 * p0;
            p += 3 * u2 * t * p1;
            p += 3 * u * t2 * p2;
            p += t3 * p3;

            return p;
        }

        private static void DrawBezierCurve(Painter2D painter, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color color, float lineWidth)
        {
            painter.lineWidth = lineWidth;
            painter.strokeColor = color;
            painter.BeginPath();
            painter.MoveTo(p0);
            painter.BezierCurveTo(p1, p2, p3);
            painter.Stroke();
        }
    }
}
