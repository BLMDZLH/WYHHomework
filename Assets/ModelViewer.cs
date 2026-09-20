using UnityEngine;

public class ModelViewer : MonoBehaviour
{
    [Header("浏览目标")]
    public Transform target;

    [Header("交互参数")]
    public float rotateSpeed = 6f;
    public float zoomSpeed = 2.5f;
    public float minDistance = 1.5f;
    public float maxDistance = 20f;

    private float _distance;
    private float _xRot;
    private float _yRot;
    private Vector3 _targetPos;

    void Start()
    {
        _targetPos = target != null ? target.position : Vector3.zero;
        _distance = Vector3.Distance(transform.position, _targetPos);
        UpdateCameraPose();
    }

    void Update()
    {
        // 鼠标左键拖拽：旋转视角
        if (Input.GetMouseButton(0))
        {
            _yRot += Input.GetAxis("Mouse X") * rotateSpeed;
            _xRot -= Input.GetAxis("Mouse Y") * rotateSpeed;
            _xRot = Mathf.Clamp(_xRot, -89f, 89f);
        }

        // 鼠标滚轮：缩放距离
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        _distance -= scroll * zoomSpeed;
        _distance = Mathf.Clamp(_distance, minDistance, maxDistance);

        UpdateCameraPose();
    }

    void UpdateCameraPose()
    {
        Quaternion rot = Quaternion.Euler(_xRot, _yRot, 0);
        transform.position = _targetPos + rot * Vector3.back * _distance;
        transform.LookAt(_targetPos);
    }
}
