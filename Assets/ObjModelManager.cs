using UnityEngine;
using System.IO;

public class ObjModelManager : MonoBehaviour
{
    [Header("OBJ文件路径")]
    [Tooltip("留空则自动加载StreamingAssets文件夹下的test.obj")]
    public string objFilePath;

    // 公开顶点数据，后续扩展功能可直接读写
    public Vector3[] Vertices { get; private set; }
    public int[] Triangles { get; private set; }
    public Vector3[] Normals { get; private set; }

    private GameObject _modelObject;
    private MeshFilter _meshFilter;

    void Start()
    {
        LoadModel();
    }

    /// <summary>
    /// 加载OBJ模型并生成场景物体
    /// </summary>
    public void LoadModel()
    {
        // 路径为空则默认加载StreamingAssets下的test.obj
        if (string.IsNullOrEmpty(objFilePath))
        {
            objFilePath = Path.Combine(Application.streamingAssetsPath, "test.obj");
        }

        Mesh mesh = ObjLoader.LoadFromFile(objFilePath);
        if (mesh == null) return;

        // 缓存顶点数据，供外部访问修改
        Vertices = mesh.vertices;
        Triangles = mesh.triangles;
        Normals = mesh.normals;

        // 销毁旧模型，创建新模型物体
        if (_modelObject != null) Destroy(_modelObject);
        _modelObject = new GameObject("LoadedOBJModel");
        _modelObject.transform.position = Vector3.zero;

        // 添加网格渲染组件
        _meshFilter = _modelObject.AddComponent<MeshFilter>();
        _meshFilter.mesh = mesh;

        MeshRenderer renderer = _modelObject.AddComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Standard"));
    }

    /// <summary>
    /// 示例：修改指定顶点的位置（演示顶点可编辑，便于后续扩展）
    /// </summary>
    public void ModifyVertex(int index, Vector3 offset)
    {
        if (Vertices == null || index < 0 || index >= Vertices.Length) return;

        Vertices[index] += offset;
        _meshFilter.mesh.vertices = Vertices;
        _meshFilter.mesh.RecalculateBounds();
        _meshFilter.mesh.RecalculateNormals();
    }
}
