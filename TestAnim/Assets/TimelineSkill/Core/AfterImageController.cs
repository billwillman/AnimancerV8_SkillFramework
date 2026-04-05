using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class AfterImageController : MonoBehaviour
{
    [SerializeField]
    Vector3 m_Offset;

    [SerializeField]
    List<MeshFilter> m_MeshFilters = new List<MeshFilter>();
    [SerializeField]
    List<SkinnedMeshRenderer> m_SkinnedMeshRenderers = new List<SkinnedMeshRenderer>();


    int[] m_IncludeSubMesh;

    public void EnableAfterImage(bool enable, params int[] targetSubMesh)
    {

    }

    public AfterImage CreateAfterImage(string name, Material material, LayerMask layerMask, params int[] subMeshIndex)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (material == null) return null;

        m_IncludeSubMesh = subMeshIndex;
        CombineInstance[] combineInstances = new CombineInstance[m_SkinnedMeshRenderers.Count + m_MeshFilters.Count];

        int index = 0;
        for (int i = 0; i < m_SkinnedMeshRenderers.Count; i++)
        {
            var render = m_SkinnedMeshRenderers[i];
            if (!render.gameObject.activeInHierarchy) continue;
            if (render.gameObject.name != name) continue;

            var mesh = Bake(render);
            combineInstances[index] = new CombineInstance
            {
                mesh = mesh,
                transform = Matrix4x4.identity,
                subMeshIndex = 0,
            };
            index++;
        }
        for (int i = 0; i < m_MeshFilters.Count; i++)
        {
            var render = m_MeshFilters[i];
            if (!render.gameObject.activeInHierarchy) continue;
            if (render.gameObject.name != name) continue;

            var temp = (render.sharedMesh != null) ? render.sharedMesh : render.mesh;
            var mesh = Instantiate(temp);
            combineInstances[index] = new CombineInstance
            {
                mesh = mesh,
                transform = render.gameObject.transform.localToWorldMatrix,
                subMeshIndex = 0,
            };
            Matrix4x4 matrix4X4 = combineInstances[index].transform;
            Vector3 worldPosition = render.gameObject.transform.position;
            worldPosition += Vector3.right * (transform.forward.x > 0 ? -1 : 1) * m_Offset.x;
            worldPosition += Vector3.up * m_Offset.y;
            worldPosition += Vector3.forward * m_Offset.z;
            matrix4X4[0, 3] = worldPosition.x;
            matrix4X4[1, 3] = worldPosition.y;
            matrix4X4[2, 3] = worldPosition.z;
            combineInstances[index].transform = matrix4X4;

            index++;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances, true, true);

        foreach (var combineInstance in combineInstances)
        {
            DestroyImmediate(combineInstance.mesh);
        }

        AfterImage afterImage = new AfterImage(combinedMesh, new Material(material), layerMask);

        return afterImage;
    }

    Mesh Bake(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        Mesh mesh = new Mesh();

        Quaternion oldRotating = skinnedMeshRenderer.transform.rotation;
        Vector3 oldPosition = skinnedMeshRenderer.transform.position;
        skinnedMeshRenderer.transform.rotation = Quaternion.identity;
        skinnedMeshRenderer.transform.position = Vector3.zero;
        skinnedMeshRenderer.BakeMesh(mesh);
        skinnedMeshRenderer.transform.rotation = oldRotating;
        skinnedMeshRenderer.transform.position = oldPosition;

        Mesh[] subMeshes = new Mesh[mesh.subMeshCount];
        for (int j = 0; j < mesh.subMeshCount; j++)
        {
            if (!m_IncludeSubMesh.Contains(j))
                continue;
            subMeshes[j] = new Mesh();
            CombineInstance[] singleSubMesh = new CombineInstance[1];
            singleSubMesh[0] = new CombineInstance
            {
                mesh = mesh,
                transform = Matrix4x4.identity,
                subMeshIndex = j,
            };
            subMeshes[j].CombineMeshes(singleSubMesh, true, true);
        }

        CombineInstance[] combineInstances = new CombineInstance[subMeshes.Length];
        for (int j = 0; j < subMeshes.Length; j++)
        {
            if (!m_IncludeSubMesh.Contains(j))
                continue;
            combineInstances[j] = new CombineInstance
            {
                mesh = subMeshes[j],
                transform = Matrix4x4.identity,
                subMeshIndex = 0,
            };

            Matrix4x4 matrix4X4 = combineInstances[j].transform;
            Vector3 worldPosition = Vector3.zero;
            worldPosition += Vector3.right * (transform.forward.x > 0 ? -1 : 1) * m_Offset.x;
            worldPosition += Vector3.up * m_Offset.y;
            worldPosition += Vector3.forward * m_Offset.z;
            matrix4X4[0, 3] = worldPosition.x;
            matrix4X4[1, 3] = worldPosition.y;
            matrix4X4[2, 3] = worldPosition.z;
            combineInstances[j].transform = matrix4X4;
        }
        mesh.CombineMeshes(combineInstances, true, true);

        for (int i = 0; i < subMeshes.Length; i++)
        {
            DestroyImmediate(subMeshes[i]);
        }

        return mesh;
    }


    public class AfterImage
    {
        Mesh m_Mesh;
        public Mesh Mesh => m_Mesh;

        Material m_Material;
        public Material Material => m_Material;

        int m_Layer;
        public int Layer => m_Layer;

        Matrix4x4 m_Matrix4X4;
        public Matrix4x4 Matrix4X4 => m_Matrix4X4;

        public AfterImage(Mesh mesh, Material material, int layer)
        {
            m_Mesh = mesh;
            m_Material = material;
            m_Layer = layer;
        }

        public void Start()
        {
            RenderPipelineManager.beginCameraRendering += Update;
        }
        public void End()
        {
            RenderPipelineManager.beginCameraRendering -= Update;
            DestroyImmediate(Mesh);
            DestroyImmediate(Material);
        }
        public void Update(ScriptableRenderContext scriptableRenderContext, Camera camera)
        {
            Graphics.DrawMesh(Mesh, Matrix4x4.identity, m_Material, m_Layer, camera);
        }
    }
}