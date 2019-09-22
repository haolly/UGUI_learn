using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.UI
{
    public interface IMeshModifier
    {
        void ModifyMesh(Mesh mesh);
        void ModifyMesh(VertexHelper verts);
    }
}