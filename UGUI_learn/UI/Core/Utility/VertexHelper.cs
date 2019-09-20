using System;
using System.Collections.Generic;

namespace UnityEngine.UI
{
    public class VertexHelper : IDisposable
    {
        private List<Vector3> m_Positions = ListPool<Vector3>.Get();
        private List<Color32> m_Colors = ListPool<Color32>.Get();
        private List<Vector2> m_Uv0S = ListPool<Vector2>.Get();
        private List<Vector2> m_Uv1S = ListPool<Vector2>.Get();
        private List<Vector2> m_Uv2S = ListPool<Vector2>.Get();
        private List<Vector2> m_Uv3S = ListPool<Vector2>.Get();
        private List<Vector3> m_Normals = ListPool<Vector3>.Get();
        private List<Vector4> m_Tangents = ListPool<Vector4>.Get();
        private List<int> m_Indices = ListPool<int>.Get();

        private static readonly Vector4 s_DefaultTangent = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);
        private static readonly Vector3 s_DefaultNormal = Vector3.back;

        public VertexHelper()
        {
        }

        public VertexHelper(Mesh m)
        {
            m_Positions.AddRange(m.vertices);
            m_Colors.AddRange(m.colors32);
            m_Uv0S.AddRange(m.uv);
            m_Uv1S.AddRange(m.uv2);
            m_Uv2S.AddRange(m.uv3);
            m_Uv3S.AddRange(m.uv4);
            m_Normals.AddRange(m.normals);
            m_Tangents.AddRange(m.tangents);
            m_Indices.AddRange(m.GetIndices(0));
        }

        public void Clear()
        {
            m_Positions.Clear();
            m_Colors.Clear();
            m_Uv0S.Clear();
            m_Uv1S.Clear();
            m_Uv2S.Clear();
            m_Uv3S.Clear();
            m_Normals.Clear();
            m_Tangents.Clear();
            m_Indices.Clear();
        }

        public int currentVertCount
        {
            get { return m_Positions.Count; }
        }

        public int currentIndexCount
        {
            get { return m_Indices.Count; }
        }

        public void PopulateUIVertex(ref UIVertex vertex, int i)
        {
            vertex.position = m_Positions[i];
            vertex.color = m_Colors[i];
            vertex.uv0 = m_Uv0S[i];
            vertex.uv1 = m_Uv1S[i];
            vertex.normal = m_Normals[i];
            vertex.tangent = m_Tangents[i];
        }
        
        //todo 
    }
}
