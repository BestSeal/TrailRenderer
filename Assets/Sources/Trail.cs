using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sources
{
    public sealed class Trail : IDisposable
    {
        private List<TrailQuad> _trailQuads;
        private float _quadWidth;
        private float _segmentLength;
        private TrailJob _job;

        public static readonly VertexAttributeDescriptor[] VertexLayout =
        {
            new() { attribute = VertexAttribute.Position, dimension = 3 },
            new() { attribute = VertexAttribute.Normal, dimension = 3 },
        };

        public int QuadsCount => _trailQuads?.Count ?? 0;

        public Trail(float maxSegmentLength, float quadWidth)
        {
            _quadWidth = quadWidth;
            _segmentLength = maxSegmentLength;
            _trailQuads = new List<TrailQuad>(1000);
        }

        public void ExpandTrail(ref Gun.Projectile projectile, Vector3 startPosition)
        {
            var endPoint = projectile.position;

            if (_trailQuads.Count > 0 && _trailQuads[^1].GetQuadLength() < _segmentLength)
            {
                _trailQuads[^1] = new TrailQuad
                {
                    startPoint = _trailQuads[^1].startPoint,
                    endPoint = endPoint
                };
                return;
            }

            startPosition = _trailQuads.Count > 0 ? _trailQuads[^1].endPoint : startPosition;

            var startPoint = startPosition != Vector3.zero
                ? startPosition
                // Adding offset in case of start point at the zero point V3(0,0,0)
                // since cross product of zero vector is always zero vector
                : startPosition + new Vector3(0.01f, 0.01f, 0.01f);

            _trailQuads.Add(new TrailQuad
            {
                startPoint = startPoint,
                endPoint = endPoint
            });
        }

        public TrailMesh GenerateMesh(Vector3 cameraDirection)
        {
            _job = new TrailJob()
            {
                cameraDirection = cameraDirection,
                quadWidth = _quadWidth,
                // Count total amount of vertices as 2n + 2 where n - amount of quads
                trailQuads = new NativeArray<TrailQuad>(_trailQuads.Count, Allocator.TempJob),
                vertices = new NativeArray<Vertex>(_trailQuads.Count * 2 + 2, Allocator.TempJob),
                // 2 triangles for every quad and each triangle requires 3 points
                indices = new NativeArray<int>(_trailQuads.Count * 6, Allocator.TempJob)
            };
            for (int i = 0; i < _trailQuads.Count; i++)
            {
                _job.trailQuads[i] = _trailQuads[i];
            }
            
            _job.Schedule().Complete();

            return new TrailMesh
            {
                vertices = _job.vertices,
                indices = _job.indices
            };
        }

        public void Dispose()
        {
            _job.Dispose();
        }

        public void ClearTrail() => _trailQuads.Clear();
    }
}