using UnityEngine;

public class QuaternionDemoController : MonoBehaviour
{
    [Header("旋转参数设置")]
    public Vector3 startEuler = new Vector3(0, 0, 0);
    public Vector3 endEuler = new Vector3(180, 180, 180);
    public float rotationSpeed = 45f;

    [Header("引用")]
    public Transform eulerCube;
    public Transform quaternionCube;

    private float t = 0f;
    private float totalAngle;
    private float accumulatedAngle = 0f;
    private Quaternion startQuat;
    private Quaternion endQuat;

    void Start()
    {
        totalAngle = (endEuler - startEuler).magnitude;

        eulerCube.rotation = Quaternion.Euler(startEuler);
        quaternionCube.rotation = Quaternion.Euler(startEuler);

        startQuat = Quaternion.Euler(startEuler);
        endQuat = Quaternion.Euler(endEuler);
    }

    void Update()
    {
        if (accumulatedAngle >= totalAngle) return;

        Vector3 deltaEuler = (endEuler - startEuler).normalized
                             * rotationSpeed * Time.deltaTime;
        Vector3 currentEuler = eulerCube.eulerAngles;
        eulerCube.eulerAngles = currentEuler + deltaEuler;

        accumulatedAngle += rotationSpeed * Time.deltaTime;
        t = Mathf.Clamp01(accumulatedAngle / totalAngle);
        quaternionCube.rotation = Quaternion.Slerp(startQuat, endQuat, t);
    }

    public void ResetDemo()
    {
        accumulatedAngle = 0f;
        t = 0f;
        eulerCube.rotation = Quaternion.Euler(startEuler);
        quaternionCube.rotation = Quaternion.Euler(startEuler);
        startQuat = Quaternion.Euler(startEuler);
        endQuat = Quaternion.Euler(endEuler);
    }
}