using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sources
{
    [BurstCompile]
    public struct TrailJob : IJob, IDisposable
    {
        public float3 cameraDirection;
        public float quadWidth;
        public NativeArray<TrailQuad> trailQuads;
        public NativeArray<Vertex> vertices;
        public NativeArray<int> indices;

        public void Dispose()
        {
            if (indices.IsCreated)
            {
                indices.Dispose();
            }

            if (vertices.IsCreated)
            {
                vertices.Dispose();
            }

            if (trailQuads.IsCreated)
            {
                trailQuads.Dispose();
            }
        }

        public void Execute()
        {
            if (trailQuads.Length == 0) return;

            vertices[0] = new Vertex
            {
                position = trailQuads[0].startPoint +
                           math.normalize(math.cross(cameraDirection, trailQuads[0].startPoint)) * quadWidth / 2,
                normal = cameraDirection
            };

            vertices[1] = new Vertex
            {
                position = trailQuads[0].startPoint -
                           math.normalize(math.cross(cameraDirection, trailQuads[0].startPoint)) * quadWidth / 2,
                normal = cameraDirection
            };

            int step;
            int ind;

            for (int i = 0; i < trailQuads.Length; i++)
            {
                vertices[(i + 1) * 2] = new Vertex()
                {
                    position = trailQuads[i].endPoint +
                               math.normalize(math.cross(cameraDirection, trailQuads[i].endPoint)) *
                               quadWidth / 2,
                    normal = cameraDirection
                };

                vertices[(i + 1) * 2 + 1] = new Vertex()
                {
                    position = trailQuads[i].endPoint -
                               math.normalize(math.cross(cameraDirection, trailQuads[i].endPoint)) *
                               quadWidth / 2,
                    normal = cameraDirection
                };

                step = 2 * i;
                ind = 6 * i;
                indices[ind] = 1 + step;
                indices[1 + ind] = 0 + step;
                indices[2 + ind] = 2 + step;

                indices[3 + ind] = 2 + step;
                indices[4 + ind] = 3 + step;
                indices[5 + ind] = 1 + step;
            }

            step = 2 * (trailQuads.Length - 1);
            ind = 6 * (trailQuads.Length - 1);
            indices[ind] = 1 + step;
            indices[1 + ind] = 0 + step;
            indices[2 + ind] = 2 + step;

            indices[3 + ind] = 2 + step;
            indices[4 + ind] = 3 + step;
            indices[5 + ind] = 1 + step;
        }
    }
}