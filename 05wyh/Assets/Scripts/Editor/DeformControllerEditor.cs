using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DeformController))]
public class DeformControllerEditor : Editor
{
    void OnSceneGUI()
    {
        DeformController ctrl = (DeformController)target;
        if (ctrl.ffdControlPoints == null || ctrl.ffdControlPoints.Length == 0) return;

        if (ctrl.currentMode != DeformController.DeformMode.FFD) return;

        int n = ctrl.gridSize;

        Handles.color = new Color(0f, 1f, 1f, 0.6f);

        for (int k = 0; k < n; k++)
        {
            for (int j = 0; j < n; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    int idx = k * n * n + j * n + i;
                    Vector3 p = ctrl.ffdControlPoints[idx];

                    if (i < n - 1)
                    {
                        int idxX = k * n * n + j * n + (i + 1);
                        Handles.DrawLine(p, ctrl.ffdControlPoints[idxX]);
                    }
                    if (j < n - 1)
                    {
                        int idxY = k * n * n + (j + 1) * n + i;
                        Handles.DrawLine(p, ctrl.ffdControlPoints[idxY]);
                    }
                    if (k < n - 1)
                    {
                        int idxZ = (k + 1) * n * n + j * n + i;
                        Handles.DrawLine(p, ctrl.ffdControlPoints[idxZ]);
                    }
                }
            }
        }

        Handles.color = Color.yellow;
        float handleSize = HandleUtility.GetHandleSize(Vector3.zero) * 0.05f;

        for (int idx = 0; idx < ctrl.ffdControlPoints.Length; idx++)
        {
            Vector3 oldPos = ctrl.ffdControlPoints[idx];
            var fmh_60_17_639278661478203811 = Quaternion.identity; Vector3 newPos = Handles.FreeMoveHandle(
                oldPos,
                handleSize,
                Vector3.one * 0.01f,
                Handles.SphereHandleCap
            );

            if (newPos != oldPos)
            {
                Undo.RecordObject(ctrl, "Move FFD Control Point");
                ctrl.ffdControlPoints[idx] = newPos;
                ctrl.ApplyFFD();
            }
        }
    }
}