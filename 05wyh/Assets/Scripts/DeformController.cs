using UnityEngine;
using System.Collections.Generic;

public class DeformController : MonoBehaviour
{
    public enum DeformMode { None, Taper, Twist, Bend, FFD }
    public DeformMode currentMode = DeformMode.None;

    [Header("FFD Settings")]
    public int gridSize = 3;
    [Range(0f, 1f)] public float ffdStrength = 1f;

    [Header("Taper / Twist / Bend Parameters")]
    public float taperAmount = 0.5f;
    public float twistAngle = 45f;
    public float bendAngle = 45f;

    [Header("Runtime Mouse Interaction (Optional)")]
    public Camera mainCamera;
    public float handleRadius = 0.1f;

    private Mesh originalMesh;
    private Mesh workingMesh;

    private Vector3[] originalVertices;
    private Vector3[] workingVertices;
    private int[] triangles;

    private Vector3 boundsMin;
    private Vector3 boundsMax;
    private Vector3 boundsSize;

    public Vector3[] ffdControlPoints;
    private Vector3[] ffdControlPointsInitial;

    private Vector3[] vertexParamCoords;
    private int draggingIndex = -1;

    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("MeshFilter or mesh missing.");
            return;
        }

        originalMesh = mf.sharedMesh;

        if (!originalMesh.isReadable)
        {
            Debug.LogError($"模型 '{originalMesh.name}' 没有开启 Read/Write Enabled。请在 Project 窗口中选中模型文件，在 Inspector 的 Model 标签页下勾选 Read/Write Enabled 并点击 Apply。");
            return;
        }

        workingMesh = Instantiate(originalMesh);
        workingMesh.name = "DeformedMesh";
        mf.mesh = workingMesh;

        originalVertices = originalMesh.vertices;
        workingVertices = new Vector3[originalVertices.Length];
        System.Array.Copy(originalVertices, workingVertices, originalVertices.Length);
        triangles = originalMesh.triangles;

        CalculateBounds();
        boundsSize = boundsMax - boundsMin;
        boundsSize = Vector3.Max(boundsSize, Vector3.one * 0.001f);

        InitFFDControlPoints();
        ComputeVertexParamCoords();

        if (mainCamera == null)
            mainCamera = Camera.main;

        Debug.Log($"Model bounds: {boundsMin} ~ {boundsMax}, size: {boundsSize}");
    }

    void CalculateBounds()
    {
        if (originalVertices.Length == 0) return;
        boundsMin = originalVertices[0];
        boundsMax = originalVertices[0];
        foreach (var v in originalVertices)
        {
            boundsMin = Vector3.Min(boundsMin, v);
            boundsMax = Vector3.Max(boundsMax, v);
        }
    }

    void InitFFDControlPoints()
    {
        int n = gridSize;
        ffdControlPoints = new Vector3[n * n * n];
        ffdControlPointsInitial = new Vector3[n * n * n];

        for (int k = 0; k < n; k++)
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int idx = k * n * n + j * n + i;
                    Vector3 p = boundsMin + new Vector3(
                        boundsSize.x * i / (n - 1),
                        boundsSize.y * j / (n - 1),
                        boundsSize.z * k / (n - 1)
                    );
                    ffdControlPoints[idx] = p;
                    ffdControlPointsInitial[idx] = p;
                }
    }

    void ComputeVertexParamCoords()
    {
        vertexParamCoords = new Vector3[originalVertices.Length];
        for (int v = 0; v < originalVertices.Length; v++)
        {
            Vector3 p = originalVertices[v];
            vertexParamCoords[v] = new Vector3(
                Mathf.Clamp01((p.x - boundsMin.x) / boundsSize.x),
                Mathf.Clamp01((p.y - boundsMin.y) / boundsSize.y),
                Mathf.Clamp01((p.z - boundsMin.z) / boundsSize.z)
            );
        }
    }

    // 锥化
    void ApplyTaper()
    {
        for (int v = 0; v < originalVertices.Length; v++)
        {
            Vector3 p = originalVertices[v];
            float t = (p.y - boundsMin.y) / boundsSize.y;
            float s = Mathf.Lerp(taperAmount, 1f, t);
            workingVertices[v] = new Vector3(p.x * s, p.y, p.z * s);
        }
        UpdateMesh();
    }

    // 扭曲
    void ApplyTwist()
    {
        float totalAngleRad = twistAngle * Mathf.Deg2Rad;
        for (int v = 0; v < originalVertices.Length; v++)
        {
            Vector3 p = originalVertices[v];
            float t = (p.y - boundsMin.y) / boundsSize.y;
            float theta = totalAngleRad * t;
            float cos = Mathf.Cos(theta);
            float sin = Mathf.Sin(theta);

            workingVertices[v] = new Vector3(
                p.x * cos - p.z * sin,
                p.y,
                p.x * sin + p.z * cos
            );
        }
        UpdateMesh();
    }

    // 弯曲
    void ApplyBend()
    {
        float totalAngleRad = bendAngle * Mathf.Deg2Rad;
        if (Mathf.Abs(totalAngleRad) < 1e-5f)
        {
            System.Array.Copy(originalVertices, workingVertices, originalVertices.Length);
            UpdateMesh();
            return;
        }

        float k = totalAngleRad / boundsSize.y;
        float R = 1f / k;
        float xCenter = (boundsMin.x + boundsMax.x) * 0.5f;

        for (int v = 0; v < originalVertices.Length; v++)
        {
            Vector3 p = originalVertices[v];

            float yLocal = Mathf.Clamp(p.y - boundsMin.y, 0, boundsSize.y);

            float theta = k * yLocal;
            float sin = Mathf.Sin(theta);
            float cos = Mathf.Cos(theta);

            float dx = p.x - xCenter;


            float xNew = (xCenter - R) + (R + dx) * cos;
            float yNew = boundsMin.y + (R + dx) * sin;

            workingVertices[v] = new Vector3(xNew, yNew, p.z);
        }
        UpdateMesh();
    }

    float Bernstein(int i, int n, float t)
    {
        float comb = BinomialCoeff(n, i);
        return comb * Mathf.Pow(t, i) * Mathf.Pow(1 - t, n - i);
    }

    float BinomialCoeff(int n, int k)
    {
        if (k < 0 || k > n) return 0;
        if (k == 0 || k == n) return 1;
        float result = 1;
        for (int i = 0; i < k; i++)
            result = result * (n - i) / (i + 1);
        return result;
    }

    public void ApplyFFD()
    {
        int n = gridSize;
        int degree = n - 1;

        for (int v = 0; v < originalVertices.Length; v++)
        {
            Vector3 param = vertexParamCoords[v];
            Vector3 deformed = Vector3.zero;

            for (int k = 0; k < n; k++)
            {
                float bu = Bernstein(k, degree, param.z);
                if (Mathf.Abs(bu) < 1e-8f) continue;

                for (int j = 0; j < n; j++)
                {
                    float bv = Bernstein(j, degree, param.y);
                    if (Mathf.Abs(bv) < 1e-8f) continue;

                    for (int i = 0; i < n; i++)
                    {
                        float bs = Bernstein(i, degree, param.x);
                        if (Mathf.Abs(bs) < 1e-8f) continue;

                        int idx = k * n * n + j * n + i;
                        Vector3 ctrl = Vector3.Lerp(ffdControlPointsInitial[idx], ffdControlPoints[idx], ffdStrength);
                        deformed += ctrl * (bs * bv * bu);
                    }
                }
            }
            workingVertices[v] = deformed;
        }
        UpdateMesh();
    }

    void UpdateMesh()
    {
        workingMesh.vertices = workingVertices;
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateBounds();
    }

    public void SetMode(int modeIndex)
    {
        currentMode = (DeformMode)modeIndex;
        ResetToOriginal();
        ApplyCurrentMode();
    }

    public void ResetToOriginal()
    {
        System.Array.Copy(originalVertices, workingVertices, originalVertices.Length);
        if (ffdControlPointsInitial != null)
            System.Array.Copy(ffdControlPointsInitial, ffdControlPoints, ffdControlPoints.Length);
        UpdateMesh();
    }

    public void ApplyCurrentMode()
    {
        switch (currentMode)
        {
            case DeformMode.Taper: ApplyTaper(); break;
            case DeformMode.Twist: ApplyTwist(); break;
            case DeformMode.Bend: ApplyBend(); break;
            case DeformMode.FFD: ApplyFFD(); break;
        }
    }

    // 运行时通过鼠标拖动 FFD 控制点
    void Update()
    {
        if (currentMode != DeformMode.FFD || mainCamera == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            for (int i = 0; i < ffdControlPoints.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(ffdControlPoints[i]);
                float dist = Vector3.Cross(ray.direction, worldPos - ray.origin).magnitude;
                if (dist < handleRadius)
                {
                    draggingIndex = i;
                    break;
                }
            }
        }

        if (Input.GetMouseButton(0) && draggingIndex >= 0)
        {
            float dx = Input.GetAxis("Mouse X");
            float dy = Input.GetAxis("Mouse Y");
            Vector3 move = mainCamera.transform.right * dx + mainCamera.transform.up * dy;

            ffdControlPoints[draggingIndex] += transform.InverseTransformVector(move * 2f);
            ApplyFFD();
        }

        if (Input.GetMouseButtonUp(0))
            draggingIndex = -1;
    }
}