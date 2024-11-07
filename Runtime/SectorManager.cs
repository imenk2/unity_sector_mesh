using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class SectorManager : MonoBehaviour
{
    [System.Serializable]
    public class SectorData
    {
        public enum VertexDirection
        {
            Y = 0,
            Z = 1
        }

        public VertexDirection dir = VertexDirection.Z;
        public Color color = Color.white;
        public float radius = 1.0f;
        public float innerradius = 0;
        public float startdgree = 0;
        public float enddgree = 30;    
        
        private VertexDirection c_dir = VertexDirection.Z;
        private Color c_color = Color.white;
        private float c_radius = 1.0f;
        private float c_innerradius = 0;
        private float c_startdgree = 0;
        private float c_enddgree = 30;
        
        public Material mat;
        public Mesh mesh;
        public GameObject go;

        public bool NeedUpdate
        {
            get
            {
                var a = CompareFloats(c_radius, radius);
                var b = CompareFloats(c_innerradius, innerradius);
                var c = CompareFloats(c_startdgree, startdgree);
                var d = CompareFloats(c_enddgree, enddgree);
                var e = c_color != color;
                var f = c_dir != dir;
                var need =  a || b || c || d || e || f;
                if (need)
                {
                    c_radius = radius;
                    c_innerradius = innerradius;
                    c_startdgree = startdgree;
                    c_enddgree = enddgree;
                    c_color = color;
                    c_dir = dir;
                }

                return need;
            }
        }

        public SectorData()
        {
            
        }
        public SectorData(Color color, float radius, float innerradius, float startdgree, float enddgree, Material mat, Renderer renderer, Mesh mesh)
        {
            this.color = color;
            this.radius = radius;
            this.innerradius = innerradius;
            this.startdgree = startdgree;
            this.enddgree = enddgree;
            this.mat = mat;
            this.mesh = mesh;
        }
        
        public bool CompareFloats(float a, float b, float tolerance = 0.0001f)
        {
            return Mathf.Abs(a - b) > tolerance;
        }

    }
    
    public List<SectorData> m_SectorDatas = new List<SectorData>();
    private List<GameObject> m_CacheGameobject = new List<GameObject>();
    private static Sector _sector;
    private static Material m_SectorMat;
    private static int m_CacheCount = 0;
    void Start()
    {
        //Init();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_CacheCount != m_SectorDatas.Count)
        {
            m_CacheCount = m_SectorDatas.Count;
            if(DisPoseGameObject())
                RedistributeSectors();
        }

        ChangeSectores(m_SectorDatas.Count > 2);
       
    }

    private void OnValidate()
    {
         
    }

    [ContextMenu("初始化4个")]
    void Init()
    {
        SectorInstanceAll();
        m_SectorDatas.Clear();
        m_CacheGameobject.Clear();
        for (int i = 0; i < 4; i++)
        {
            var sectorObj = new GameObject("sector " + i);
            sectorObj.transform.parent = transform;
            sectorObj.transform.localPosition = Vector3.zero;
            var meshFilter = sectorObj.AddComponent<MeshFilter>();
            var meshRenderer = sectorObj.AddComponent<MeshRenderer>();
            SectorData data = new SectorData();
            data.mat = Instantiate(m_SectorMat);
            var c = Color.Lerp(Color.red, Color.yellow, i / 4f);
            data.color = c;
            data.radius = 1.0f;
            data.innerradius = 0.0f;
            data.dir = SectorData.VertexDirection.Z;
            data.startdgree = i * 90;
            data.enddgree = (i + 1) * 90;
            _sector.UpdateMesh(data);
            meshRenderer.sharedMaterial = data.mat;
            meshFilter.sharedMesh = data.mesh;
            data.go = sectorObj;
            m_SectorDatas.Add(data);
            m_CacheGameobject.Add(sectorObj);
        }

        m_CacheCount = m_SectorDatas.Count;
        RedistributeSectors();
    }

    [ContextMenu("创建")]
    public void CreateSector()
    {
        SectorInstanceAll();
        var sectorObj = new GameObject("sector " + (m_SectorDatas.Count));
        sectorObj.transform.parent = this.transform;
        sectorObj.transform.localPosition = Vector3.zero;
        var meshFilter = sectorObj.AddComponent<MeshFilter>();
        var meshRenderer = sectorObj.AddComponent<MeshRenderer>();
        SectorData data = new SectorData();
        data.mat = Instantiate(m_SectorMat);
        data.color = Color.white;
        data.radius = 1.0f;
        data.innerradius = 0.0f;
        data.dir = SectorData.VertexDirection.Z;
        data.startdgree = 0;
        data.enddgree = 30;
        _sector.UpdateMesh(data);
        meshFilter.sharedMesh = data.mesh;
        meshRenderer.sharedMaterial = data.mat;
        data.go = sectorObj;
        m_SectorDatas.Add(data);
        m_CacheGameobject.Add(sectorObj);
        m_CacheCount = m_SectorDatas.Count;
        RedistributeSectors();
    }

    void SectorInstanceAll()
    {
        _sector = Sector.Instance;
        var shader = Shader.Find("Unlit");
        if (!shader) return;
        
        if (!m_SectorMat)
        {
            m_SectorMat = CoreUtils.CreateEngineMaterial(shader);
        }
    }
    public void ChangeSectores(bool three)
    {
        if (three)
        {
            for (int i = 1; i < m_SectorDatas.Count - 1; i++)
            {
                if (m_SectorDatas[i].NeedUpdate)
                {
                    m_SectorDatas[i - 1].enddgree = m_SectorDatas[i].startdgree;
                    m_SectorDatas[i + 1].startdgree = m_SectorDatas[i].enddgree;
                    _sector.UpdateMesh(m_SectorDatas[i - 1]);
                    _sector.UpdateMesh(m_SectorDatas[i + 1]);
                    _sector.UpdateMesh(m_SectorDatas[i]);
                }
            }

            //第一个扇形
            if (m_SectorDatas[0].NeedUpdate)
            {
                m_SectorDatas[1].startdgree = m_SectorDatas[0].enddgree;//下一个
                _sector.UpdateMesh(m_SectorDatas[1]);
                m_SectorDatas[m_SectorDatas.Count - 1].enddgree = m_SectorDatas[0].startdgree + 360;//上一个, 回到末尾
                _sector.UpdateMesh(m_SectorDatas[m_SectorDatas.Count - 1]);
                _sector.UpdateMesh(m_SectorDatas[0]);
            }
            //最后一个扇形
            if (m_SectorDatas[m_SectorDatas.Count - 1].NeedUpdate)
            {
                m_SectorDatas[0].startdgree = m_SectorDatas[m_SectorDatas.Count - 1].enddgree - 360;//下一个，回到起点
                _sector.UpdateMesh(m_SectorDatas[m_SectorDatas.Count - 1]);
                m_SectorDatas[m_SectorDatas.Count - 2].enddgree = m_SectorDatas[m_SectorDatas.Count - 1].startdgree;//上一个
                _sector.UpdateMesh(m_SectorDatas[m_SectorDatas.Count - 2]);
                _sector.UpdateMesh(m_SectorDatas[0]);
            }
        }
        else
        {
            //2
            if (m_SectorDatas.Count > 1)
            {
                if (m_SectorDatas[0].NeedUpdate)
                {
                    m_SectorDatas[1].startdgree = m_SectorDatas[0].enddgree;//下一个
                    m_SectorDatas[1].enddgree = m_SectorDatas[0].startdgree + 360;
                    _sector.UpdateMesh(m_SectorDatas[1]);
                    _sector.UpdateMesh(m_SectorDatas[0]);
                }else if (m_SectorDatas[1].NeedUpdate)
                {
                    m_SectorDatas[0].startdgree = m_SectorDatas[1].enddgree - 360;//
                    m_SectorDatas[0].enddgree = m_SectorDatas[1].startdgree;//
                    _sector.UpdateMesh(m_SectorDatas[0]);
                    _sector.UpdateMesh(m_SectorDatas[1]);
                }
            }
            //1
            else if(m_SectorDatas.Count == 1 && m_SectorDatas[0].NeedUpdate)
            {
                _sector.UpdateMesh(m_SectorDatas[0]);
            }
             
        }
       
    }
    public void RedistributeSectors()
    {
        float degreesPerSector = 360f / m_SectorDatas.Count;
        float currentDegree = 0f;
       
        for (int i = 0; i < m_SectorDatas.Count; i++)
        {
                m_SectorDatas[i].startdgree = currentDegree;
                m_SectorDatas[i].enddgree = currentDegree + degreesPerSector;
                _sector.UpdateMesh(m_SectorDatas[i]);
                currentDegree += degreesPerSector;
        }
    }

    bool DisPoseGameObject()
    {
        if (m_SectorDatas.Count > 0)
        {
            var selectGos = m_SectorDatas.Select(d => d.go);
            var delete = m_CacheGameobject.Find(go => !selectGos.Contains(go));
            if (delete)
            {
                DestroyImmediate(delete);
                for (int i = 0; i < m_CacheGameobject.Count; i++)
                {
                    if (!m_CacheGameobject[i])
                    {
                        m_CacheGameobject.RemoveAt(i);
                    }
                }
                return true;
            }
        }
        return false;
    }

    void OnDestroy()
    {
        for (int i = 0; i < m_SectorDatas.Count; i++)
        { 
            DestroyImmediate(m_SectorDatas[i].mesh);
            m_SectorDatas[i].mesh = null;
            DestroyImmediate(m_SectorDatas[i].mat);
            m_SectorDatas[i].mat = null;
            DestroyImmediate(m_SectorDatas[i].go);
            m_SectorDatas[i].go = null;
            m_SectorDatas[i] = null;
        }
        m_SectorDatas.Clear();
    }
    
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var c = Gizmos.color;
        Gizmos.color = Color.yellow;
        if (m_SectorDatas.Count > 0)
        {
            if (m_SectorDatas[0].mesh)
            {
                if(debugIndex >= 0 && debugIndex < m_SectorDatas[0].mesh.vertices.Length)
                    Gizmos.DrawSphere(m_SectorDatas[0].mesh.vertices[debugIndex] + transform.position, 0.1f);
            }
        }

        Gizmos.color = c;
    }

    public int debugIndex = -1;

#endif
}
