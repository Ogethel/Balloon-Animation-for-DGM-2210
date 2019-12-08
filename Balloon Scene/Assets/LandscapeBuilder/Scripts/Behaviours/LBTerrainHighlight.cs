#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Projector))]
    public class LBTerrainHighlight : MonoBehaviour
    {
        private Projector projector;
        private float heightAboveSamplePoint = 30f;

        private Camera sceneViewCam;
        private int framesTakenToFindSceneCamera = 0;
        private Event currentEvent;
        private float highlighterSize = 0f;

        /// <summary>
        /// Returns the radius of the highlighter (Read-Only)
        /// </summary>
        public float HighlighterSize { get { return highlighterSize; } }

        void Awake()
        {
            Initialise();
        }

        private void Initialise()
        {
            projector = GetComponent<Projector>();
            projector.orthographic = true;
            // Rotate the projector to face downwards
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Attempt to find the scene view camera
            EditorApplication.update += FindSceneCamera;
        }

        void OnDrawGizmos()
        {
            if (sceneViewCam != null)
            {
                // Get the current event data
                currentEvent = Event.current;
                // Find the mouse position
                Vector3 mousePosition = currentEvent.mousePosition;
                // Invert the y-axis of the mouse position (for some reason it is incorrect...?)
                mousePosition.y = sceneViewCam.pixelHeight - mousePosition.y;
                // Do a raycast to find the world position of the mouse (what you are hovering over in the editor)
                RaycastHit hitInfo;
                if (Physics.Raycast(sceneViewCam.ScreenPointToRay(mousePosition), out hitInfo))
                {
                    // Move the projector to the calculated position plus a certain height offset
                    transform.position = hitInfo.point + (Vector3.up * heightAboveSamplePoint);
                }
            }
            else
            {
                // Set up the projector and find the scene view camera
                Initialise();
            }
        }

        private void FindSceneCamera()
        {
            // The scene view camera should be the one currently active
            sceneViewCam = Camera.current;
            // If it isn't found, try again next frame, up to a maximum of 10 attempts
            framesTakenToFindSceneCamera++;
            if (sceneViewCam != null || framesTakenToFindSceneCamera > 10) { EditorApplication.update -= FindSceneCamera; }
        }

        /// <summary>
        /// Sets the projector values
        /// </summary>
        /// <param name="size">Size.</param>
        /// <param name="terrainHeight">Terrain height.</param>
        public void SetProjectorValues(float size, float terrainHeight)
        {
            highlighterSize = size;
            projector.orthographicSize = size;
            projector.farClipPlane = terrainHeight * 2f;
            heightAboveSamplePoint = terrainHeight;
        }

        /// <summary>
        /// Gets the real world position of the projector
        /// </summary>
        /// <returns>The real world position.</returns>
        public Vector3 GetRealWorldPosition()
        {
            return transform.position - (Vector3.up * heightAboveSamplePoint);
        }

        /// <summary>
        /// Get the selection rectange (the bounds of the highlighter)
        /// </summary>
        /// <returns></returns>
        public Rect GetSelectionRectangle()
        {
            Vector3 selectionPos = this.GetRealWorldPosition();
            return Rect.MinMaxRect(selectionPos.x - highlighterSize, selectionPos.z - highlighterSize, selectionPos.x + highlighterSize, selectionPos.z + highlighterSize);
        }

        /// <summary>
        /// Create a new terrain highlighter and return a reference to the LBTerrainHighlight
        /// script attached to the gameobject
        /// </summary>
        /// <returns></returns>
        public static LBTerrainHighlight CreateTerrainHighLighter()
        {
            LBTerrainHighlight highLighter = null;

            GameObject th = new GameObject("Selection Highlight");
            Projector p = th.AddComponent<Projector>();
            Material highlighterMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/TerrainHighlighter.mat", typeof(Material));
            if (highlighterMaterial != null) { p.material = highlighterMaterial; }
            else { Debug.LogWarning("TerrainHighlighter material not found at path: Assets/LandscapeBuilder/Materials/TerrainHighlighter. Did you accidentally delete it?"); }
            highLighter = th.AddComponent<LBTerrainHighlight>();

            return highLighter;
        }

        /// <summary>
        /// Create a new terrain highlighter and return a reference to the LBTerrainHightlight
        /// script attached to the gameobject. Provide option to select the material of the hightlighter
        /// </summary>
        /// <param name="MaterialName"></param>
        /// <returns></returns>
        public static LBTerrainHighlight CreateTerrainHighLighter(string MaterialName)
        {
            LBTerrainHighlight highLighter = null;

            GameObject th = new GameObject("Selection Highlight");
            Projector p = th.AddComponent<Projector>();
            Material highlighterMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/" + MaterialName + ".mat", typeof(Material));
            if (highlighterMaterial != null) { p.material = highlighterMaterial; }
            else { Debug.LogWarning("TerrainHighlighter material not found at path: Assets/LandscapeBuilder/Materials/" + MaterialName + ". Did you accidentally delete it?"); }
            highLighter = th.AddComponent<LBTerrainHighlight>();

            return highLighter;
        }
    }
}
#endif