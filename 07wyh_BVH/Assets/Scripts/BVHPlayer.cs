using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BVHPlayer : MonoBehaviour
{
    [Header("文件与显示")]
    public string bvhFileName = "sample.bvh";   // 放在 Assets/StreamingAssets 下
    public float scale = 0.1f;                  // BVH 单位通常是厘米，缩小到米
    public Vector3 rootEuler = new Vector3(0, 180, 0); // 坐标系修正，左右反了就把 Y 改 0/180

    [Header("播放控制")]
    public float playbackSpeed = 1.0f;
    public bool isPlaying = true;
    public bool loop = true;

    [Header("骨骼可视化")]
    public float lineWidth = 0.02f;
    public Material lineMaterial;               // 可以不填，代码会创建一个默认材质

    public BVHData Data { get; private set; }
    public float CurrentTime { get; private set; }
    public int CurrentFrame { get; private set; }

    private Dictionary<BVHJoint, Transform> jointToTransform = new Dictionary<BVHJoint, Transform>();
    private Dictionary<BVHJoint, LineRenderer> jointToLine = new Dictionary<BVHJoint, LineRenderer>();

    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, bvhFileName);
        if (!File.Exists(path))
        {
            Debug.LogError("BVH 文件不存在: " + path);
            return;
        }

        string text = File.ReadAllText(path);
        Data = BVHParser.Parse(text);
        Debug.Log($"解析完成: {Data.frameCount} 帧, 每帧 {Data.frameTime}s, {Data.joints.Count} 个关节");

        BuildHierarchy();
        BuildBoneLines();
        ApplyPose(0, 0, 0f);
        UpdateBoneLines();
    }

    void BuildHierarchy()
    {
        // 本 GameObject 就是根节点
        transform.localScale = Vector3.one * scale;
        transform.localEulerAngles = rootEuler;

        foreach (var joint in Data.joints)
        {
            if (joint.parent == null)
            {
                jointToTransform[joint] = transform;
                transform.name = "BVH_" + joint.name;
            }
            else
            {
                var go = new GameObject("BVH_" + joint.name);
                go.transform.SetParent(jointToTransform[joint.parent], false);
                go.transform.localPosition = joint.offset;
                jointToTransform[joint] = go.transform;
            }
            joint.transform = jointToTransform[joint];
        }
    }

    void BuildBoneLines()
    {
        foreach (var joint in Data.joints)
        {
            if (joint.parent == null) continue;

            var go = new GameObject("Line_" + joint.name);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.startColor = Color.cyan;
            lr.endColor = Color.cyan;
            lr.material = lineMaterial != null
                ? lineMaterial
                : new Material(Shader.Find("Sprites/Default"));

            jointToLine[joint] = lr;
        }
    }

    void Update()
    {
        if (Data == null || Data.frameCount == 0) return;

        if (isPlaying)
        {
            CurrentTime += Time.deltaTime * playbackSpeed;

            if (loop)
            {
                if (Data.Duration > 0f) CurrentTime %= Data.Duration;
            }
            else
            {
                CurrentTime = Mathf.Clamp(CurrentTime, 0f, Data.Duration - 0.0001f);
            }
        }

        float frameFloat = CurrentTime / Data.frameTime;
        int frameA = Mathf.FloorToInt(frameFloat);
        int frameB = frameA + 1;
        if (frameB >= Data.frameCount) frameB = loop ? 0 : Data.frameCount - 1;
        frameA = Mathf.Clamp(frameA, 0, Data.frameCount - 1);

        float t = frameFloat - Mathf.Floor(frameFloat);
        if (!loop && frameA == Data.frameCount - 1) t = 0f;

        CurrentFrame = frameA;
        ApplyPose(frameA, frameB, t);
        UpdateBoneLines();
    }

    void ApplyPose(int frameA, int frameB, float t)
    {
        float[] dataA = Data.frames[frameA];
        float[] dataB = Data.frames[frameB];

        foreach (var joint in Data.joints)
        {
            Transform tf = jointToTransform[joint];

            Vector3 posA = ReadPosition(joint, dataA);
            Vector3 posB = ReadPosition(joint, dataB);
            tf.localPosition = Vector3.Lerp(posA, posB, t);

            Quaternion rotA = ReadRotation(joint, dataA);
            Quaternion rotB = ReadRotation(joint, dataB);
            tf.localRotation = Quaternion.Slerp(rotA, rotB, t);
        }
    }

    Vector3 ReadPosition(BVHJoint joint, float[] frame)
    {
        float x = joint.offset.x, y = joint.offset.y, z = joint.offset.z;
        bool hasPos = false;
        foreach (var ch in joint.channels)
        {
            if (ch.type == "Xposition") { x = frame[ch.index]; hasPos = true; }
            else if (ch.type == "Yposition") { y = frame[ch.index]; hasPos = true; }
            else if (ch.type == "Zposition") { z = frame[ch.index]; hasPos = true; }
        }
        return hasPos ? new Vector3(x, y, z) : joint.offset;
    }

    Quaternion ReadRotation(BVHJoint joint, float[] frame)
    {
        Quaternion q = Quaternion.identity;
        foreach (var ch in joint.channels)
        {
            float angle = frame[ch.index];
            if (ch.type == "Xrotation") q = q * Quaternion.AngleAxis(angle, Vector3.right);
            else if (ch.type == "Yrotation") q = q * Quaternion.AngleAxis(angle, Vector3.up);
            else if (ch.type == "Zrotation") q = q * Quaternion.AngleAxis(angle, Vector3.forward);
        }
        return q;
    }

    void UpdateBoneLines()
    {
        foreach (var kv in jointToLine)
        {
            var joint = kv.Key;
            var lr = kv.Value;
            lr.SetPosition(0, jointToTransform[joint.parent].position);
            lr.SetPosition(1, jointToTransform[joint].position);
        }
    }

    // ---------- 供 UI 调用 ----------
    public void TogglePlay() => isPlaying = !isPlaying;
    public void SetSpeed(float s) => playbackSpeed = s;
    public void Seek(float t) => CurrentTime = Mathf.Clamp(t, 0f, Data != null ? Data.Duration : 0f);
}