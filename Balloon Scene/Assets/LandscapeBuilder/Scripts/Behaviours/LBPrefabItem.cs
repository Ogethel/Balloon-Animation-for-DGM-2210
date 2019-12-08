using UnityEngine;
using System.Collections;

namespace LandscapeBuilder
{
    [ExecuteInEditMode]
    public class LBPrefabItem : MonoBehaviour
    {
        // Component to add to parent gameobject of prefabs instantiated by Mesh/Prefab Tab or Groups Tab

        // Added v1.4.4 Beta 2a
        // ObjPathDesignerPrefab is used when temporarily creating a path using the LBObjPathDesigner
        public enum PrefabItemType
        {
            LegacyMeshPrefab = 0,
            GroupMemberPrefab = 1,
            ObjPathDesignerPrefab = 2,
            ObjPathPrefab = 3,
            VSProVegMaskArea = 4,
            VSProBiomeMaskArea = 5
        }

        [HideInInspector]
        public PrefabItemType prefabItemType = PrefabItemType.LegacyMeshPrefab;
        [HideInInspector]
        public string groupMemberGUID = string.Empty;
    }
}