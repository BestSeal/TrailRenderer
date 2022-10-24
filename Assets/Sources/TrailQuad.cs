using Unity.Mathematics;

namespace Sources
{
    public struct TrailQuad
    {
        public float3 startPoint;
        public float3 endPoint;

        public float GetQuadLength() => math.distance(startPoint, endPoint);
    }
}