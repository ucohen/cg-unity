using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class BezierCurve : MonoBehaviour
{
    // Bezier control points
    public Vector3 p0;
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;

    private float[] cumLengths; // Cumulative lengths lookup table
    private readonly int numSteps = 128; // Number of points to sample for the cumLengths LUT

    // Returns position B(t) on the Bezier curve for given parameter 0 <= t <= 1
    public Vector3 GetPoint(float t)
    {
        return Mathf.Pow(1-t, 3)*p0 + 3*Mathf.Pow(1-t,2)*t*p1 + 3*(1-t)*Mathf.Pow(t,2)*p2 + Mathf.Pow(t,3)*p3;
    }

    // Returns first derivative B'(t) for given parameter 0 <= t <= 1
    public Vector3 GetFirstDerivative(float t)
    {
        return 3 * (
            (  -t*t +2*t -1) * p0 +
            ( 3*t*t -4*t +1) * p1 +
            (-3*t*t +2*t   ) * p2 +
            (   t*t        ) * p3
            );
    }

    // Returns second derivative B''(t) for given parameter 0 <= t <= 1
    public Vector3 GetSecondDerivative(float t)
    {
        return 3 * (
            (-2*t +2) * p0 +
            ( 6*t -4) * p1 +
            (-6*t +2) * p2 +
            ( 2*t   ) * p3
            );
    }

    // Returns the tangent vector to the curve at point B(t) for a given 0 <= t <= 1
    public Vector3 GetTangent(float t)
    {
        return GetFirstDerivative(t).normalized;
    }

    // Returns the Frenet normal to the curve at point B(t) for a given 0 <= t <= 1
    public Vector3 GetNormal(float t)
    {
        return Vector3.Cross(GetTangent(t), GetBinormal(t)).normalized;
    }

    // Returns the Frenet binormal to the curve at point B(t) for a given 0 <= t <= 1
    public Vector3 GetBinormal(float t)
    {
        Vector3 b_tag = GetFirstDerivative(t);
        Vector3 b_tagaim = GetSecondDerivative(t);
        Vector3 t_tag = (b_tag + b_tagaim).normalized;
        
        return Vector3.Cross(GetTangent(t), t_tag).normalized;
    }

    // Calculates the arc-lengths lookup table
    public void CalcCumLengths()
    {
        List<Vector3> si = new List<Vector3>();
        for (int i=0; i<=numSteps; i++)
        {
            float t = (float)i / numSteps;
            si.Add(GetPoint(t));
        }
        cumLengths = new float[numSteps+1];
        cumLengths[0] = 0;
        for (int i=1; i<=numSteps; i++)
        {
            cumLengths[i] = cumLengths[i-1] + (si[i]-si[i-1]).magnitude;
        }
    }

    // Returns the total arc-length of the Bezier curve
    public float ArcLength()
    {
        Debug.Log($"total est. len: {cumLengths[numSteps]}");
        return cumLengths[numSteps];
    }

    // Returns approximate t s.t. the arc-length to B(t) = arcLength
    public float ArcLengthToT(float a)
    {
        int i = 0;
        for (i=0; i<numSteps; ++i)
        {
            if (cumLengths[i]<=a && a<=cumLengths[i+1])
                break;
        }
        float percentile = Mathf.InverseLerp(cumLengths[i], cumLengths[i+1], a); // float Percentage of value between start and end.
        
        float ti = (float)i / numSteps;
        float ti_p1 = (float)(i+1) / numSteps;
        float t = Mathf.Lerp(ti, ti_p1, percentile);             // linear interpolation

        Debug.Log($"total partial len (a={a}): {t}");
        return t;
    }

    // Start is called before the first frame update
    public void Start()
    {
        Refresh();
    }

    // Update the curve and send a message to other components on the GameObject
    public void Refresh()
    {
        CalcCumLengths();
        if (Application.isPlaying)
        {
            SendMessage("CurveUpdated", SendMessageOptions.DontRequireReceiver);
        }
    }

    // Set default values in editor
    public void Reset()
    {
        p0 = new Vector3(1f, 0f, 1f);
        p1 = new Vector3(1f, 0f, -1f);
        p2 = new Vector3(-1f, 0f, -1f);
        p3 = new Vector3(-1f, 0f, 1f);

        Refresh();
    }
}



