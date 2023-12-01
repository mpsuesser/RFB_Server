using UnityEngine;

public static class Util {
    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1,
        Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2) {

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parallel
        if (Mathf.Approximately(planarFactor, 0f) &&
            !Mathf.Approximately(crossVec1and2.sqrMagnitude, 0f)) {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        } else {
            intersection = Vector3.zero;
            return false;
        }
    }

    public static Vector2 ConvertTo2D(Vector3 _in) {
        Vector2 _out = new Vector2(_in.x, _in.z);
        _out *= _in.magnitude;
        return _out;
    }

    public static Vector3 ConvertTo3D(Vector2 _in) {
        Vector3 _out = new Vector3(_in.x, 1, _in.y);
        _out *= _in.magnitude;
        return _out;
    }
}
