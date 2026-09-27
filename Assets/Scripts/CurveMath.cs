using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 参数曲线数学计算库
/// 包含贝塞尔、B样条、Catmull-Rom等曲线的核心算法
/// </summary>
public static class CurveMath
{
    #region 基础工具
    // 二维向量线性插值
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
    {
        return a + (b - a) * t;
    }
    #endregion

    #region 贝塞尔曲线（Bezier Curve）
    /// <summary>
    /// 德卡斯特里奥算法 - 计算n次贝塞尔曲线上t位置的点
    /// 课件核心算法，支持任意数量控制点
    /// </summary>
    /// <param name="controlPoints">控制点列表</param>
    /// <param name="t">参数t，范围[0,1]</param>
    public static Vector2 DeCasteljau(List<Vector2> controlPoints, float t)
    {
        int n = controlPoints.Count;
        if (n == 0) return Vector2.zero;
        if (n == 1) return controlPoints[0];

        // 复制控制点用于迭代计算
        List<Vector2> points = new List<Vector2>(controlPoints);

        // 逐层迭代插值（对应课件中的递推过程）
        for (int k = 1; k < n; k++)
        {
            for (int i = 0; i < n - k; i++)
            {
                points[i] = Lerp(points[i], points[i + 1], t);
            }
        }
        return points[0];
    }

    /// <summary>
    /// 贝塞尔曲线升阶：n次 → n+1次，曲线形状保持不变
    /// 实现曲线转换的核心功能
    /// </summary>
    public static List<Vector2> BezierElevateDegree(List<Vector2> controlPoints)
    {
        int n = controlPoints.Count - 1; // 当前曲线次数
        List<Vector2> newPoints = new List<Vector2>(n + 2);

        newPoints.Add(controlPoints[0]); // 首端点不变

        // 计算中间新增控制点
        for (int i = 1; i <= n; i++)
        {
            Vector2 p = (i / (float)(n + 1)) * controlPoints[i - 1]
                      + ((n + 1 - i) / (float)(n + 1)) * controlPoints[i];
            newPoints.Add(p);
        }

        newPoints.Add(controlPoints[n]); // 末端点不变
        return newPoints;
    }

    /// <summary>
    /// 采样贝塞尔曲线，获得离散点用于渲染
    /// </summary>
    /// <param name="controlPoints">控制点</param>
    /// <param name="sampleCount">采样数量，越大曲线越平滑</param>
    public static List<Vector2> SampleBezier(List<Vector2> controlPoints, int sampleCount = 100)
    {
        List<Vector2> samples = new List<Vector2>();
        float step = 1f / sampleCount;
        for (int i = 0; i <= sampleCount; i++)
        {
            float t = i * step;
            samples.Add(DeCasteljau(controlPoints, t));
        }
        return samples;
    }
    #endregion

    #region 均匀B样条曲线（B-Spline）
    /// <summary>
    /// 均匀二次B样条曲线采样
    /// </summary>
    public static List<Vector2> SampleBSpline(List<Vector2> controlPoints, int sampleCount = 100)
    {
        List<Vector2> samples = new List<Vector2>();
        int n = controlPoints.Count;
        if (n < 3) return samples;

        float step = 1f / sampleCount;
        // 每3个控制点生成一段曲线
        for (int i = 0; i <= n - 3; i++)
        {
            for (int j = 0; j <= sampleCount; j++)
            {
                float t = j * step;
                float t2 = t * t;

                // 二次B样条基函数
                float b0 = 0.5f * (1 - 2 * t + t2);
                float b1 = 0.5f * (1 + 2 * t - 2 * t2);
                float b2 = 0.5f * t2;

                Vector2 point = b0 * controlPoints[i]
                              + b1 * controlPoints[i + 1]
                              + b2 * controlPoints[i + 2];
                samples.Add(point);
            }
        }
        return samples;
    }
    #endregion

    #region Catmull-Rom 样条
    /// <summary>
    /// Catmull-Rom样条采样，曲线经过所有控制点
    /// </summary>
    public static List<Vector2> SampleCatmullRom(List<Vector2> controlPoints, int sampleCount = 100)
    {
        List<Vector2> samples = new List<Vector2>();
        int n = controlPoints.Count;
        if (n < 4) return samples;

        float step = 1f / sampleCount;
        for (int i = 1; i < n - 2; i++)
        {
            Vector2 p0 = controlPoints[i - 1];
            Vector2 p1 = controlPoints[i];
            Vector2 p2 = controlPoints[i + 1];
            Vector2 p3 = controlPoints[i + 2];

            for (int j = 0; j <= sampleCount; j++)
            {
                float t = j * step;
                float t2 = t * t;
                float t3 = t2 * t;

                Vector2 point = 0.5f * (
                    (-t3 + 2 * t2 - t) * p0 +
                    (3 * t3 - 5 * t2 + 2) * p1 +
                    (-3 * t3 + 4 * t2 + t) * p2 +
                    (t3 - t2) * p3
                );
                samples.Add(point);
            }
        }
        return samples;
    }
    #endregion
}
