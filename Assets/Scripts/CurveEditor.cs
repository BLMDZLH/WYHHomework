using System.Collections.Generic;
using UnityEngine;

public enum CurveType
{
    Bezier,
    BSpline,
    CatmullRom
}

public class CurveEditor : MonoBehaviour
{
    [Header("基础设置")]
    public GameObject pointPrefab;
    public int sampleCount = 120;
    public CurveType currentType = CurveType.Bezier;

    [Header("显示设置")]
    public bool showControlLines = true;
    public Color curveColor = Color.blue;
    public Color lineColor = Color.gray;

    [Header("渲染器引用")]
    public LineRenderer curveRenderer;
    public LineRenderer controlLineRenderer;

    private List<ControlPoint> controlPoints = new List<ControlPoint>();
    private ControlPoint selectedPoint;

    void Awake()
    {
        if (curveRenderer != null)
        {
            curveRenderer.startWidth = 0.05f;
            curveRenderer.endWidth = 0.05f;
            curveRenderer.material = new Material(Shader.Find("Sprites/Default"));
            curveRenderer.startColor = curveColor;
            curveRenderer.endColor = curveColor;
            curveRenderer.positionCount = 0;
            curveRenderer.receiveShadows = false;
            curveRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            curveRenderer.useWorldSpace = true;
        }

        if (controlLineRenderer != null)
        {
            controlLineRenderer.startWidth = 0.02f;
            controlLineRenderer.endWidth = 0.02f;
            controlLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            controlLineRenderer.startColor = lineColor;
            controlLineRenderer.endColor = lineColor;
            controlLineRenderer.positionCount = 0;
            controlLineRenderer.receiveShadows = false;
            controlLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            controlLineRenderer.useWorldSpace = true;
        }
    }

    void Start()
    {
        InitDefaultPoints();
        UpdateCurve();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            AddPoint(mousePos);
            UpdateCurve();
        }
    }

    void InitDefaultPoints()
    {
        Vector2[] defaultPos = new Vector2[]
        {
            new Vector2(-3, -1),
            new Vector2(-1, 2),
            new Vector2(1, -2),
            new Vector2(3, 1)
        };

        foreach (var pos in defaultPos)
        {
            AddPoint(pos);
        }
    }

    public void AddPoint(Vector2 position)
    {
        GameObject point = Instantiate(pointPrefab, position, Quaternion.identity, transform);
        ControlPoint cp = point.GetComponent<ControlPoint>();
        cp.editor = this;
        controlPoints.Add(cp);
    }

    public void DeleteSelectedPoint()
    {
        if (selectedPoint == null || controlPoints.Count <= 2) return;
        controlPoints.Remove(selectedPoint);
        Destroy(selectedPoint.gameObject);
        selectedPoint = null;
        UpdateCurve();
    }

    public void SelectPoint(ControlPoint point)
    {
        selectedPoint = point;
    }

    public void UpdateCurve()
    {
        List<Vector2> points = new List<Vector2>();
        foreach (var cp in controlPoints)
        {
            points.Add(cp.transform.position);
        }

        List<Vector2> curveSamples = new List<Vector2>();
        switch (currentType)
        {
            case CurveType.Bezier:
                curveSamples = CurveMath.SampleBezier(points, sampleCount);
                break;
            case CurveType.BSpline:
                curveSamples = CurveMath.SampleBSpline(points, sampleCount);
                break;
            case CurveType.CatmullRom:
                curveSamples = CurveMath.SampleCatmullRom(points, sampleCount);
                break;
        }

        if (curveRenderer != null)
        {
            curveRenderer.positionCount = curveSamples.Count;
            for (int i = 0; i < curveSamples.Count; i++)
            {
                curveRenderer.SetPosition(i, curveSamples[i]);
            }
        }

        if (controlLineRenderer != null)
        {
            if (showControlLines)
            {
                controlLineRenderer.positionCount = points.Count;
                for (int i = 0; i < points.Count; i++)
                {
                    controlLineRenderer.SetPosition(i, points[i]);
                }
            }
            else
            {
                controlLineRenderer.positionCount = 0;
            }
        }
    }

    public void SwitchCurveType(int typeIndex)
    {
        currentType = (CurveType)typeIndex;
        UpdateCurve();
    }

    public void ElevateBezierDegree()
    {
        if (currentType != CurveType.Bezier) return;

        List<Vector2> oldPoints = new List<Vector2>();
        foreach (var cp in controlPoints)
        {
            oldPoints.Add(cp.transform.position);
        }

        List<Vector2> newPoints = CurveMath.BezierElevateDegree(oldPoints);

        foreach (var cp in controlPoints)
        {
            Destroy(cp.gameObject);
        }
        controlPoints.Clear();

        foreach (var pos in newPoints)
        {
            AddPoint(pos);
        }

        UpdateCurve();
    }

    public void ResetCurve()
    {
        foreach (var cp in controlPoints)
        {
            Destroy(cp.gameObject);
        }
        controlPoints.Clear();
        InitDefaultPoints();
        UpdateCurve();
    }
}
