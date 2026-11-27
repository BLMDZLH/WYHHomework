using UnityEngine;

public class RopeController : MonoBehaviour
{
    [Header("绳索设置")]
    public GameObject segmentPrefab;   // 拖入 RopeSegment 预制体
    public int segmentCount = 20;      // 链节数量
    public float segmentSpacing = 0.25f; // 链节间距
    public Transform startAnchor;      // 绳索起点（可留空，默认在当前位置生成）

    [Header("关节限制参数")]
    public float maxAngleLimit = 45f;  // 链节之间允许弯曲的最大角度（度）

    private Rigidbody[] segments;

    void Start()
    {
        GenerateRope();
    }

    void GenerateRope()
    {
        segments = new Rigidbody[segmentCount];
        Vector3 spawnPos = startAnchor != null ? startAnchor.position : transform.position;

        for (int i = 0; i < segmentCount; i++)
        {
            // 沿 Y 轴向下生成链节
            Vector3 pos = spawnPos + Vector3.down * i * segmentSpacing;
            GameObject seg = Instantiate(segmentPrefab, pos, Quaternion.identity, transform);
            seg.name = $"RopeSegment_{i}";

            Rigidbody rb = seg.GetComponent<Rigidbody>();
            if (rb == null) rb = seg.AddComponent<Rigidbody>(); // 确保有刚体组件

            segments[i] = rb;

            // 第一个链节设置为运动学（固定在天花板）
            if (i == 0)
            {
                rb.isKinematic = true;
            }
            else
            {
                // 设置物理属性，减轻质量以防止拉扯变形过度
                rb.mass = 1f;
                rb.drag = 0.05f; // 阻力
                rb.angularDrag = 0.05f;
                rb.interpolation = RigidbodyInterpolation.Interpolate; // 平滑插值
            }
        }

        // 为每个链节添加 ConfigurableJoint，连接前一个链节
        for (int i = 1; i < segmentCount; i++)
        {
            ConfigurableJoint joint = segments[i].gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = segments[i - 1];

            // 1. 锁定线性位移（防止链条被无限拉长，像绳子一样有长度限制）
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            // 2. 允许角度旋转（核心修复点：这是让链条能弯曲的关键）
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;

            // 3. 设置角度限制范围（允许链节之间有一定角度弯曲）
            SoftJointLimit limit = new SoftJointLimit();
            limit.limit = maxAngleLimit;
            joint.lowAngularXLimit = limit;
            joint.highAngularXLimit = limit;
            joint.angularYLimit = limit;
            joint.angularZLimit = limit;

            // 自动配置锚点（确保连接位置正确）
            joint.autoConfigureConnectedAnchor = true;
        }

        // 添加一个初始的轻微侧向推力，让绳索开始摆动（否则可能会因为绝对垂直而静止）
        if (segments.Length > 2)
        {
            segments[segments.Length / 2].AddForce(new Vector3(2f, 0, 0), ForceMode.Impulse);
        }
    }
}