// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
using UnityEngine;
using System.Collections;

/// <summary>
/// Used with LBObjPath, contains metadata about each
/// point along a path.
/// NOTE: The 3D position in scene or LBGroup is stored in LBPath.positionList
/// </summary>
[System.Serializable]
public class LBPathPoint
{
    #region Public variables
    // IMPORTANT When changing this section update:
    // SetClassDefaults() and LBPathPoint(LBPathPoint lbPathPoint) clone constructor
    public string GUID;
    public bool showInEditor;
    [Range(-359.9f, 359.9f)] public float rotationZ;
    #endregion

    #region Private variables

    #endregion

    #region Constructors

    public LBPathPoint()
    {
        SetClassDefaults();
    }

    // Clone constructor
    public LBPathPoint(LBPathPoint lbPathPoint)
    {
        GUID = lbPathPoint.GUID;
        showInEditor = lbPathPoint.showInEditor;
        rotationZ = lbPathPoint.rotationZ;
    }

    #endregion

    #region Public Member Methods


    #endregion

    #region Private Member Methods

    private void SetClassDefaults()
    {
        // Assign a unique identifier
        GUID = System.Guid.NewGuid().ToString();

        showInEditor = false;
        rotationZ = 0f;
    }

    #endregion

    #region Public Static Methods


    #endregion
}
