using System;
using System.Collections.Generic;
using UnityEngine;


public class CCMeshData
{
    public List<Vector3> points; // Original mesh points
    public List<Vector4> faces; // Original mesh quad faces
    public List<Vector4> edges; // Original mesh edges
    public List<Vector3> facePoints; // Face points, as described in the Catmull-Clark algorithm
    public List<Vector3> edgePoints; // Edge points, as described in the Catmull-Clark algorithm
    public List<Vector3> newPoints; // New locations of the original mesh points, according to Catmull-Clark
}

public static class CatmullClark
{
    // Returns a QuadMeshData representing the input mesh after one iteration of Catmull-Clark subdivision.
    public static QuadMeshData Subdivide(QuadMeshData quadMeshData)
    {
        // Create and initialize a CCMeshData corresponding to the given QuadMeshData
        CCMeshData meshData = new CCMeshData();
        meshData.points = quadMeshData.vertices;
        meshData.faces = quadMeshData.quads;
        meshData.edges = GetEdges(meshData);
        meshData.facePoints = GetFacePoints(meshData);
        meshData.edgePoints = GetEdgePoints(meshData);
        meshData.newPoints = GetNewPoints(meshData);

        // Combine facePoints, edgePoints and newPoints into a subdivided QuadMeshData
        QuadMeshData qmd = new QuadMeshData();
        
        // new vertices are combined
        foreach (var v in meshData.newPoints)
            qmd.vertices.Add(v);
        foreach (var v in meshData.facePoints)
            qmd.vertices.Add(v);
        foreach (var v in meshData.edgePoints)
            qmd.vertices.Add(v);
        
        // helper dictionaty to fetch edge point
        Dictionary<int, int> dedges = new Dictionary<int, int>();
        for (int i=0; i<meshData.edges.Count; ++i)
        {
            var edge = meshData.edges[i];
            int key = getKey(edge.x, edge.y, qmd.vertices.Count);
            dedges[key] = i;
        }

        int C = qmd.vertices.Count;
        int total_points = meshData.points.Count + meshData.faces.Count;
        // split each face into 4 faces
        for (int i=0; i<meshData.faces.Count; ++i)
        {
            int face_point = meshData.points.Count + i;
            Vector4 face = meshData.faces[i];
            int k1 = getKey(face.x, face.y, C);
            int k2 = getKey(face.y, face.z, C);
            int k3 = getKey(face.z, face.w, C);
            int k4 = getKey(face.w, face.x, C);

            int e1 = total_points + dedges[k1];
            int e2 = total_points + dedges[k2];
            int e3 = total_points + dedges[k3];
            int e4 = total_points + dedges[k4];

            qmd.quads.Add(new Vector4(face.x, e1, face_point, e4));
            qmd.quads.Add(new Vector4(face.y, e2, face_point, e1));
            qmd.quads.Add(new Vector4(face.z, e3, face_point, e2));
            qmd.quads.Add(new Vector4(face.w, e4, face_point, e3));
        }
        Debug.Log($"#vertices: {qmd.vertices.Count}, #quads: {qmd.quads.Count}");
        return qmd;
    }
    private static int getKey(float a, float b, int C)
    {
        return (int)(Math.Min(a,b) * C + Math.Max(a,b));
    }

    // Returns a list of all edges in the mesh defined by given points and faces.
    // Each edge is represented by Vector4(p1, p2, f1, f2)
    // p1, p2 are the edge vertices
    // f1, f2 are faces incident to the edge. If the edge belongs to one face only, f2 is -1
    public static List<Vector4> GetEdges(CCMeshData mesh)
    {
        List<Vector4> edges = new List<Vector4>();
        Dictionary<Tuple<int,int>, int> dedges = new Dictionary<Tuple<int,int>, int>();
        for (int i=0; i<mesh.faces.Count; ++i)
        {
            var f = mesh.faces[i];
            UpdateEdge(mesh, edges, dedges, i, (int)f.x, (int)f.y);
            UpdateEdge(mesh, edges, dedges, i, (int)f.y, (int)f.z);
            UpdateEdge(mesh, edges, dedges, i, (int)f.z, (int)f.w);
            UpdateEdge(mesh, edges, dedges, i, (int)f.w, (int)f.x);
        }

        // add edges without pairs
        foreach(var kvp in dedges)
        {
            int p1 = kvp.Key.Item1;
            int p2 = kvp.Key.Item2;
            edges.Add(new Vector4(p1, p2, kvp.Value, -1));
        }
        return edges;
    }
    private static void UpdateEdge(CCMeshData mesh, List<Vector4> edges, Dictionary<Tuple<int,int>, int> dedges, int i, int p1, int p2)
    {
        int f2;
        Tuple<int,int> key = new Tuple<int,int>(Math.Min(p1,p2), Math.Max(p1,p2));
        if (dedges.TryGetValue(key, out f2))
        {
            edges.Add(new Vector4(p1, p2, f2, i));
            dedges.Remove(key);
        }
        else
        {
            dedges.Add(key, i);
        }
    }

    // Returns a list of "face points" for the given CCMeshData, as described in the Catmull-Clark algorithm 
    public static List<Vector3> GetFacePoints(CCMeshData mesh)
    {
        List<Vector3> facePoints = new List<Vector3>();
        foreach (Vector4 face in mesh.faces)
        {
            Vector3 p = (mesh.points[(int)face.x] + mesh.points[(int)face.y] + mesh.points[(int)face.z] + mesh.points[(int)face.w]) / 4;
            facePoints.Add(p);
        }
        return facePoints;
    }

