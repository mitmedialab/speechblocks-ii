using SimpleJSON;
using UnityEngine;

public class SerializationUtil
{
    public static JSONNode ParseOrNull(string record)
    {
        return null != record ? JSONNode.Parse(record) : null;
    }

    public static JSONNode SerializeColor(Color32 color)
    {
        JSONNode colorNode = new JSONObject();
        colorNode["r"] = color.r;
        colorNode["g"] = color.g;
        colorNode["b"] = color.b;
        if (255 != color.a) { colorNode["a"] = color.a; }
        return colorNode;
    }

    public static Color32 DeserializeColor(JSONNode node)
    {
        return new Color32((byte)node["r"], (byte)node["g"], (byte)node["b"], (null != node['a']) ? (byte)node["a"] : (byte)255);
    }

    public static JSONNode SerializeVector(Vector3 vector)
    {
        JSONNode vectorNode = new JSONObject();
        vectorNode["x"] = vector.x;
        vectorNode["y"] = vector.y;
        vectorNode["z"] = vector.z;
        return vectorNode;
    }

    public static Vector3 DeserializeVector(JSONNode node)
    {
        return new Vector3(node["x"], node["y"], node["z"]);
    }

    public static JSONNode SerializeQuaternion(Quaternion quaternion)
    {
        JSONNode quaternionNode = new JSONObject();
        quaternionNode["x"] = quaternion.x;
        quaternionNode["y"] = quaternion.y;
        quaternionNode["z"] = quaternion.z;
        quaternionNode["w"] = quaternion.w;
        return quaternionNode;
    }

    public static Quaternion DesearializeQuaternion(JSONNode node)
    {
        return new Quaternion(node["x"], node["y"], node["z"], node["w"]);
    }

    public static void SerializeTransform(Transform transform, JSONNode record)
    {
        record["pos"] = SerializeVector(transform.localPosition);
        record["rot"] = SerializeQuaternion(transform.localRotation);
        record["scl"] = SerializeVector(transform.localScale);
    }

    public static void DeserializeTransform(Transform transform, JSONNode record)
    {
        transform.localPosition = DeserializeVector(record["pos"]);
        transform.localRotation = DesearializeQuaternion(record["rot"]);
        transform.localScale = DeserializeVector(record["scl"]);
    }

    public static bool TransformMatches(Transform transform, JSONNode description)
    {
        try
        {
            if (Vector3.Distance(transform.localPosition, DeserializeVector(description["pos"])) > 0.025f) return false;
            if (Vector3.Distance(transform.localScale, DeserializeVector(description["scl"])) > 0.025f) return false;
            if (QuaternionL1Distance(transform.localRotation, DesearializeQuaternion(description["rot"])) > 0.01f) return false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static float QuaternionL1Distance(Quaternion q1, Quaternion q2)
    {
        return Mathf.Max(Mathf.Abs(q1.x - q2.x), Mathf.Abs(q1.y - q2.y), Mathf.Abs(q1.z - q2.z), Mathf.Abs(q1.w - q2.w));
    }
}
