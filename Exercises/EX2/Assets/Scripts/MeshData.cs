using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MeshData
{
    public List<Vector3> vertices; // The vertices of the mesh 
    public List<int> triangles; // Indices of vertices that make up the mesh faces
    public Vector3[] normals; // The normals of the mesh, one per vertex

    // Class initializer
    public MeshData()
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();
    }

    // Returns a Unity Mesh of this MeshData that can be rendered
    public Mesh ToUnityMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            normals = normals
        };

        return mesh;
    }

    // Calculates surface normals for each vertex, according to face orientation
    public void CalculateNormals()
    {
        List<int>[] v_facelist = new List<int>[vertices.Count];
        for (int i=0;i<v_facelist.Length;++i)
            v_facelist[i] = new List<int>();

        List<Vector3> face_normals = new List<Vector3>();
        for (int t=0; t<triangles.Count; t+=3)
        {
            // 1. find all triangles having this vertex
            int v1 = triangles[t  ];
            int v2 = triangles[t+1];
            int v3 = triangles[t+2];
            v_facelist[v1].Add(t/3);
            v_facelist[v2].Add(t/3);
            v_facelist[v3].Add(t/3);

            // 2. calculate the normals for each triangle
            Vector3 V1 = vertices[v1];
            Vector3 V2 = vertices[v2];
            Vector3 V3 = vertices[v3];
            Vector3 n = Vector3.Cross((V1-V3), (V2-V3));
            face_normals.Add(n.normalized);
        }

        // 3. set the average normal as the vertex normal
        normals = new Vector3[vertices.Count];
        List<Vector3> normlist = new List<Vector3>();
        for (int i=0; i<vertices.Count; ++i)
        {
            Vector3 norm = new Vector3(0,0,0);
            foreach (int face in v_facelist[i])
            {
                norm += face_normals[face];
            }
            if (v_facelist[i].Count==0)
                Debug.Log($"vertex: <{i}> not used!");
            
            normals[i] = norm.normalized;
        }
        Debug.Log("CalculateNormals Done!");
    }

    // Edits mesh such that each face has a unique set of 3 vertices
    public void MakeFlatShaded()
    {
        List<Vector3> new_verteces = new List<Vector3>();
        List<int> new_triangles = new List<int>();

        for (int i=0; i<triangles.Count; i++)
        {
            new_verteces.Add(vertices[triangles[i]]);
            new_triangles.Add(i);
        }

        vertices = new_verteces;
        triangles = new_triangles;
    }
}