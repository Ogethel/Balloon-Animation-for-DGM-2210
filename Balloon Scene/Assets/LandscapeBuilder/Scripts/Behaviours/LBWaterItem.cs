using UnityEngine;
using System.Collections;

namespace LandscapeBuilder
{
    /// <summary>
    /// This gets attached to water objects in the scene hierarchy
    /// It is used to match the parent gameobject with a LBWater class
    /// instance in landscape.landscapeWaterList.
    /// Added version 1.2.0
    /// </summary>
    [ExecuteInEditMode]
    public class LBWaterItem : MonoBehaviour
    {
        [HideInInspector] public string GUID;
    }
}