using UnityEngine;
using System.Collections;

namespace LandscapeBuilder
{
    /// <summary>
    /// This is attached to a LBStencil layer gameobject in the scene
    /// so that it can be quickly located and cleaned up.
    /// </summary>
    public class LBStencilLayerItem : MonoBehaviour
    {
        [HideInInspector] public string GUID;
    }
}