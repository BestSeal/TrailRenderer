using System;
using Unity.Collections;

namespace Sources
{
    public struct TrailMesh : IDisposable
    {
        public NativeArray<Vertex> vertices;
        public NativeArray<int> indices;

        public void Dispose()
        {
            vertices.Dispose();
            indices.Dispose();
        }
    }
}