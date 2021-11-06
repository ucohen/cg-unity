using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    public TextAsset BVHFile; // The BVH file that defines the animation and skeleton
    public bool animate; // Indicates whether or not the animation should be running

    private BVHData data; // BVH data of the BVHFile will be loaded here
    private int currFrame = 0; // Current frame of the animation
    float lasttime;

    public float fps;
    // GameObject root_joint;
    // Start is called before the first frame update
    void Start()
    {
        BVHParser parser = new BVHParser();
        data = parser.Parse(BVHFile);
        // GameObject joint = 
        CreateJoint(data.rootJoint, Vector3.zero);
        Debug.Log($"END Of start {BVHFile}");
        lasttime = Time.realtimeSinceStartup;
    }

    // Returns a Matrix4x4 representing a rotation aligning the up direction of an object with the given v
    Matrix4x4 RotateTowardsVector(Vector3 v)
    {
        // Your code here
        v = v.normalized;
        // Debug.Log($"v= {v}");

        float thetax = 90.0f - Mathf.Atan2(v.y, v.z) * Mathf.Rad2Deg;
        float thetaz = 90.0f - Mathf.Atan2(Mathf.Sqrt(v.y*v.y + v.z*v.z), v.x) * Mathf.Rad2Deg;
        Matrix4x4 Rx = MatrixUtils.RotateX(-thetax);
        Matrix4x4 Rz = MatrixUtils.RotateZ(thetaz);
        Matrix4x4 R =  Rx.inverse* Rz.inverse;  //Rz * Rx;  //

        // test
        // Vector3 up = new Vector3(0,1,0);
        // Vector3 rout = R.MultiplyVector(up);
        // Matrix4x4 Rref = MatrixUtils.RotateTowardsVector(v);
        // Vector3 rout1 = Rref.MultiplyVector(up);
        // Debug.Log($"rout1= {rout1}, should equal={v}");
        return R;
    }

    // Creates a Cylinder GameObject between two given points in 3D space
    GameObject CreateCylinderBetweenPoints(Vector3 p1, Vector3 p2, float diameter)
    {
        GameObject bone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Vector3 scale = new Vector3(diameter,(p2-p1).magnitude/2f, diameter);

        Matrix4x4 T = MatrixUtils.Translate((p1+p2)/2f);
        Matrix4x4 R = RotateTowardsVector(p2-p1);
        Matrix4x4 S = MatrixUtils.Scale(scale);
        MatrixUtils.ApplyTransform(bone, T * R * S);
        
        // Debug.DrawLine(p1, p2, Color.red, 50.0f);
        return bone;
    }

    // Creates a GameObject representing a given BVHJoint and recursively creates GameObjects for it's child joints
    GameObject CreateJoint(BVHJoint joint, Vector3 parentPosition)
    {
        joint.gameObject = new GameObject(joint.name);
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.parent = joint.gameObject.transform;
        
        int scale = joint.name=="Head" ? 8 : 2;
        Matrix4x4 S = MatrixUtils.Scale(new Vector3(scale, scale, scale));
        Matrix4x4 T = MatrixUtils.Translate(parentPosition + joint.offset);
        MatrixUtils.ApplyTransform(joint.gameObject, T * S);

        if (!joint.isEndSite)
        {
            foreach (BVHJoint child in joint.children)
            {
                GameObject child_joint = CreateJoint(child, sphere.transform.position);
                child_joint.transform.parent = joint.gameObject.transform;

                GameObject bone = CreateCylinderBetweenPoints(joint.gameObject.transform.position, child_joint.transform.position, 0.5f);
                bone.transform.parent = joint.gameObject.transform;
            }
        }
        return joint.gameObject;
    }

    // Transforms BVHJoint according to the keyframe channel data, and recursively transforms its children
    private void TransformJoint(BVHJoint joint, Matrix4x4 parentTransform, float[] keyframe)
    {
        // Transform t = joint.gameObject.transform;
        Matrix4x4 T  = MatrixUtils.Translate(joint.offset);
        Matrix4x4 Rx = MatrixUtils.RotateX(-keyframe[joint.rotationChannels.x]);
        Matrix4x4 Ry = MatrixUtils.RotateY(-keyframe[joint.rotationChannels.y]);
        Matrix4x4 Rz = MatrixUtils.RotateZ(keyframe[joint.rotationChannels.z]);
        // Matrix4x4 S  = MatrixUtils.Scale(t.localScale);  // rigid body motion - no scaling!

        Matrix4x4 R = Matrix4x4.identity;
        for (int i=0; i<3; ++i)
        {
            if (i==joint.rotationOrder.x)   { R = R * Rx;  continue; }
            if (i==joint.rotationOrder.y)   { R = R * Ry;  continue; }
            if (i==joint.rotationOrder.z)   { R = R * Rz;  continue; }
        }
        Matrix4x4 M = parentTransform * T * R; // * S;
        MatrixUtils.ApplyTransform(joint.gameObject, M);

        if (!joint.isEndSite)
            foreach (BVHJoint child in joint.children)
            {
                TransformJoint(child, M, keyframe);
            }
    }

    // Update is called once per frame
    void Update()
    {
        float now = Time.realtimeSinceStartup;
        if (now > lasttime + data.frameLength)
        {
            int n = (int)((now - lasttime) / data.frameLength);
            currFrame = (currFrame + n) % data.numFrames;
            // fps = 1f/(now - lasttime);

            if (animate)
            {
                Debug.Log($"#{currFrame}:  now:{now}  lasttime:{lasttime}");

                // print(data.keyframes[currFrame][data.rootJoint.positionChannels.x]);
                Matrix4x4 T = MatrixUtils.Translate(
                    new Vector3(data.keyframes[currFrame][data.rootJoint.positionChannels.x],
                                data.keyframes[currFrame][data.rootJoint.positionChannels.y],
                                data.keyframes[currFrame][data.rootJoint.positionChannels.z]));
                Matrix4x4 S  = MatrixUtils.Scale(Vector3.one);  // rigid body motion - no scaling!
                MatrixUtils.ApplyTransform(data.rootJoint.gameObject, T * S);

                TransformJoint(data.rootJoint, T, data.keyframes[currFrame]);
                // animate = false;
            }
            lasttime = now;
        }
    }
    // private void OnGUI() {
    //     GUIStyle guiStyle = GUIStyle.none;
    //     guiStyle.fontSize = 30;
    //     guiStyle.normal.textColor = Color.red;
    //     guiStyle.alignment = TextAnchor.UpperLeft;            
    //     GUI.Label(new Rect(450, 50, 500, 100), System.String.Format("{0:F0} FPS", fps), guiStyle);
    // }
}
