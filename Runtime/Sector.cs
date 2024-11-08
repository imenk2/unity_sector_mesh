using System;
using UnityEngine;

 
[ExecuteAlways]
[Serializable]
public class Sector
{
    private static Sector _instance;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

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
            sd.mat.SetTexture(BaseMap, sd.texture2D);
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
            float rad = CalculateRotation(startdgree, enddgree);
            float offset = 0.5f;
            Vector2 displacement = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            var offcenter = displacement.normalized / 2f;
            if (sd.dir == SectorManager.SectorData.VertexDirection.Y)
            {
                uv[i] = new Vector2(vertices[i].x / sd.radius + 0.5f, vertices[i].z / sd.radius + 0.5f);
                if (!sd.uvdir)
                {
                    uv[i].x -= 0.5f;
                    uv[i].y -= 0.5f;
                    uv[i].x += 0.25f - offcenter.x;
                    uv[i].y -= offcenter.y;
                    uv[i].x += 0.5f;
                    uv[i].y += 0.5f;
                }
                else
                {
                    var rotate = Unity_Rotate_About_Axis_Radians_float(new Vector3(uv[i].x - offset, 0.0f, uv[i].y - offset), Vector3.up, rad);
                    uv[i].x = rotate.x + offset;
                    uv[i].y = rotate.z + offset;
                }
                uv[i].x = 1.0f - uv[i].x;//x方向朝向
                uv[i].y = 1.0f - uv[i].y;//y方向朝向
            }
            else if (sd.dir == SectorManager.SectorData.VertexDirection.Z)
            {
                uv[i] = new Vector2(vertices[i].x / sd.radius + 0.5f, vertices[i].y / sd.radius + 0.5f);
                if (!sd.uvdir)
                {
                    uv[i].x -= 0.5f;
                    uv[i].y -= 0.5f;
                    uv[i].x += 0.25f - offcenter.x;
                    uv[i].y += offcenter.y;
                    uv[i].x += 0.5f;
                    uv[i].y += 0.5f;
                }else{
                    var rotate = Unity_Rotate_About_Axis_Radians_float(new Vector3(uv[i].x - offset, uv[i].y - offset, 0.0f),Vector3.forward, rad);
                    uv[i].x = rotate.x + offset;
                    uv[i].y = rotate.y + offset;
                }
                uv[i].x = 1.0f - uv[i].x;//x方向朝向
            }
           
        }

        sd.mesh.vertices = vertices;
        sd.mesh.triangles = triangles;
        sd.mesh.uv = uv;
        sd.mesh.RecalculateBounds(); // 更新Mesh的边界
    }

    public float CalculateRotation(float start, float end)
    {
        float angleDifference;
        if (end >= start)
        {
            angleDifference = end - start;
        }
        else
        {
            angleDifference = end + (360 - start);
        }

        // 计算中点角度
        float midAngle = start + (angleDifference / 2.0f);

        // 确保中点角度在0到360度之间
        if (midAngle < 0)
        {
            midAngle += 360.0f;
        }
        else if (midAngle > 360.0f)
        {
            midAngle -= 360.0f;
        }

        // 将中点角度转换为弧度
        float rotationAngle = midAngle * Mathf.Deg2Rad;

        return rotationAngle;
    }
    
    Vector3 Unity_Rotate_About_Axis_Radians_float(Vector3 In, Vector3 Axis, float Rotation)
    {
        float s = Mathf.Sin(Rotation);
        float c = Mathf.Cos(Rotation);
        float one_minus_c = 1.0f - c;
    
        Axis = Vector3.Normalize(Axis);

        Matrix4x4 rot_mat = new Matrix4x4();
        rot_mat[0, 0] = one_minus_c * Axis.x * Axis.x + c;
        rot_mat[0, 1] = one_minus_c * Axis.x * Axis.y - Axis.z * s;
        rot_mat[0, 2] = one_minus_c * Axis.z * Axis.x + Axis.y * s;
        rot_mat[1, 0] = one_minus_c * Axis.x * Axis.y + Axis.z * s;
        rot_mat[1, 1] = one_minus_c * Axis.y * Axis.y + c;
        rot_mat[1, 2] = one_minus_c * Axis.y * Axis.z - Axis.x * s;
        rot_mat[2, 0] = one_minus_c * Axis.z * Axis.x - Axis.y * s;
        rot_mat[2, 1] = one_minus_c * Axis.y * Axis.z + Axis.x * s;
        rot_mat[2, 2] = one_minus_c * Axis.z * Axis.z + c;
        rot_mat.SetColumn(3, new Vector4(0, 0, 0, 1));
        return rot_mat.MultiplyPoint(In);
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