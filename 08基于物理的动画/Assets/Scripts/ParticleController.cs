using UnityEngine;

public class ParticleController : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private int maxParticles = 1000;

    [Header("发射设置")]
    public float emitInterval = 0.1f;   // 发射间隔
    public int emitCount = 10;          // 每次发射数量
    private float timer = 0f;

    [Header("风力影响")]
    public Vector3 windForce = new Vector3(2f, 0f, 0f); // 模拟风的方向和强度

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[maxParticles];
    }

    void Update()
    {
        // 定时发射粒子
        timer += Time.deltaTime;
        if (timer >= emitInterval)
        {
            timer = 0f;
            ps.Emit(emitCount);
        }

        // 每帧获取存活粒子，施加风力
        int aliveCount = ps.GetParticles(particles);
        for (int i = 0; i < aliveCount; i++)
        {
            // 叠加风力到粒子速度上
            particles[i].velocity += windForce * Time.deltaTime;
        }

        // 将修改后的粒子数据写回系统[reference:1]
        if (aliveCount > 0)
            ps.SetParticles(particles, aliveCount);
    }
}