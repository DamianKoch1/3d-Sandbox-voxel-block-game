using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh : MonoBehaviour
{
    protected Mesh mesh;

    protected MeshRenderer mr;

    protected MeshFilter mf;

    protected MeshCollider mc;

    [SerializeField]
    protected bool hasCollider;

    public void Initialize()
    {
        mr = GetComponent<MeshRenderer>();
        mf = GetComponent<MeshFilter>();
        mesh = new Mesh();
        if (hasCollider)
        {
            mc = gameObject.AddComponent<MeshCollider>();
        }
    }

    /// <summary>
    /// Builds mesh from vertices / triangles, applies it to collider / filter
    /// </summary>
    /// <param name="vertices"></param>
    /// <param name="triangles"></param>
    public void ApplyMesh(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        mesh.Clear();

        mesh.vertices = vertices.ToArray();

        mesh.triangles = triangles.ToArray();

        mesh.uv = uvs.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();

        mf.mesh = mesh;

        if (hasCollider)
        {
            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
        }
    }
}
