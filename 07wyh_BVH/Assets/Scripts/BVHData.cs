using System.Collections.Generic;
using UnityEngine;

public class BVHChannel
{
    public string type;   // Xposition / Yposition / Zposition / Xrotation / Yrotation / Zrotation
    public int index;     // 该通道在每帧数据数组中的全局下标
}

public class BVHJoint
{
    public string name;
    public Vector3 offset;                              // 来自 OFFSET
    public List<BVHChannel> channels = new List<BVHChannel>();
    public BVHJoint parent;
    public List<BVHJoint> children = new List<BVHJoint>();

    public Transform transform;                         // 运行时对应的场景 Transform
}

public class BVHData
{
    public int frameCount;
    public float frameTime;                             // 每帧秒数
    public List<BVHJoint> joints = new List<BVHJoint>();
    public List<float[]> frames = new List<float[]>();  // 每帧一个 float 数组

    public float Duration => frameCount * frameTime;
}