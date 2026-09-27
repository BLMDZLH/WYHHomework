using UnityEngine;

public class ControlPoint : MonoBehaviour
{
    private Camera mainCam;
    private bool isDragging;
    public CurveEditor editor; // 曲线编辑器引用

    void Start()
    {
        mainCam = Camera.main;
    }

    void OnMouseDown()
    {
        isDragging = true;
        editor.SelectPoint(this);
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        // 鼠标屏幕坐标转世界坐标
        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        transform.position = mousePos;

        // 通知编辑器重新计算曲线
        editor.UpdateCurve();
    }

    void OnMouseUp()
    {
        isDragging = false;
    }
}
