using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierCurve))]
public class BezierCurveInspector : Editor
{
    private const float handleSize = 0.1f;
    private const float pickSize = 0.06f;

    private BezierCurve curve;
    private Transform handleTransform;
    private Quaternion handleRotation;
    private int selectedIndex = -1;

    private void OnSceneGUI()
    {
        curve = target as BezierCurve;
        handleTransform = curve.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ?
            handleTransform.rotation : Quaternion.identity;

        Vector3 p0 = ShowControlPoint(0);
        Vector3 p1 = ShowControlPoint(1);
        Vector3 p2 = ShowControlPoint(2);
        Vector3 p3 = ShowControlPoint(3);

        Handles.color = Color.gray;
        Handles.DrawLine(p0, p1);
        Handles.DrawLine(p2, p3);

        Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 3f);
    }

    private Vector3 ShowControlPoint(int index)
    {
        Vector3 point = handleTransform.TransformPoint(GetControlPoint(index));
        float size = HandleUtility.GetHandleSize(point);
        Handles.color = Color.white;
        if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.SphereHandleCap))
        {
            selectedIndex = index;
        }
        if (selectedIndex == index)
        {
            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(curve, "Move Point");
                EditorUtility.SetDirty(curve);
                SetControlPoint(index, handleTransform.InverseTransformPoint(point));
                curve.Refresh();
            }
        }
        return point;
    }

    private Vector3 GetControlPoint(int index)
    {
        if (index == 0) return curve.p0;
        if (index == 1) return curve.p1;
        if (index == 2) return curve.p2;
        return curve.p3;
    }

    private void SetControlPoint(int index, Vector3 position)
    {
        if (index == 0) curve.p0 = position;
        else if (index == 1) curve.p1 = position;
        else if (index == 2) curve.p2 = position;
        else curve.p3 = position;
    }
}