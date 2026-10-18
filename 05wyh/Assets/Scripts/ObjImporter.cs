using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

public class ObjImporter
{
    public static Mesh ImportFile(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("OBJ file not found: " + path);
            return null;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        Dictionary<string, int> vertexCache = new Dictionary<string, int>();

        string[] lines = File.ReadAllLines(path);
        foreach (string line in lines)
        {
            line.Trim();
            if (line.StartsWith("v "))
            {
                string[] parts = line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                vertices.Add(new Vector3(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture)));
            }
            else if (line.StartsWith("vt "))
            {
                string[] parts = line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                uvs.Add(new Vector2(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture)));
            }
            else if (line.StartsWith("vn "))
            {
                string[] parts = line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                normals.Add(new Vector3(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture)));
            }
            else if (line.StartsWith("f "))
            {
                string[] parts = line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < parts.Length; i++)
                {
                    string key = parts[i];
                    if (!vertexCache.ContainsKey(key))
                    {
                        vertexCache[key] = vertices.Count;
                        string[] indices = key.Split('/');
                        int vIdx = int.Parse(indices[0]) - 1;
                        vertices.Add(vertices[vIdx]);
                    }
                    triangles.Add(vertexCache[key]);
                }
            }
        }
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Debug.LogWarning("请根据model.obj的实际格式完善解析逻辑。");
        return mesh;
    }
}