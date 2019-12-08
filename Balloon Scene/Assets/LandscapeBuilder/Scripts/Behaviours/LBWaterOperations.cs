using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System;

namespace LandscapeBuilder
{
    public class LBWaterOperations : MonoBehaviour
    {
        // LBWaterOperations class for use with LBWater. Called from LandscapeBuilderWindow.cs

        // Temp variables
        private static float xF;
        private static float yF;

        /// <summary>
        /// How much to scale the landscape-wide primary body of water
        /// NOTE: can't use an enum because it starts with numbers and contains dots
        /// </summary>
        public static string[] WaterPrimarySizeFactorArray = new string[] { "1x", "1.25x", "1.5x", "2x", "2.5x" };
        public static float[] WaterPrimarySizeFactorArrayLookup = new float[] { 1f, 1.25f, 1.5f, 2f, 2.5f };

        public static float WaterPrimarySizeFactor(int Index)
        {
            // Defaults to 2x landscape size
            float factor = 2;

            if (WaterPrimarySizeFactorArray != null && WaterPrimarySizeFactorArrayLookup != null)
            {
                if (WaterPrimarySizeFactorArray.Length > Index && WaterPrimarySizeFactorArrayLookup.Length == WaterPrimarySizeFactorArray.Length)
                {
                    factor = WaterPrimarySizeFactorArrayLookup[Index];
                }
            }
            return factor;
        }

        public static Transform FindWaterInLandscape(GameObject landscapeGameObject, LBWater lbWater)
        {
            Transform waterTransform = null;
            LBWaterItem lbWaterItem = null;

            // Get an array of all the tranforms under the Landscape gameobject
            Transform[] landscapeTransforms = landscapeGameObject.GetComponentsInChildren<Transform>(true);

            foreach (Transform trfm in landscapeTransforms)
            {
                if (trfm != null)
                {
                    // Does this transform have an attached LBWaterItem script?
                    lbWaterItem = trfm.GetComponent<LBWaterItem>();

                    if (lbWaterItem != null)
                    {
                        // Check to see if it matches our saved value
                        if (lbWaterItem.GUID == lbWater.GUID)
                        {
                            waterTransform = trfm;
                            break;
                        }
                    }
                }
            }

            return waterTransform;
        }

        public static LBWater GetPrimaryWaterBody(LBLandscape landscape)
        {
            LBWater lbWaterPrimary = null;

            if (landscape == null)
            {
                Debug.LogError("GetPrimaryWaterBody - landscape not defined");
            }
            else
            {
                if (landscape.landscapeWaterList != null)
                {
                    foreach (LBWater lbWater in landscape.landscapeWaterList)
                    {
                        if (lbWater != null)
                        {
                            if (lbWater.isPrimaryWater)
                            {
                                lbWaterPrimary = lbWater;
                                break;
                            }
                        }
                    }
                }
            }

            return lbWaterPrimary;
        }

        public static void RemoveWaterReflectionCamera(string CameraName)
        {
            GameObject reflectionCamera = null;
            bool moreCameras = false;

            moreCameras = false;
            do
            {
                reflectionCamera = GameObject.Find("Water4AdvancedReflection" + CameraName);
                if (reflectionCamera != null) { DestroyImmediate(reflectionCamera); moreCameras = true; }
                else { moreCameras = false; }
            } while (moreCameras);

            moreCameras = false;
            do
            {
                reflectionCamera = GameObject.Find("Water4SimpleReflection" + CameraName);
                if (reflectionCamera != null) { DestroyImmediate(reflectionCamera); moreCameras = true; }
                else { moreCameras = false; }
            } while (moreCameras);
        }

        /// <summary>
        /// Add water to the scene - for backward compatibility only.
        /// Please use:
        ///   public static LBWater AddWaterToScene(LBWaterParameters lbWaterParms, ref int numberOfMeshesToCreate)
        /// </summary>
        /// <param name="landscape"></param>
        /// <param name="landscapeGameObject"></param>
        /// <param name="waterPosition"></param>
        /// <param name="waterSize"></param>
        /// <param name="waterIsPrimary"></param>
        /// <param name="waterHeight"></param>
        /// <param name="waterPrefab"></param>
        /// <param name="keepPrefabAspectRatio"></param>
        /// <param name="waterResizingMode"></param>
        /// <param name="waterMaxMeshThreshold"></param>
        /// <param name="waterMainCamera"></param>
        /// <param name="waterCausticPrefabList"></param>
        /// <param name="numberOfMeshesToCreate"></param>
        /// <returns></returns>
        public static LBWater AddWaterToScene(LBLandscape landscape, GameObject landscapeGameObject, Vector3 waterPosition, Vector2 waterSize, bool waterIsPrimary,
                                          float waterHeight, Transform waterPrefab, bool keepPrefabAspectRatio, LBWater.WaterResizingMode waterResizingMode,
                                          int waterMaxMeshThreshold, Camera waterMainCamera, List<Transform> waterCausticPrefabList, ref int numberOfMeshesToCreate)
        {
            LBWaterParameters lbWaterParms = new LBWaterParameters();

            lbWaterParms.landscape = landscape;
            lbWaterParms.landscapeGameObject = landscapeGameObject;
            lbWaterParms.waterPosition = waterPosition;
            lbWaterParms.waterSize = waterSize;
            lbWaterParms.waterIsPrimary = waterIsPrimary;
            lbWaterParms.waterHeight = waterHeight;
            lbWaterParms.waterPrefab = waterPrefab;
            lbWaterParms.keepPrefabAspectRatio = keepPrefabAspectRatio;
            lbWaterParms.waterResizingMode = waterResizingMode;
            lbWaterParms.waterMaxMeshThreshold = waterMaxMeshThreshold;
            lbWaterParms.waterMainCamera = waterMainCamera;
            lbWaterParms.waterCausticsPrefabList = waterCausticPrefabList;
            lbWaterParms.isRiver = false;
            lbWaterParms.lbLighting = null;

            return AddWaterToScene(lbWaterParms, ref numberOfMeshesToCreate);
        }

