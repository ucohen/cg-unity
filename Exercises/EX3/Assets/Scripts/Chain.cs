using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Chain : MonoBehaviour
{
    private BezierCurve curve; // The Bezier curve around which to build the chain
    private List<GameObject> chainLinks = new List<GameObject>(); // A list to contain the chain links GameObjects

    public GameObject ChainLink; // Reference to a GameObject representing a chain link
    public float LinkSize = 2.0f; // Distance between links

    // Awake is called when the script instance is being loaded
    public void Awake()
    {
        curve = GetComponent<BezierCurve>();
    }

    // Constructs a chain made of links along the given Bezier curve, updates them in the chainLinks List
    public void ShowChain()
    {
        // Clean up the list of old chain links
        foreach (GameObject link in chainLinks)
        {
            Destroy(link);
        }

        float length = 0.0f;
        bool top = true;
        GameObject go;
        while (length < curve.ArcLength())
        {
            float t = curve.ArcLengthToT(length);
            Vector3 si = curve.GetPoint(t);
            Vector3 ti = curve.GetTangent(t);
            Vector3 bi = curve.GetBinormal(t);
            Vector3 ni = curve.GetNormal(t);
            if (top)
                go = CreateChainLink(si, ti, bi);
            else
                go = CreateChainLink(si, ti, ni);
            chainLinks.Add(go);
            length += LinkSize;
            top = !top;
        }
    }

    // Instantiates & returns a ChainLink at given position, oriented towards the given forward and up vectors
    public GameObject CreateChainLink(Vector3 position, Vector3 forward, Vector3 up)
    {
        GameObject chainLink = Instantiate(ChainLink);
        chainLink.transform.position = position;
        chainLink.transform.rotation = Quaternion.LookRotation(forward, up);
        chainLink.transform.parent = transform;
        return chainLink;
    }

    // Rebuild chain when BezierCurve component is changed
    public void CurveUpdated()
    {
        ShowChain();
    }
}

[CustomEditor(typeof(Chain))]
class ChainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Show Chain"))
        {
            var chain = target as Chain;
            chain.ShowChain();
        }
    }
}