using System;
using UnityEngine;

[ExecuteAlways]
[Serializable]
public class Sector
{
    private static Sector _instance;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    public static Sector Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Sector();
            }

            return _instance;
        }
    }
   
    public void UpdateMesh(SectorManager.SectorData sd)
    {
        if (sd.mat)
        {
            sd.mat.SetColor(BaseColor, sd.color);
        }

        CreateMesh(sd);
    }

    void CreateMesh(SectorManager.SectorData sd)
    {
        // 确保起始角度小于结束角度
        var startdgree = sd.startdgree;
        var enddgree = sd.enddgree;
        if (startdgree > enddgree)
        {
            (startdgree, enddgree) = (enddgree, startdgree);
        }

        // 计算总角度
        float angledegree = enddgree - startdgree;

        // 计算segments，确保至少为4，且足够细分以支持1度的面
        var segments = 16;//Mathf.Max(Mathf.CeilToInt(angledegree / 30f * 4), 4);

        int vertices_count = segments * 2 + 2; // 外圆和内圆的顶点
        int triangle_count = (segments - 1) * 6;

        // 负载属性与mesh
        if (!sd.mesh)
        {
            sd.mesh = new Mesh();
        }

        Vector3[] vertices = sd.mesh.vertices;
        if (ArrayCheck(sd.mesh.vertices, vertices_count))
        {
            vertices = new Vector3[vertices_count];
        }

        float startAngleRad = Mathf.Deg2Rad * sd.startdgree;
        float endAngleRad = Mathf.Deg2Rad * sd.enddgree;
        float angleCur = startAngleRad;
        float angledelta = (endAngleRad - startAngleRad) / (segments - 1);
        for (int i = 0; i < vertices_count; i += 2)
        {
            float cosA = Mathf.Cos(angleCur);
            float sinA = Mathf.Sin(angleCur);
            if (sd.dir == SectorManager.SectorData.VertexDirection.Y)
            {
                vertices[i] = new Vector3(sd.radius * cosA, 0, sd.radius * sinA);
                vertices[i + 1] = new Vector3(sd.innerradius * cosA, 0, sd.innerradius * sinA);
            }
            else if (sd.dir == SectorManager.SectorData.VertexDirection.Z)
            {
                vertices[i] = new Vector3(sd.radius * cosA, -sd.radius * sinA, 0);
                vertices[i + 1] = new Vector3(sd.innerradius * cosA, -sd.innerradius * sinA, 0);
            }      
            angleCur += angledelta;
        }

        int[] triangles = sd.mesh.triangles;
        if (ArrayCheck(sd.mesh.triangles, triangle_count))
        {
            triangles = new int[triangle_count];
        }

        for (int i = 0, vi = 0; i < triangle_count; i += 6, vi += 2)
        {
            triangles[i + 0] = vi; // 外圆经
            triangles[i + 1] = vi + 1; // 内圆心
            triangles[i + 2] = vi + 3; // 外圆经
            triangles[i + 3] = vi + 3;// 内圆心
            triangles[i + 4] = vi + 2;
            triangles[i + 5] = vi;
        }

        Vector2[] uv = sd.mesh.uv;
        if (ArrayCheck(sd.mesh.uv, vertices_count))
        {
            uv = new Vector2[vertices_count];
        }

        for (int i = 0; i < vertices_count; i++)
        {
            if (sd.dir == SectorManager.SectorData.VertexDirection.Y)
            {
                uv[i] = new Vector2(vertices[i].x / sd.radius + 0.5f, vertices[i].z / sd.radius + 0.5f);
            }
            else if (sd.dir == SectorManager.SectorData.VertexDirection.Z)
            {
                uv[i] = new Vector2(vertices[i].x / sd.radius + 0.5f, vertices[i].y / sd.radius + 0.5f);
            }
        }

        sd.mesh.vertices = vertices;
        sd.mesh.triangles = triangles;
        sd.mesh.uv = uv;
        sd.mesh.RecalculateBounds(); // 更新Mesh的边界
    }

    bool ArrayCheck<T>(T[] array, int count)
    {
        if (array == null)
        {
            return true;
        }

        if (array.Length != count)
        {
            return true;
        }

        return false;
    }
}