        /// <summary>
        /// Add Water to the current scene
        /// Is also available for Runtime builds with the following limitations:
        /// 1. Standard Assets Water4Simple is not available
        /// 2. Standard Assets Water4Advanced is not available
        /// Water4Simple/Advanced are newer version than WaterProDaytime/Nighttime
        /// and became availble to non-UnityPro users in Unity 5.x
        /// </summary>
        /// <param name="lbWaterParms"></param>
        /// <param name="numberOfMeshesToCreate"></param>
        /// <returns></returns>
        public static LBWater AddWaterToScene(LBWaterParameters lbWaterParms, ref int numberOfMeshesToCreate)
        {
            LBWater lbWater = null;
            bool isAQUASRiverInstalled = LBIntegration.isAQUASRiverInstalled(false);

            #if UNITY_EDITOR
            Transform waterTransform = null;
            if (lbWaterParms.isRiver && isAQUASRiverInstalled)
            {
                GameObject aquasObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                if (aquasObj == null) { Debug.LogWarning("LBWaterOperations - could not add AQUAS River Plane"); return lbWater; }
                else { waterTransform = aquasObj.transform; }
            }
            else if (lbWaterParms.waterResizingMode == LBWater.WaterResizingMode.CalmWater)
            {
                // Create a custom mesh for Calm Water
                LBMesh lbMesh = CreateWaterPlane(lbWaterParms.landscapeGameObject.name + " Water", lbWaterParms.waterSize.x, lbWaterParms.waterSize.y, true);

                if (lbMesh != null)
                {
                    waterTransform = LBMeshOperations.AddMeshToScene(lbMesh, Vector3.zero, lbWaterParms.landscapeGameObject.name + " Water", lbWaterParms.landscapeGameObject.transform, lbWaterParms.waterMaterial, false, false);
                }
            }
            else { if (lbWaterParms.waterPrefab != null) { waterTransform = (Transform)PrefabUtility.InstantiatePrefab(lbWaterParms.waterPrefab); } }
            #else
            Transform waterTransform = null;
            if (lbWaterParms.isRiver && isAQUASRiverInstalled)
            {
                GameObject aquasObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                if (aquasObj == null) { Debug.LogWarning("LBWaterOperations - could not add AQUAS River Plane"); return lbWater; }
                else { waterTransform = aquasObj.transform; }
            }
            else if (lbWaterParms.waterResizingMode == LBWater.WaterResizingMode.CalmWater)
            {
                // Create a custom mesh for Calm Water
                LBMesh lbMesh = CreateWaterPlane(lbWaterParms.landscapeGameObject.name + " Water", lbWaterParms.waterSize.x, lbWaterParms.waterSize.y, true);

                if (lbMesh != null)
                {
                    waterTransform = LBMeshOperations.AddMeshToScene(lbMesh, Vector3.zero, lbWaterParms.landscapeGameObject.name + " Water", lbWaterParms.landscapeGameObject.transform, lbWaterParms.waterMaterial, false, false);
                }
            }
            else { if (lbWaterParms.waterPrefab != null) { waterTransform = (Transform)Instantiate(lbWaterParms.waterPrefab); } }
            #endif

            if (waterTransform != null)
            {
                waterTransform.position = lbWaterParms.waterPosition + (Vector3.up * lbWaterParms.waterHeight);
                waterTransform.rotation = Quaternion.identity;
                waterTransform.parent = lbWaterParms.landscapeGameObject.transform;
                waterTransform.localScale = Vector3.one;
                if (lbWaterParms.waterResizingMode != LBWater.WaterResizingMode.StandardAssets)
                {
                    waterTransform.gameObject.name = lbWaterParms.landscapeGameObject.name + " Water";
                }

                MeshFilter[] waterMeshes = waterTransform.GetComponentsInChildren<MeshFilter>();

                float minXPos = Mathf.Infinity;
                float maxXPos = Mathf.NegativeInfinity;
                float minZPos = Mathf.Infinity;
                float maxZPos = Mathf.NegativeInfinity;
                float sizeX = 0f, sizeZ = 0f;

                Vector3[] waterMeshVerts;
                Vector3 vertPos;

                for (int i = 0; i < waterMeshes.Length; i++)
                {
                    Mesh waterMesh = waterMeshes[i].sharedMesh;
                    if (waterMesh != null)
                    {
                        waterMeshVerts = waterMesh.vertices;
                        for (int v = 0; v < waterMeshVerts.Length; v++)
                        {
                            vertPos = Vector3.Scale(waterMeshVerts[v], waterMeshes[i].transform.lossyScale) + waterMeshes[i].transform.localPosition;
                            minXPos = Mathf.Min(minXPos, vertPos.x);
                            maxXPos = Mathf.Max(maxXPos, vertPos.x);
                            minZPos = Mathf.Min(minZPos, vertPos.z);
                            maxZPos = Mathf.Max(maxZPos, vertPos.z);
                        }
                    }
                }

                // This is a reference variable passed in by the caller, and gets updated here
                // and is available once this method returns.
                numberOfMeshesToCreate = 0;

                // Estimate the number of meshes that would be created
                if (lbWaterParms.waterResizingMode == LBWater.WaterResizingMode.DuplicatingMeshes)
                {
                    if (minXPos != Mathf.Infinity && maxXPos != Mathf.NegativeInfinity && minZPos != Mathf.Infinity && maxZPos != Mathf.NegativeInfinity)
                    {
                        sizeX = (maxXPos - minXPos);
                        sizeZ = (maxZPos - minZPos);
                        numberOfMeshesToCreate = Mathf.RoundToInt(((lbWaterParms.waterSize.x / sizeX) * (lbWaterParms.waterSize.y / sizeZ)) * (float)waterMeshes.Length);
                    }
                }

                if (numberOfMeshesToCreate > lbWaterParms.waterMaxMeshThreshold)
                {
                    // Attempt to rollback the prefab Instantiation
                    DestroyImmediate(waterTransform.gameObject);
                    Debug.Log("Did not add water to the scene because it would have exceeded the mesh threadhold of " + lbWaterParms.waterMaxMeshThreshold.ToString() + " estimated meshes: " + numberOfMeshesToCreate.ToString());
                }
                else
                {
                    // Create a new instance of LBWater which can be saved with the LBLandscape script in the scene
                    lbWater = new LBWater();
                    if (lbWater == null)
                    {
                        Debug.LogWarning("LBWaterOperations: Could not add LBWater when adding water to the scene");
                    }
                    else
                    {
                        // Set the LBWater instance properties
                        lbWater.waterLevel = lbWaterParms.waterHeight;
                        lbWater.isPrimaryWater = lbWaterParms.waterIsPrimary;
                        if (lbWaterParms.isRiver && isAQUASRiverInstalled) { lbWater.waterPrefabName = null; }
                        else if (lbWaterParms.waterResizingMode == LBWater.WaterResizingMode.CalmWater) { lbWater.waterPrefabName = null; }
                        else { lbWater.waterPrefabName = lbWaterParms.waterPrefab.name; }
                        lbWater.waterPosition = waterTransform.position;
                        lbWater.meshBoundsRect.xMin = minXPos;
                        lbWater.meshBoundsRect.xMax = maxXPos;
                        lbWater.meshBoundsRect.yMin = minZPos;
                        lbWater.meshBoundsRect.yMax = maxZPos;
                        lbWater.resizingMode = lbWaterParms.waterResizingMode;
                        lbWater.waterSize = lbWaterParms.waterSize;
                        lbWater.isRiver = lbWaterParms.isRiver;
                        lbWater.riverMaterial = lbWaterParms.riverMaterial;
                        lbWater.underWaterFXPrefabName = lbWaterParms.underWaterFXPrefabName;
                        lbWater.underWaterFXPrefabPath = lbWaterParms.underWaterFXPrefabPath;
                        lbWater.isUnderWaterFXEnabled = lbWaterParms.isUnderWaterFXEnabled;
                        lbWater.flowMapTexture = lbWaterParms.flowMapTexture;
                        lbWater.riverFlowSpeed = lbWaterParms.riverFlowSpeed;
                        lbWater.waterMaterial = lbWaterParms.waterMaterial;

                        if (minXPos != Mathf.Infinity && maxXPos != Mathf.NegativeInfinity && minZPos != Mathf.Infinity && maxZPos != Mathf.NegativeInfinity)
                        {
                            sizeX = (maxXPos - minXPos);
                            sizeZ = (maxZPos - minZPos);
                            if (lbWaterParms.waterResizingMode == LBWater.WaterResizingMode.TransformScaling)
                            {
                                if (lbWaterParms.keepPrefabAspectRatio)
                                {
                                    float scaleXYZ = Mathf.Max(lbWaterParms.waterSize.x / sizeX, lbWaterParms.waterSize.y / sizeZ);
                                    waterTransform.localScale = scaleXYZ * Vector3.one;
                                }
                                else
                                {
                                    waterTransform.localScale = new Vector3(lbWaterParms.waterSize.x / sizeX, Mathf.Min(lbWaterParms.waterSize.x / sizeX, lbWaterParms.waterSize.y / sizeZ), lbWaterParms.waterSize.y / sizeZ);
                                }
                            }
                            else if (lbWaterParms.waterResizingMode == LBWater.WaterResizingMode.DuplicatingMeshes)
                            {
                                for (xF = (-lbWaterParms.waterSize.x * 0.5f); xF < (lbWaterParms.waterSize.x * 0.5f) + 1f; xF += sizeX)
                                {
                                    for (yF = (-lbWaterParms.waterSize.y * 0.5f); yF < (lbWaterParms.waterSize.y * 0.5f) + 1f; yF += sizeZ)
                                    {
                                        for (int i = 0; i < waterMeshes.Length; i++)
                                        {
                                            if (!Mathf.Approximately(xF, 0f) || !Mathf.Approximately(yF, 0f))
                                            {
                                                Transform newWaterTransform = (Transform)Instantiate(waterMeshes[i].transform,
                                                                                                     waterMeshes[i].transform.position + new Vector3(xF, 0f, yF),
                                                                                                     Quaternion.identity);
                                                newWaterTransform.gameObject.name = waterMeshes[i].gameObject.name;
                                                newWaterTransform.parent = waterTransform;
                                            }
                                        }
                                    }
                                }
                            }
                            else if (lbWaterParms.waterResizingMode == LBWater.WaterResizingMode.StandardAssets)
                            {
                                if (waterTransform.gameObject.name == "Water4Advanced")
                                {
#if UNITY_EDITOR
                                    if (lbWaterParms.keepPrefabAspectRatio)
                                    {
                                        float scaleXYZ = Mathf.Max(lbWaterParms.waterSize.x / sizeX, lbWaterParms.waterSize.y / sizeZ);
                                        waterTransform.localScale = scaleXYZ * Vector3.one;
                                    }
                                    else
                                    {
                                        waterTransform.localScale = new Vector3(lbWaterParms.waterSize.x / sizeX, lbWaterParms.waterSize.x / sizeX, lbWaterParms.waterSize.y / sizeZ);
                                    }

                                    MeshRenderer[] mRenderers = waterTransform.GetComponentsInChildren<MeshRenderer>();
                                    Material waterMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

                                    for (int i = 0; i < mRenderers.Length; i++)
                                    {
                                        if (i == 0 && mRenderers[0] != null)
                                        {
                                            waterMaterial = Instantiate(mRenderers[0].sharedMaterial);
                                            waterMaterial.name = "Water4Advanced_" + lbWaterParms.landscapeGameObject.name;
                                            if (waterMaterial != null)
                                            {
                                                waterMaterial.SetVector("_AnimationTiling", waterMaterial.GetVector("_AnimationTiling") / waterTransform.localScale.z);
                                                waterMaterial.SetVector("_GAmplitude", waterMaterial.GetVector("_GAmplitude") / waterTransform.localScale.z);
                                                waterMaterial.SetVector("_GFrequency", waterMaterial.GetVector("_GFrequency") / waterTransform.localScale.z);
                                                waterMaterial.SetVector("_GSteepness", waterMaterial.GetVector("_GSteepness") / waterTransform.localScale.z);
                                                //waterMaterial.SetVector("_GSpeed", waterMaterial.GetVector("_GSpeed") / waterTransform.localScale.z);
                                                waterMaterial.SetVector("_GDirectionAB", waterMaterial.GetVector("_GDirectionAB") / waterTransform.localScale.z);
                                                waterMaterial.SetVector("_GDirectionCD", waterMaterial.GetVector("_GDirectionCD") / waterTransform.localScale.z);
                                            }
                                        }
                                        if (waterMaterial != null)
                                        {
                                            mRenderers[i].material = waterMaterial;
                                        }
                                    }

                                    // First, get the WaterBase type from the standard assets assembly
                                    System.Type waterBaseType = null;
                                    System.Type waterSpecularLightingType = null;
                                    try
                                    {
                                        // GetType(AssemblyQualifiedName) where AssemblyQualifiedName for this water is UnityStandardAssets.Water.WaterBase,Assembly-CSharp
                                        // classname.GetType().AssemblyQualifiedName
#if UNITY_5_2
                                    waterBaseType = System.Type.GetType("UnityStandardAssets.Water.WaterBase,Assembly-CSharp-firstpass", true, true);
                                    waterSpecularLightingType = System.Type.GetType("UnityStandardAssets.Water.SpecularLighting,Assembly-CSharp-firstpass", true, true);
#else
                                        waterBaseType = System.Type.GetType("UnityStandardAssets.Water.WaterBase,Assembly-CSharp", true, true);
                                        waterSpecularLightingType = System.Type.GetType("UnityStandardAssets.Water.SpecularLighting,Assembly-CSharp", true, true);
#endif
                                        if (waterBaseType != null || waterSpecularLightingType != null)
                                        {
                                            var waterBaseScript = waterTransform.GetComponent(waterBaseType);
                                            if (waterBaseScript != null)
                                            {
                                                System.Reflection.FieldInfo field = waterBaseType.GetField("sharedMaterial");
                                                field.SetValue(waterBaseScript, waterMaterial);

                                                // The Specular Lighting script expects to the see the sun in the specularLight field
                                                // so that the light can be correctly reflects across the surface.
                                                var waterSpecularLightingScript = waterTransform.GetComponent(waterSpecularLightingType);
                                                if (waterSpecularLightingScript != null)
                                                {
                                                    Transform sunTransform = null;
                                                    if (lbWaterParms.lbLighting != null)
                                                    {
                                                        if (lbWaterParms.lbLighting.sun != null)
                                                        {
                                                            sunTransform = lbWaterParms.lbLighting.sun.transform;
                                                        }
                                                    }

                                                    if (sunTransform != null)
                                                    {
                                                        waterSpecularLightingType.InvokeMember("specularLight", System.Reflection.BindingFlags.SetField, null, waterSpecularLightingScript, new object[] { sunTransform });
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Debug.LogWarning("Standard Assets Water is not included in your project, so the standard assets water feature cannot be used.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.LogWarning("Standard Assets Water for Water4Advanced could not be found. Import Standard Assets Environment Water to use this feature. " + ex.Message.ToString());
                                    }

                                    // This was deprecated because users without standard assets water in their project
                                    // would get an error thrown because of it. We now use the reflection code above instead
                                    // WaterBase waterBaseScript = waterTransform.GetComponent<WaterBase>();
                                    // if (waterBaseScript != null) { waterBaseScript.sharedMaterial = waterMaterial; }
#else
                                Debug.LogWarning("Water4Advanced is not supported in this Runtime release. Try WaterProDaytime or WaterProNighttime.");
#endif
                                }
                                else if (waterTransform.gameObject.name == "Water4Simple")
                                {
#if UNITY_EDITOR
                                    if (lbWaterParms.keepPrefabAspectRatio)
                                    {
                                        float scaleXYZ = Mathf.Max(lbWaterParms.waterSize.x / sizeX, lbWaterParms.waterSize.y / sizeZ);
                                        waterTransform.localScale = scaleXYZ * Vector3.one;
                                    }
                                    else
                                    {
                                        waterTransform.localScale = new Vector3(lbWaterParms.waterSize.x / sizeX, lbWaterParms.waterSize.x / sizeX, lbWaterParms.waterSize.y / sizeZ);
                                    }

                                    MeshRenderer[] mRenderers = waterTransform.GetComponentsInChildren<MeshRenderer>();
                                    Material waterMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

                                    for (int i = 0; i < mRenderers.Length; i++)
                                    {
                                        if (i == 0 && mRenderers[0] != null)
                                        {
                                            waterMaterial = new Material(mRenderers[0].sharedMaterial);
                                            waterMaterial.name = "Water4Simple_" + lbWaterParms.landscapeGameObject.name;
                                            if (waterMaterial != null)
                                            {
                                                waterMaterial.SetVector("_AnimationTiling", waterMaterial.GetVector("_AnimationTiling") / waterTransform.localScale.z);
                                                waterMaterial.SetVector("_GAmplitude", waterMaterial.GetVector("_GAmplitude") / waterTransform.localScale.z);
                                                waterMaterial.SetVector("_GFrequency", waterMaterial.GetVector("_GFrequency") / waterTransform.localScale.z);
                                                waterMaterial.SetVector("_GSteepness", waterMaterial.GetVector("_GSteepness") / waterTransform.localScale.z);
                                                //waterMaterial.SetVector("_GSpeed", waterMaterial.GetVector("_GSpeed") / waterTransform.localScale.z);
                                                waterMaterial.SetVector("_GDirectionAB", waterMaterial.GetVector("_GDirectionAB") / waterTransform.localScale.z);
                                                waterMaterial.SetVector("_GDirectionCD", waterMaterial.GetVector("_GDirectionCD") / waterTransform.localScale.z);
                                            }
                                        }
                                        if (waterMaterial != null)
                                        {
                                            mRenderers[i].material = waterMaterial;
                                        }
                                    }

                                    // First, get the WaterBase type from the standard assets assembly
                                    System.Type waterBaseType = null;
                                    System.Type waterSpecularLightingType = null;
                                    try
                                    {
#if UNITY_5_2
                                    waterBaseType = System.Type.GetType("UnityStandardAssets.Water.WaterBase,Assembly-CSharp-firstpass", true, true);
                                    waterSpecularLightingType = System.Type.GetType("UnityStandardAssets.Water.SpecularLighting,Assembly-CSharp-firstpass", true, true);
#else
                                        waterBaseType = System.Type.GetType("UnityStandardAssets.Water.WaterBase,Assembly-CSharp", true, true);
                                        waterSpecularLightingType = System.Type.GetType("UnityStandardAssets.Water.SpecularLighting,Assembly-CSharp", true, true);
#endif
                                        if (waterBaseType != null || waterSpecularLightingType != null)
                                        {
                                            var waterBaseScript = waterTransform.GetComponent(waterBaseType);
                                            if (waterBaseScript != null)
                                            {
                                                System.Reflection.FieldInfo field = waterBaseType.GetField("sharedMaterial");
                                                field.SetValue(waterBaseScript, waterMaterial);

                                                // The Specular Lighting script expects to the see the sun in the specularLight field
                                                // so that the light can be correctly reflects across the surface.
                                                var waterSpecularLightingScript = waterTransform.GetComponent(waterSpecularLightingType);
                                                if (waterSpecularLightingScript != null)
                                                {
                                                    Transform sunTransform = null;
                                                    if (lbWaterParms.lbLighting != null)
                                                    {
                                                        if (lbWaterParms.lbLighting.sun != null)
                                                        {
                                                            sunTransform = lbWaterParms.lbLighting.sun.transform;
                                                        }
                                                    }

                                                    if (sunTransform != null)
                                                    {
                                                        waterSpecularLightingType.InvokeMember("specularLight", System.Reflection.BindingFlags.SetField, null, waterSpecularLightingScript, new object[] { sunTransform });
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Debug.LogWarning("Standard Assets Water is not included in your project, so the standard assets water feature cannot be used.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.LogWarning("Standard Assets Water for Water4Simple could not be found. Import Standard Assets Environment Water to use this feature." + ex.Message.ToString());
                                    }

                                    // This was deprecated because users without standard assets water in their project
                                    // would get an error thrown because of it. We now use the reflection code above instead
                                    // WaterBase waterBaseScript = waterTransform.GetComponent<WaterBase>();
                                    // if (waterBaseScript != null) { waterBaseScript.sharedMaterial = waterMaterial; }
#else
                                Debug.LogWarning("Water4Simple is not supported in this Runtime release. Try WaterBasicDaytime or WaterBasicNighttime.");
#endif
                                }
                                else if (waterTransform.gameObject.name == "WaterProDaytime" || waterTransform.gameObject.name == "WaterProNighttime")
                                {
                                    waterTransform.localScale = new Vector3(lbWaterParms.waterSize.x / sizeX, 1f, lbWaterParms.waterSize.y / sizeZ);
                                }
                                else if (waterTransform.gameObject.name == "WaterBasicDaytime" || waterTransform.gameObject.name == "WaterBasicNightime")
                                {
                                    waterTransform.localScale = new Vector3(lbWaterParms.waterSize.x / sizeX, 1f, lbWaterParms.waterSize.y / sizeZ);
                                }
                            }
                            else if (lbWaterParms.waterResizingMode == LBWater.WaterResizingMode.AQUASLite)
                            {
                                // With AQUAS Lite we always want to keep the aspect ratio
                                float scaleXYZ = Mathf.Max(lbWaterParms.waterSize.x / sizeX, lbWaterParms.waterSize.y / sizeZ);
                                waterTransform.localScale = scaleXYZ * Vector3.one;

                                if (lbWaterParms.waterMainCamera == null)
                                {
                                    Debug.Log("LBWaterOperations - could not add AQUAS_Camera script to MainCamera.");
                                }
                                else
                                {
                                    System.Type aquasCamerType = null;
                                    try
                                    {
                                        // Attempt to find the AQUAS_Camera.cs script in the project
                                        aquasCamerType = System.Type.GetType("AQUAS_Camera, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true, true);
                                        if (aquasCamerType == null)
                                        {
                                            Debug.LogError("AQUAS Lite camera script could not be found in the project.");
                                        }
                                        else
                                        {
                                            // Check to see if the AQUAS_Camera script is already attached to the main camera
                                            var aquasCameraComponent = lbWaterParms.waterMainCamera.gameObject.GetComponent(aquasCamerType);
                                            // Only add the script if it isn't already attached.
                                            if (aquasCameraComponent == null)
                                            {
                                                lbWaterParms.waterMainCamera.gameObject.AddComponent(aquasCamerType);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.LogWarning("AQUAS Lite could not be found. Import AQUAS Lite to use this feature. " + ex.Message.ToString());
                                    }
                                }
                            }
                            else if (lbWaterParms.waterResizingMode == LBWater.WaterResizingMode.AQUAS)
                            {
                                lbWaterParms.waterTransform = waterTransform;
                                LBIntegration.AddAQUASWaterToScene(lbWaterParms, lbWater, sizeX, sizeZ);
                            }
                            else if (lbWaterParms.waterResizingMode == LBWater.WaterResizingMode.CalmWater)
                            {
                                lbWaterParms.waterTransform = waterTransform;
                                LBIntegration.AddCalmWaterToScene(lbWaterParms, lbWater, sizeX, sizeZ, true);
                            }

                            lbWater.meshScale = waterTransform.localScale;
                            lbWater.name = waterTransform.gameObject.name;

                            // Add a LBWaterItem script to the gameobject in the scene
                            LBWaterItem lbWaterItem = waterTransform.gameObject.AddComponent<LBWaterItem>();
                            lbWaterItem.GUID = lbWater.GUID;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("ERROR: Failed to instantiate water prefab - Is the provided transform a prefab?");
            }

            return lbWater;
        }

        /// <summary>
        /// Add water to a mesh that has been created in the scene, typically from a LBMapPath or LBPath
        /// NOTE: lbWaterParms.
        /// </summary>
        /// <param name="lbWaterParms"></param>
        /// <returns></returns>
        public static LBWater AddWaterToMesh(LBWaterParameters lbWaterParms)
        {
            LBWater lbWater = null;

            // Basic validation
            if (lbWaterParms == null) { Debug.LogWarning("LBWaterOperations.AddWaterToMesh - lbWaterParms cannot be null"); }
            else if (lbWaterParms.waterTransform == null) { Debug.LogWarning("LBWaterOperations.AddWaterToMesh - mesh transform cannot be null"); }
            else if (lbWaterParms.landscape == null) { Debug.LogWarning("LBWaterOperations.AddWaterToMesh - landscape cannot be null"); }
            else
            {
                // Create a new instance of LBWater which can be saved with the MapPath.LBPath script in the scene
                lbWater = new LBWater();
                if (lbWater == null)
                {
                    Debug.LogWarning("LBWaterOperations.AddWaterToMesh Could not add LBWater when adding water to the mesh in the scene");
                }
                else
                {
                    lbWater.name = lbWaterParms.waterTransform.name;
                    lbWater.meshResizingMode = lbWaterParms.waterMeshResizingMode;

                    if (lbWaterParms.waterMeshResizingMode != LBWater.WaterMeshResizingMode.StandardAssets)
                    {
                        // Remove other types of water (if they exist). Don't show errors because if the components aren't in the
                        // project they they cannot be removed anyway.
                        EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, "Water4Simple", null, null, false, false);
                        EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, "Water4Advanced", null, null, false, false);
                        EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, "WaterBasicDaytime", null, null, false, false);
                        EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, "WaterBasicNighttime", null, null, false, false);
                        EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, "WaterProDaytime", null, null, false, false);
                        EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, "WaterProNighttime", null, null, false, false);
                    }

                    if (lbWaterParms.waterMeshResizingMode != LBWater.WaterMeshResizingMode.AQUAS)
                    {
                        // Cleanup any previous AQUAS scripts that may have been used last time
                        LBIntegration.RemoveAQUASComponents(lbWaterParms.waterTransform.gameObject, false);
                    }

                    if (lbWaterParms.waterMeshResizingMode != LBWater.WaterMeshResizingMode.CalmWater)
                    {
                        LBIntegration.EnableCalmWaterComponents(lbWaterParms.waterTransform, false, false);
                    }

                    if (lbWaterParms.waterMeshResizingMode == LBWater.WaterMeshResizingMode.AQUAS)
                    {
                        if (LBIntegration.AddAQUASWaterToMesh(lbWaterParms, lbWater))
                        {
                            // Need to pass a reference to variable that gets set to notify of scene change - it is ignored here, as
                            // the calling function will typically save the scene.
                            bool isSaveSceneRequired = false;
                            GameObject aquasCameraGameObj = lbWaterParms.waterMainCamera != null ? lbWaterParms.waterMainCamera.gameObject : null;

                            LBIntegration.AddAQUASCameraScript(aquasCameraGameObj, true, ref isSaveSceneRequired);
                        }
                        else
                        {
                            Debug.LogWarning("LBWaterOperations.AddWaterToMesh - something went wrong trying to integrate with AQUAS.");
                        }
                    }
                    else if (lbWaterParms.waterMeshResizingMode == LBWater.WaterMeshResizingMode.CalmWater)
                    {
                        if (!LBIntegration.AddCalmWaterToMesh(lbWaterParms, lbWater, true))
                        {
                            Debug.LogWarning("LBWaterOperations.AddWaterToMesh - something went wrong trying to integrate with Calm Water.");
                        }
                    }
                    else if (lbWaterParms.waterMeshResizingMode == LBWater.WaterMeshResizingMode.Custom)
                    {
                        Renderer renderer = lbWaterParms.waterTransform.GetComponent<Renderer>();
                        if (renderer == null) { Debug.LogWarning("LBWaterOperations.AddWaterToMesh - renderer is missing from " + lbWaterParms.waterTransform.name); }
                        else
                        {
                            // Remove any mesh colliders from the river surface mesh
                            DestroyImmediate(lbWaterParms.waterTransform.GetComponent<MeshCollider>());

                            // By default do not cast shadows
                            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        }
                    }
                    else if (lbWaterParms.waterMeshResizingMode == LBWater.WaterMeshResizingMode.StandardAssets)
                    {
                        Renderer renderer = lbWaterParms.waterTransform.GetComponent<Renderer>();
                        if (renderer == null) { Debug.LogWarning("LBWaterOperations.AddWaterToMesh - renderer is missing from " + lbWaterParms.waterTransform.name); }
                        else
                        {
                            // The Standard Assets water should not cast shadows
                            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                            string waterTile = string.Empty;
                            if (lbWaterParms.riverMaterial != null) { waterTile = lbWaterParms.riverMaterial.name; }

                            Transform sunTransform = null;
                            if (lbWaterParms.lbLighting != null)
                            {
                                if (lbWaterParms.lbLighting.sun != null)
                                {
                                    sunTransform = lbWaterParms.lbLighting.sun.transform;
                                }
                            }

                            // Remove other types of water (if they exist)
                            if (waterTile != "Water4Simple") { EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, "Water4Simple", null, null, false, false); }
                            if (waterTile != "Water4Advanced") { EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, "Water4Advanced", null, null, false, false); }
                            if (waterTile != "WaterBasicDaytime") { EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, "WaterBasicDaytime", null, null, false, false); }
                            if (waterTile != "WaterBasicNighttime") { EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, "WaterBasicNighttime", null, null, false, false); }
                            if (waterTile != "WaterProDaytime") { EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, "WaterProDaytime", null, null, false, false); }
                            if (waterTile != "WaterProNighttime") { EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, "WaterProNighttime", null, null, false, false); }

                            EnableStandardAssetsWaterComponents(lbWaterParms.waterTransform, waterTile, lbWaterParms.riverMaterial, sunTransform, true, true);
                        }
                    }
                }
            }

            return lbWater;
        }

        /// <summary>
        /// Adds or removes Standard Assets Water components / scripts to a GameObject
        /// NOTE: Currently doesn't add correct tiles and reflection plane for Water4Simple, Water4Advanced
        /// </summary>
        /// <param name="waterTransform"></param>
        /// <param name="waterTitle"></param>
        /// <param name="waterMaterial"></param>
        /// <param name="sunTransform"></param>
        /// <param name="isEnabled"></param>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        public static bool EnableStandardAssetsWaterComponents(Transform waterTransform, string waterTitle, Material waterMaterial, Transform sunTransform, bool isEnabled, bool showErrors)
        {
            bool isSuccessful = false;

            if (waterTitle == "WaterProDaytime" || waterTitle == "WaterProNighttime")
            {
                System.Type waterType = null;
                try
                {
                    // Requires Unity 5.3 or newer
                    waterType = System.Type.GetType("UnityStandardAssets.Water.Water,Assembly-CSharp", true, true);
                    if (waterType != null)
                    {
                        // Check to see if the water script is already attached to the transform
                        var waterComponent = waterTransform.GetComponent(waterType);
                        // Only add the script if it isn't already attached.
                        if (waterComponent == null && isEnabled)
                        {
                            waterComponent = waterTransform.gameObject.AddComponent(waterType);
                        }
                        else if (waterComponent != null && !isEnabled)
                        {
                            // remove it.
                            DestroyImmediate(waterComponent);
                        }
                    }

                    isSuccessful = true;
                }
                catch (Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("Standard Assets Water could not be found. Import Standard Assets Environment Water to use this feature." + ex.Message.ToString()); }
                }
            }
            if (waterTitle == "WaterBasicDaytime" || waterTitle == "WaterBasicNighttime")
            {
                System.Type waterType = null;
                try
                {
                    // Requires Unity 5.3 or newer
                    waterType = System.Type.GetType("UnityStandardAssets.Water.WaterBasic,Assembly-CSharp", true, true);
                    if (waterType != null)
                    {
                        // Check to see if the water script is already attached to the transform
                        var waterComponent = waterTransform.GetComponent(waterType);
                        // Only add the script if it isn't already attached.
                        if (waterComponent == null && isEnabled)
                        {
                            waterComponent = waterTransform.gameObject.AddComponent(waterType);
                        }
                        else if (waterComponent != null && !isEnabled)
                        {
                            // remove it.
                            DestroyImmediate(waterComponent);
                        }
                    }

                    isSuccessful = true;
                }
                catch (Exception ex)
                {
                    if (showErrors) { Debug.LogWarning("Standard Assets Water could not be found. Import Standard Assets Environment Water to use this feature." + ex.Message.ToString()); }
                }
            }
            else if (waterTitle == "Water4Advanced" || waterTitle == "Water4Simple")
            {
                // First, get the WaterBase type from the standard assets assembly
                System.Type waterBaseType = null;
                System.Type waterSpecularLightingType = null;
                System.Type waterGerstnerDisplaceType = null;
                System.Type waterPlanarReflectionType = null;
                try
                {
                    // GetType(AssemblyQualifiedName) where AssemblyQualifiedName for this water is UnityStandardAssets.Water.WaterBase,Assembly-CSharp
                    // classname.GetType().AssemblyQualifiedName
                    waterBaseType = System.Type.GetType("UnityStandardAssets.Water.WaterBase,Assembly-CSharp", true, true);
                    waterSpecularLightingType = System.Type.GetType("UnityStandardAssets.Water.SpecularLighting,Assembly-CSharp", true, true);
                    waterGerstnerDisplaceType = System.Type.GetType("UnityStandardAssets.Water.GerstnerDisplace,Assembly-CSharp", true, true);
                    waterPlanarReflectionType = System.Type.GetType("UnityStandardAssets.Water.PlanarReflection,Assembly-CSharp", true, true);
                    if (waterBaseType != null && waterSpecularLightingType != null && waterGerstnerDisplaceType != null && waterPlanarReflectionType != null)
                    {
                        var waterBaseScript = waterTransform.GetComponent(waterBaseType);
                        var waterSpecularLightingScript = waterTransform.GetComponent(waterSpecularLightingType);
                        var waterGerstnerDisplaceScript = waterTransform.GetComponent(waterGerstnerDisplaceType);
                        var waterPlanarReflectionScript = waterTransform.GetComponent(waterPlanarReflectionType);

                        // Remove if required (in reverse order because Specular depends on waterbase
                        if (waterSpecularLightingScript != null && !isEnabled) { DestroyImmediate(waterSpecularLightingScript); }
                        if (waterGerstnerDisplaceScript != null && !isEnabled) { DestroyImmediate(waterGerstnerDisplaceScript); }
                        if (waterPlanarReflectionScript != null && !isEnabled) { DestroyImmediate(waterPlanarReflectionScript); }
                        if (waterBaseScript != null && !isEnabled) { DestroyImmediate(waterBaseScript); }

                        // Only add the script if it isn't already attached.
                        if (waterBaseScript == null && isEnabled) { waterBaseScript = waterTransform.gameObject.AddComponent(waterBaseType); }

                        if (waterGerstnerDisplaceScript == null && isEnabled) { waterGerstnerDisplaceScript = waterTransform.gameObject.AddComponent(waterGerstnerDisplaceType); }
                        if (waterGerstnerDisplaceScript == null && isEnabled) { waterGerstnerDisplaceScript = waterTransform.gameObject.AddComponent(waterGerstnerDisplaceType); }
                        if (waterPlanarReflectionScript == null && isEnabled) { waterPlanarReflectionScript = waterTransform.gameObject.AddComponent(waterPlanarReflectionType); }

                        if (waterBaseScript != null && isEnabled)
                        {
                            System.Reflection.FieldInfo field = waterBaseType.GetField("sharedMaterial");
                            field.SetValue(waterBaseScript, waterMaterial);

                            // The Specular Lighting script expects to the see the sun in the specularLight field
                            // so that the light can be correctly reflects across the surface.
                            if (waterSpecularLightingScript != null)
                            {
                                if (sunTransform != null)
                                {
                                    waterSpecularLightingType.InvokeMember("specularLight", System.Reflection.BindingFlags.SetField, null, waterSpecularLightingScript, new object[] { sunTransform });
                                }
                            }

                            if (waterPlanarReflectionScript != null)
                            {
                                waterPlanarReflectionType.InvokeMember("reflectSkybox", System.Reflection.BindingFlags.SetField, null, waterPlanarReflectionScript, new object[] { true });

                                System.Reflection.FieldInfo layerMaskField = waterPlanarReflectionType.GetField("reflectionMask");
                                LayerMask layerMask = (LayerMask)layerMaskField.GetValue(waterPlanarReflectionScript);

                                // Set all bits on (Everything)
                                layerMask.value = -1;
                                layerMaskField.SetValue(waterPlanarReflectionScript, layerMask);

                                //LBIntegration.ReflectionOutputFields(waterPlanarReflectionType, true, true);
                            }

                            isSuccessful = true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Standard Assets Water4Simple/Advanced is not included in your project, so the standard assets water feature cannot be used.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Standard Assets Water for Water4Advanced could not be found. Import Standard Assets Environment Water to use this feature. " + ex.Message.ToString());
                }

            }

            return isSuccessful;
        }

        /// <summary>
        /// Create a water plane (mesh) with 1,1,1 scale
        /// Currently used with Calm Water
        /// </summary>
        /// <param name="sizeX"></param>
        /// <param name="sizeY"></param>
        public static LBMesh CreateWaterPlane(string meshName, float sizeX, float sizeZ, bool showErrors)
        {
            LBMesh lbMesh = new LBMesh();

            if (lbMesh == null) { if (showErrors) { Debug.LogWarning("ERROR: LBWaterOperations.CreateWaterPlane - failed to create instance of LBMesh"); } }
            else
            {
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                List<Vector2> uvs = new List<Vector2>();
                List<Vector3> normals = new List<Vector3>();
                List<Vector4> tangents = new List<Vector4>();
                List<Vector4> colours = new List<Vector4>();    // Store as Vector4s rather than Color so they are serializable (if required)

                Vector3 normal = new Vector3(0f, 1f, 0f);
                Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);

                // Default colour of each vert (stored in LB as a Vector4)
                Vector4 defaultVertColour = new Vector4(1f, 1f, 1f, 1f);

                float halfX = sizeX / 2f;
                float halfZ = sizeZ / 2f;

                Vector3 bottomLeft = new Vector3(-halfX, 0f, -halfZ);
                Vector3 topLeft = new Vector3(-halfX, 0f, halfZ);
                Vector3 bottomRight = new Vector3(halfX, 0f, -halfZ);
                Vector3 topRight = new Vector3(halfX, 0f, halfZ);

                vertices.Add(bottomLeft);
                vertices.Add(topLeft);
                vertices.Add(topRight);
                vertices.Add(bottomRight);

                // Set default vert colour
                colours.Add(defaultVertColour);
                colours.Add(defaultVertColour);

                // Left triangle
                triangles.Add(0);
                triangles.Add(1);
                triangles.Add(2);

                // Right triangle
                triangles.Add(0);
                triangles.Add(2);
                triangles.Add(3);

                // Add UVs in same order as verts
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(1f, 0f));

                // Add the normals for the verts
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);

                // Add the tangents for the verts
                tangents.Add(tangent);
                tangents.Add(tangent);
                tangents.Add(tangent);
                tangents.Add(tangent);

                lbMesh.verts = vertices;
                lbMesh.triangles = triangles;
                lbMesh.normals = normals;
                lbMesh.uvs = uvs;
                lbMesh.tangents = tangents;

                // Create the Unity mesh
                lbMesh.mesh = new Mesh();
                lbMesh.mesh.name = meshName;

                // Assign verts, triangle to new Unity Mesh and recalc bounds and normals
                if (!lbMesh.UpdateMesh(false, showErrors)) { lbMesh = null; }
            }
            return lbMesh;
        }
    }
}