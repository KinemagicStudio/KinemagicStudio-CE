namespace RuntimeNodeGraph
{
    public static class MathUtils
    {
        public static UnityEngine.Vector2 ToUnityVector2(this System.Numerics.Vector2 vector)
        {
            return new UnityEngine.Vector2(vector.X, vector.Y);
        }
        
        public static UnityEngine.Vector2 ToUnityVector2(this System.Numerics.Vector3 vector)
        {
            return new UnityEngine.Vector2(vector.X, vector.Y);
        }

        public static System.Numerics.Vector2 ToSystemNumericsVector2(this UnityEngine.Vector2 vector)
        {
            return new System.Numerics.Vector2(vector.x, vector.y);
        }

        public static System.Numerics.Vector2 ToSystemNumericsVector2(this UnityEngine.Vector3 vector)
        {
            return new System.Numerics.Vector2(vector.x, vector.y);
        }
    }
}