using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

public static class ObjLoader
{
    /// <summary>
    /// 从本地路径加载并解析OBJ文件，返回构建好的Unity网格
    /// </summary>
    public static Mesh LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("OBJ文件不存在：" + filePath);
            return null;
        }

        // 原始数据容器（对应OBJ文件的四类核心数据）
        List<Vector3> vertexList = new List<Vector3>();   // 顶点坐标 v
        List<Vector3> normalList = new List<Vector3>();   // 法线向量 vn
        List<Vector2> uvList = new List<Vector2>();       // 纹理坐标 vt

        // 最终Mesh输出数据
        List<Vector3> outVertices = new List<Vector3>();
        List<Vector3> outNormals = new List<Vector3>();
        List<Vector2> outUvs = new List<Vector2>();
        List<int> triangleIndices = new List<int>();      // 三角面索引

        // 逐行解析OBJ文件
        string[] allLines = File.ReadAllLines(filePath);
        foreach (string line in allLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] parts = line.Split(' ');
            if (parts.Length == 0) continue;

            switch (parts[0])
            {
                // 解析顶点坐标：v x y z
                case "v":
                    Vector3 vertex = new Vector3(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)
                    );
                    vertexList.Add(vertex);
                    break;

                // 解析法线向量：vn x y z
                case "vn":
                    Vector3 normal = new Vector3(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)
                    );
                    normalList.Add(normal);
                    break;

                // 解析纹理坐标：vt u v
                case "vt":
                    Vector2 uv = new Vector2(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture)
                    );
                    uvList.Add(uv);
                    break;

                // 解析面：f v/vt/vn v/vt/vn ...
                case "f":
                    int firstVertexIndex = outVertices.Count;
                    int validVertexCount = 0;

                    // 先添加该面所有顶点数据
                    for (int i = 1; i < parts.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(parts[i])) continue;
                        string[] indexParts = parts[i].Split('/');

                        // OBJ索引从1开始，转换为Unity的0基索引
                        int vIdx = int.Parse(indexParts[0]) - 1;
                        int vtIdx = -1;
                        int vnIdx = -1;

                        if (indexParts.Length > 1 && !string.IsNullOrEmpty(indexParts[1]))
                            vtIdx = int.Parse(indexParts[1]) - 1;
                        if (indexParts.Length > 2 && !string.IsNullOrEmpty(indexParts[2]))
                            vnIdx = int.Parse(indexParts[2]) - 1;

                        outVertices.Add(vertexList[vIdx]);
                        outUvs.Add(vtIdx >= 0 ? uvList[vtIdx] : Vector2.zero);
                        outNormals.Add(vnIdx >= 0 ? normalList[vnIdx] : Vector3.zero);
                        validVertexCount++;
                    }

                    // 扇形三角化：将任意多边形拆分为多个三角形（对应课件"渲染时拆分为三角形"知识点）
                    for (int i = 1; i < validVertexCount - 1; i++)
                    {
                        triangleIndices.Add(firstVertexIndex);
                        triangleIndices.Add(firstVertexIndex + i);
                        triangleIndices.Add(firstVertexIndex + i + 1);
                    }
                    break;
            }
        }

        // 构建Unity Mesh对象
        Mesh mesh = new Mesh();
        mesh.name = Path.GetFileNameWithoutExtension(filePath);
        mesh.vertices = outVertices.ToArray();
        mesh.normals = outNormals.ToArray();
        mesh.uv = outUvs.ToArray();
        mesh.triangles = triangleIndices.ToArray();

        // 自动计算边界与法线（兼容无法线的OBJ文件）
        mesh.RecalculateBounds();
        if (normalList.Count == 0) mesh.RecalculateNormals();

        Debug.Log($"模型加载完成 | 顶点数：{outVertices.Count} | 三角面数：{triangleIndices.Count / 3}");
        return mesh;
    }
}
