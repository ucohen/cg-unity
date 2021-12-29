using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class BezierMesh : MonoBehaviour
{
    private BezierCurve curve; // The Bezier curve around which to build the mesh

    public float Radius = 0.5f; // The distance of mesh vertices from the curve
    public int NumSteps = 16; // Number of points along the curve to sample
    public int NumSides = 8; // Number of vertices created at each point

    // Awake is called when the script instance is being loaded
    public void Awake()
    {
        curve = GetComponent<BezierCurve>();
        BuildMesh();
    }

    // Returns a "tube" Mesh built around the given Bézier curve
    public static Mesh GetBezierMesh(BezierCurve curve, float radius, int numSteps, int numSides)
    {
        QuadMeshData meshData = new QuadMeshData();

        // Debug.DrawLine(curve.p0, curve.p0+curve.GetTangent(0), Color.cyan, 3f);
        // Debug.DrawLine(curve.p0, curve.p0+curve.GetNormal(0), Color.red, 3f);
        
        // 1. sample curve
        for (int i=0; i<=numSteps; i++)
        {
            float t = (float)i/numSteps;
            Vector3 si = curve.GetPoint(t);
            Vector3 bi = curve.GetBinormal(t);
            Vector3 ni = curve.GetNormal(t);

            // 2. create sides
            for (float j=0; j<360; j+=360/numSides)
            {
                Vector2 p = radius * GetUnitCirclePoint(j);
                meshData.vertices.Add(si + p.x*(bi) + p.y*(ni));
            }
        }

        // 3. create quads
        for (int i=0; i<numSteps; i++)
        {
            int r1 = i*numSides;
            int r2 = (i+1)*numSides;
            for (int j=0; j<numSides-1; j++)
            {
                meshData.quads.Add(new Vector4(r1+j+1, r1+j, r2+j, r2+j+1));
            }
            meshData.quads.Add(new Vector4(r1, r1+numSides-1, r2+numSides-1, r2));
        }
        return meshData.ToUnityMesh();
    }

    // Returns 2D coordinates of a point on the unit circle at a given angle from the x-axis
    private static Vector2 GetUnitCirclePoint(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }

    public void BuildMesh()
    {
        var meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = GetBezierMesh(curve, Radius, NumSteps, NumSides);
    }

    // Rebuild mesh when BezierCurve component is changed
    public void CurveUpdated()
    {
        BuildMesh();
    }
}



[CustomEditor(typeof(BezierMesh))]
class BezierMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Update Mesh"))
        {
            var bezierMesh = target as BezierMesh;
            bezierMesh.BuildMesh();
        }
    }
}