using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// This is just an example, you can use these callbacks if you want.
// Projectiles will be added and removed cyclically,
// so you may expect projectile indices to be persistent.

namespace Sources
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public sealed class ProjectileTrailRenderer : MonoBehaviour
    {
        [SerializeField] private Gun gun;

        [Tooltip("Width of a trail")] [SerializeField]
        private float width = 0.5f;

        [Tooltip("The max length of a polygon (shorter polygons make trails smoother but more complex)")]
        [SerializeField]
        private float segmentLength = 20.0f;
        private MeshFilter _meshFilter;
        private Trail[] _trails;
        private Transform _activeCameraTransform;
        private Vector3 _gunPosition;

        private void Start()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _trails = new Trail[gun.maxProjectileCount];
            _activeCameraTransform = Camera.main.transform;
            _gunPosition = gun.transform.position;
            _meshFilter.mesh = new Mesh();

            if (gun != null)
            {
                gun.onProjectileCreated += OnProjectileCreated;
                gun.onProjectileRemoved += OnProjectileRemoved;
                gun.onProjectileMoved += OnProjectileMoved;
            }
        }

        private void LateUpdate() => DrawTrails();

        /// <summary>
        /// A callback that is called when a new projectile is created.
        /// </summary>
        /// <param name="index">Unique numeric ID of a projectile in range [0, gun.maxProjectileCount - 1].</param>
        /// <param name="projectile">The created projectile.</param>
        private void OnProjectileCreated(int index, ref Gun.Projectile projectile)
        {
            _trails[index] = new Trail(segmentLength, width);
        }

        /// <summary>
        /// A callback that is called when a projectile is removed.
        /// </summary>
        /// <param name="index">Unique numeric ID of a projectile in range [0, gun.maxProjectileCount - 1].</param>
        /// <param name="projectile">The removed projectile.</param>
        private void OnProjectileRemoved(int index, ref Gun.Projectile projectile)
        {
            _trails[index].ClearTrail();
        }

        /// <summary>
        /// A callback that is called every frame while a projectile is moving.
        /// </summary>
        /// <param name="index">Unique numeric ID of a projectile in range [0, gun.maxProjectileCount - 1].</param>
        /// <param name="projectile">The moved projectile.</param>
        private void OnProjectileMoved(int index, ref Gun.Projectile projectile)
        {
            _trails[index].ExpandTrail(ref projectile, _gunPosition);
        }

        private void DrawTrails()
        {
            var totalQuadsCount = _trails.Sum(x => x?.QuadsCount ?? 0);

            if (totalQuadsCount == 0) return;

            var vertices = new NativeArray<Vertex>(totalQuadsCount * 2 + _trails.Count(x => x?.QuadsCount > 0) * 2,
                Allocator.TempJob);
            var indices = new NativeArray<int>(totalQuadsCount * 6, Allocator.TempJob);
            var offsetIndices = 0;
            var offsetVertices = 0;

            foreach (var trail in _trails)
            {
                if (trail == null || trail.QuadsCount == 0) continue;

                var trailMesh = trail.GenerateMesh(_activeCameraTransform.forward);

                for (int i = 0; i < trailMesh.indices.Length; i++)
                {
                    indices[i + offsetIndices] = trailMesh.indices[i] + offsetVertices;
                }

                offsetIndices += trailMesh.indices.Length;

                for (int i = 0; i < trailMesh.vertices.Length; i++)
                {
                    vertices[i + offsetVertices] = trailMesh.vertices[i];
                }

                offsetVertices += trailMesh.vertices.Length;
            }


            var mesh = _meshFilter.mesh;
            
            mesh.SetVertexBufferParams(vertices.Length, Trail.VertexLayout);
            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
            mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
            mesh.SetIndexBufferData(indices, 0, 0, indices.Length);
            mesh.subMeshCount = 1;
            SubMeshDescriptor smd = new SubMeshDescriptor
            {
                topology = MeshTopology.Triangles,
                vertexCount = vertices.Length,
                indexCount = indices.Length
            };
            mesh.SetSubMesh(0, smd, 0);

            vertices.Dispose();
            indices.Dispose();
            DisposeTrails();
        }

        private void DisposeTrails()
        {
            foreach (var trail in _trails)
            {
                trail?.Dispose();
            }
        }
    }
}