    // Returns a list of "edge points" for the given CCMeshData, as described in the Catmull-Clark algorithm 
    public static List<Vector3> GetEdgePoints(CCMeshData mesh)
    {
        List<Vector3> edgePoints = new List<Vector3>();
        Vector3 p;
        foreach (Vector4 edge in mesh.edges)
        {
            if ((int)edge.w == -1)
            {
                p = (mesh.points[(int)edge.x] + mesh.points[(int)edge.y] + mesh.facePoints[(int)edge.z]) / 3;
                Debug.Log("THIS SHOULDNT HAPPEN!!!!");
            }
            else
                p = (mesh.points[(int)edge.x] + mesh.points[(int)edge.y] + mesh.facePoints[(int)edge.z] + mesh.facePoints[(int)edge.w]) / 4;
            edgePoints.Add(p);
        }
        return edgePoints;
    }
    public static List<Vector3> GetNewPoints1(CCMeshData mesh)
    {
        // P = (F + 2R + (n-3)P) / n
        List<Vector3> newPoints = new List<Vector3>();
        List<int> num_items = new List<int>();

        // initialize the vertex location
        foreach (var p in mesh.points)
        {
            newPoints.Add(Vector3.zero);
            num_items.Add(0);
        }
        
        // add facepoint locations
        List<Vector3> F   = new List<Vector3>(mesh.points.Count);
        List<int>     F_n = new List<int>(mesh.points.Count);
        for (int i=0; i<mesh.faces.Count; ++i)
        {
            var face = mesh.faces[i];
            int p1 = (int)face.x, p2=(int)face.y, p3=(int)face.z, p4=(int)face.w;
            F[p1] += mesh.facePoints[i];
            F[p2] += mesh.facePoints[i];
            F[p3] += mesh.facePoints[i];
            F[p4] += mesh.facePoints[i];
            F_n[p1]++; F_n[p2]++; F_n[p3]++; F_n[p4]++;
        }

        // add edgepoint locations
        for (int i=0; i<mesh.edges.Count; ++i)
        {
            var edge = mesh.edges[i];
            int p1 = (int)edge.x, p2=(int)edge.y;
            newPoints[p1] += mesh.edgePoints[i];
            newPoints[p2] += mesh.edgePoints[i];
            num_items[p1]++; num_items[p2]++;
        }

        // normalize results
        for (int i=0; i<newPoints.Count; ++i)
            if (num_items[i]>0)
                newPoints[i] /= num_items[i];

        return newPoints;
    }
    // Returns a list of new locations of the original points for the given CCMeshData, as described in the CC algorithm 
    // template
    // private static List<int> fillList(List<T> L, U value)
    // {
    //     List<T> L1 = new List<T>();
    //     for (int i=0;i<c;++i)
    //         L1.Add(0);
    //     return L1;
    // }
    public static List<Vector3> GetNewPoints(CCMeshData mesh)
    {
        // P = (F + 2R + (n-3)P) / n
        List<Vector3> newPoints = new List<Vector3>();
        // List<int> num_items = new List<int>();

        // initialize the vertex location
        
        // F - facepoint average
        List<Vector3> F   = new List<Vector3>();
        List<int>     F_n = new List<int>(mesh.points.Count);
        List<Vector3> R   = new List<Vector3>(mesh.points.Count);
        List<int>     R_n = new List<int>(mesh.points.Count);
        foreach (var p in mesh.points)
        {
            newPoints.Add(p);
            F.Add(Vector3.zero);
            F_n.Add(0);
            R.Add(Vector3.zero);
            R_n.Add(0);
        }

        for (int i=0; i<mesh.faces.Count; ++i)
        {
            var face = mesh.faces[i];
            int p1 = (int)face.x, p2=(int)face.y, p3=(int)face.z, p4=(int)face.w;
            Vector3 facepoint = mesh.facePoints[i];
            F[p1]+=facepoint;  F[p2]+=facepoint;  F[p3]+=facepoint;  F[p4]+=facepoint;
            F_n[p1]++;         F_n[p2]++;         F_n[p3]++;         F_n[p4]++;
        }
        for (int i=0; i<mesh.points.Count; ++i)
            F[i] /= F_n[i];

        // R - edges mid-point average
        for (int i=0; i<mesh.edges.Count; ++i)
        {
            var edge = mesh.edges[i];
            int p1 = (int)edge.x, p2=(int)edge.y;
            Vector3 midpoint = (mesh.points[p1] + mesh.points[p2]) / 2;
            R[p1] += midpoint;  R[p2] += midpoint;
            R_n[p1]++;          R_n[p2]++;
        }
        for (int i=0; i<mesh.points.Count; ++i)
            R[i] /= R_n[i];

        // normalize results
        for (int i=0; i<mesh.points.Count; ++i)
        {
            newPoints[i] = (F[i] + 2*R[i] + (F_n[i]-3)*newPoints[i]) / F_n[i]; 
        }

        // inspect min-size 
        // Vector3 first = newPoints[0];
        // float d = 100F;
        // for (int i=1; i<mesh.points.Count; ++i)
        // {
        //     Vector3 p = newPoints[i];
        //     d = Math.Min(d, (p-first).magnitude);
        // }
        // Debug.Log($"min dist= {d}");
        return newPoints;
    }
}
