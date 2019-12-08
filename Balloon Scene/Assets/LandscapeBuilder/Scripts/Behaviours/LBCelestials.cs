using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LandscapeBuilder
{
    [ExecuteInEditMode]
    public class LBCelestials : MonoBehaviour
    {
        // To change:
        // 1. Delete the existing (default) Unity layer of LB Celestials
        // 2. Change celestrialsUnityLayer here
        // 3. Save this file
        // 4. Go back to the Unity editor and wait for it to recompile scripts
        // This will need to be done each time you import a new version of LB.
        public static readonly int celestialsUnityLayer = 25;
        
        #region Public Variables
        public Camera celestialsCamera;
        public LBLighting lighting;
        public Transform moon;
        #endregion

        #region Private Variables
        private CombineInstance[] combineInstances;
        #endregion

        #region Public Member Methods

        public void Initialise(LBLighting lightingScript)
        {
            lighting = lightingScript;

            // Only add celestials camera if it doesn't already exist
            Transform celestialTrm = transform.Find("Celestials Camera");
            if (celestialTrm == null)
            {
                GameObject celestialsCameraObject = new GameObject("Celestials Camera");
                celestialsCameraObject.transform.parent = transform;
                celestialsCameraObject.transform.localPosition = Vector3.zero;
                celestialsCamera = celestialsCameraObject.AddComponent<Camera>();
            }

            celestialsCamera.nearClipPlane = 0.1f;
            celestialsCamera.farClipPlane = 10f;
            celestialsCamera.depth = -100f;
            celestialsCamera.clearFlags = CameraClearFlags.Skybox;
            if (lighting.mainCamera != null) { celestialsCamera.fieldOfView = lighting.mainCamera.fieldOfView; }
            else { Debug.LogWarning("WARNING: Celestials Main Camera is null. Some celestials may not work correctly."); }
        }

        public void BuildCelestrials(LBLighting lightingScript)
        {
            if (lighting == null) { Initialise(lightingScript); }
            DestroyStarGameObject();
            DestroyMoonGameObject();
            CreateStars(lightingScript.numberOfStars, lightingScript.starSize);
            CreateMoon(lightingScript.moonSize);
        }

        /// <summary>
        /// Create the Stars mesh - works in Editor or at runtime
        /// </summary>
        /// <param name="numberOfStars"></param>
        /// <param name="starSize"></param>
        public void CreateStars(int numberOfStars, float starSize)
        {
            // In v1.3.2 Beta 10a the FBX was moved to Assets/LandscapeBuilder/Models/Resources so that it will load at runtime correctly
            Mesh starMesh = Resources.Load("StarLowPolyFBX", typeof(Mesh)) as Mesh;

#if UNITY_EDITOR
            if (starMesh == null)
            {
                Debug.LogWarning("Star mesh could not be found at path: Assets/LandscapeBuilder/Models/Resources/StarLowPolyFBX.fbx, trying old location.");
                starMesh = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Models/StarLowPolyFBX.fbx", typeof(Mesh));
            }
#endif

            if (starMesh != null)
            {
                GameObject starMeshGameObject = new GameObject("Stars");
                starMeshGameObject.transform.parent = transform;
                starMeshGameObject.transform.localPosition = Vector3.zero;
                starMeshGameObject.transform.localRotation = Quaternion.identity;
                starMeshGameObject.transform.localScale = Vector3.one;
                MeshFilter starMFilter = starMeshGameObject.AddComponent<MeshFilter>();
                MeshRenderer starMRenderer = starMeshGameObject.AddComponent<MeshRenderer>();

                starMRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                starMRenderer.receiveShadows = false;

                starMeshGameObject.layer = celestialsUnityLayer;

#if UNITY_5_3
			UnityEngine.Random.seed = 0;
#else
                UnityEngine.Random.InitState(0);
#endif

                combineInstances = new CombineInstance[numberOfStars];
                for (int i = 0; i < combineInstances.Length; i++)
                {
                    Vector3 starPos = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(1f, 5f);
                    starPos.y = Mathf.Abs(starPos.y);
                    combineInstances[i].transform = Matrix4x4.TRS(starPos, Quaternion.identity, Vector3.one * 0.001f * starSize);
                    combineInstances[i].mesh = starMesh;
                }

                starMFilter.sharedMesh = new Mesh();
                starMFilter.sharedMesh.name = "LB Stars Mesh";
                starMFilter.sharedMesh.CombineMeshes(combineInstances);
#if !UNITY_5_5_OR_NEWER
			starMFilter.sharedMesh.Optimize();
#endif

                // In v1.3.2 Beta 10a the LBStar material was moved to Assets/LandscapeBuilder/Materials/Resources so that it will load at runtime correctly
                Material starMaterial = Resources.Load("LBStar", typeof(Material)) as Material;

#if UNITY_EDITOR
                if (starMaterial == null)
                {
                    Debug.LogWarning("Star material could not be found at path: Assets/LandscapeBuilder/Materials/Resources/LBStar.mat. Looking in old location");
                    // Attempt to load from pre-v1.3.2 Beta 10a location
                    starMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/LBStar.mat", typeof(Material));
                    if (starMaterial == null)
                    {
                        starMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                        Debug.LogWarning("Star material could not be found at path: Assets/LandscapeBuilder/Materials/LBStar.mat. Falling back to Default-Material.");
                    }
                }
#endif

                // If star material is still null get from standard shader
                if (starMaterial == null)
                {
                    starMaterial = new Material(Shader.Find("Standard"));
                    Debug.LogWarning("Star material could not be found at path: Assets/LandscapeBuilder/Materials/Resources/LBStar.mat. Falling back to Standard shader default material.");
                }

                if (starMaterial != null) { starMRenderer.material = starMaterial; }

                starMeshGameObject.isStatic = true;
            }
            else
            {
                Debug.LogWarning("Star mesh could not be found at path: Assets/LandscapeBuilder/Models/StarLowPolyFBX.fbx. Not creating stars.");
            }
        }

        /// <summary>
        /// Create the Moon mesh - works in Editor or at runtime
        /// </summary>
        /// <param name="moonSize"></param>
        public void CreateMoon(float moonSize)
        {
            GameObject sphereTemp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh moonMesh = sphereTemp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(sphereTemp);
            if (moonMesh != null)
            {
                GameObject moonRootGameObject = new GameObject("Moon");
                moonRootGameObject.transform.parent = transform;
                moonRootGameObject.transform.localPosition = Vector3.zero;
                moonRootGameObject.transform.localRotation = Quaternion.identity;
                moonRootGameObject.transform.localScale = Vector3.one;
                moon = moonRootGameObject.transform;

                GameObject moonMeshGameObject = new GameObject("Moon Mesh");
                moonMeshGameObject.transform.parent = moon;
                moonMeshGameObject.transform.localPosition = new Vector3(0f, 0f, -0.2f);
                moonMeshGameObject.transform.localRotation = Quaternion.identity;
                moonMeshGameObject.transform.localScale = Vector3.one * 0.01f * moonSize;

                MeshFilter moonMFilter = moonMeshGameObject.AddComponent<MeshFilter>();
                MeshRenderer moonMRenderer = moonMeshGameObject.AddComponent<MeshRenderer>();

                Material moonMaterial = Resources.Load("LBMoon", typeof(Material)) as Material;

                #if UNITY_EDITOR
                if (moonMaterial == null)
                {
                    Debug.LogWarning("Moon material could not be found at path: Assets/LandscapeBuilder/Materials/Resources/LBMoon.mat. Looking in old location");
                    // Attempt to load from pre-v1.3.2 Beta 10a location
                    moonMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/LBMoon.mat", typeof(Material));
                    if (moonMaterial == null)
                    {
                        moonMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                        Debug.LogWarning("Moon material could not be found at path: Assets/LandscapeBuilder/Materials/LBMoon.mat. Falling back to Default-Material.");
                    }
                }
                #endif

                // If moon material is still null get from standard shader
                if (moonMaterial == null)
                {
                    moonMaterial = new Material(Shader.Find("Standard"));
                    Debug.LogWarning("Moon material could not be found at path: Assets/LandscapeBuilder/Materials/Resources/LBMoon.mat. Falling back to Standard shader default material.");
                }

                if (moonMaterial != null) { moonMRenderer.material = moonMaterial; }

                moonMFilter.sharedMesh = moonMesh;

                moonMRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                moonMRenderer.receiveShadows = false;

                moonMeshGameObject.layer = celestialsUnityLayer;
            }
            else
            {
                Debug.LogWarning("Moon mesh could not be found at path: Assets/LandscapeBuilder/Models/Star.blend. Not creating moon.");
            }
        }

        /// <summary>
        /// Verify that the stars and moon have been created
        /// If camera and/or light script isn't linked correctly,
        /// rebuild everything.
        /// </summary>
        public void CheckCelestials(LBLighting lightingScript)
        {
            if (celestialsCamera == null && lighting == null)
            {
                BuildCelestrials(lightingScript);
            }
            else
            {
                if (transform.Find("Stars") == null)
                {
                    CreateStars(lightingScript.numberOfStars, lightingScript.starSize);
                }

                if (transform.Find("Moon") == null)
                {
                    CreateMoon(lightingScript.moonSize);
                }
            }
        }

        public void DestroyStarGameObject()
        {
            Transform starObj = transform.Find("Stars");
            if (starObj != null) { DestroyImmediate(starObj.gameObject); }
        }

        public void DestroyMoonGameObject()
        {
            Transform moonObj = transform.Find("Moon");
            if (moonObj != null) { DestroyImmediate(moonObj.gameObject); }
        }

        public void UpdateCelestials(float timeFloat, bool useMoon, Quaternion moonRotation)
        {
            if (timeFloat < 0.25f)
            {
                celestialsCamera.farClipPlane = 0.8f + (Mathf.InverseLerp(0.25f, 0.1f, timeFloat) * 4.2f);
            }
            else if (timeFloat > 0.75f)
            {
                celestialsCamera.farClipPlane = 0.8f + (Mathf.InverseLerp(0.75f, 0.9f, timeFloat) * 4.2f);
            }
            else
            {
                celestialsCamera.farClipPlane = 0.4f;
            }

            UpdateCelestialsRotation();

            if (useMoon && moon != null) { moon.transform.rotation = moonRotation; }
        }

        public void UpdateCelestialsAdvanced(float starVisibility, bool useMoon, Quaternion moonRotation)
        {
            celestialsCamera.farClipPlane = 0.4f + (4.6f * starVisibility);

            UpdateCelestialsRotation();

            if (useMoon && moon != null) { moon.transform.rotation = moonRotation; }
        }

        public void UpdateCelestialsRotation()
        {
            if (lighting.mainCamera != null)
            {
                celestialsCamera.transform.rotation = lighting.mainCamera.transform.rotation;
                celestialsCamera.fieldOfView = lighting.mainCamera.fieldOfView;
            }
            //else { Debug.LogWarning("WARNING: Celestials Main Camera is null. Some celestials may not work correctly."); }
        }

        #endregion
    }
}