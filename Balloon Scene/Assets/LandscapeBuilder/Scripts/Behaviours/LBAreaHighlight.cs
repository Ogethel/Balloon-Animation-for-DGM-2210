#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace LandscapeBuilder
{
    [ExecuteInEditMode]
    public class LBAreaHighlight : MonoBehaviour
    {
        private Projector projector;
        private bool rectSet = false;
        private Vector3 landscapePos;
        private GUIStyle sizeLabel;
        private Vector3 sizeLabelOffset;
        private Vector3 sizeHandleScale = Vector3.one;

        void Awake()
        {
            projector = GetComponent<Projector>();
            projector.orthographic = true;
            // Rotate the projector to face downwards
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            sizeLabelOffset = new Vector3(-45f, -30f, 0f);
        }

        private void OnScene(SceneView sceneView)
        {
            if (projector != null)
            {
                // Draw a position handle, but don't allow the user to change the y-axis position
                float yPos = projector.transform.position.y;
                Vector3 handlePos = new Vector3(projector.transform.position.x, 0f, projector.transform.position.z);

                // Let user change size of area in scene view using the Scaling tool
                if (Tools.current == Tool.Scale)
                {
                    sizeLabel = new GUIStyle("Box");
                    sizeLabel.fontSize = 14;
                    sizeLabel.border = new RectOffset(2, 2, 2, 2);
                    GUI.skin.box.normal.textColor = Color.white;
                    sizeLabel.onFocused.textColor = UnityEngine.Color.white;

                    Rect areaRect = GetAreaRect();
                    sizeHandleScale.x = areaRect.width;
                    sizeHandleScale.z = areaRect.height;

                    // Display the scale / resizing handles
                    sizeHandleScale = Handles.ScaleHandle(sizeHandleScale, handlePos, Quaternion.identity, HandleUtility.GetHandleSize(handlePos));

                    areaRect.width = Mathf.Clamp(sizeHandleScale.x, 2f, 10000f);
                    areaRect.height = Mathf.Clamp(sizeHandleScale.z, 2f, 10000f);

                    // Update the size of the projector
                    AssignAreaRect(areaRect, landscapePos, projector.farClipPlane);

                    Handles.Label(handlePos+ sizeLabelOffset, "W: " + areaRect.width.ToString("0.00") + " H: " + areaRect.height.ToString("0.00"), sizeLabel);
                }
                else
                {
                    projector.transform.position = Handles.PositionHandle(handlePos, Quaternion.identity);
                    projector.transform.position = new Vector3(projector.transform.position.x, yPos, projector.transform.position.z);
                }
            }
        }

        #region Event Methods
        private void OnEnable()
        {
            // Add the OnScene method to the drawing of the scene event (delegate?)
            // so that handles will be drawn even when the object isn't selected
            #if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnScene;
            SceneView.duringSceneGui += OnScene;
            #else
            SceneView.onSceneGUIDelegate -= OnScene;
            SceneView.onSceneGUIDelegate += OnScene;
            #endif
            Tools.hidden = true;
        }

        /// <summary>
        /// Called automatically by Unity when the gameobject looses focus
        /// </summary>
        private void OnDisable()
        {
            // Turn on the default scene handles
            Tools.hidden = false;
            Tools.current = Tool.Move;
        }

        private void OnDestroy()
        {
            #if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnScene;
            #else
            SceneView.onSceneGUIDelegate -= OnScene;
            #endif
        }

        #endregion

        // Assign the area rectangle
        public void AssignAreaRect(Rect areaRect, Vector3 landscapePosition, float areaTotalHeight)
        {
            if (projector != null)
            {
                // Resize projector
                projector.orthographicSize = areaRect.height * 0.5f;
                projector.aspectRatio = areaRect.width / areaRect.height;
                projector.farClipPlane = areaTotalHeight;
                // Position projector
                landscapePos = landscapePosition;
                projector.transform.position = new Vector3(areaRect.x + (areaRect.width * 0.5f), areaTotalHeight, areaRect.y + (areaRect.height * 0.5f)) + landscapePos;
                rectSet = true;
            }
        }

        public bool AreaRectSet()
        {
            return rectSet;
        }

        // Retrieve the area rectangle
        public Rect GetAreaRect()
        {
            Rect areaRect = new Rect();
            if (projector != null)
            {
                areaRect.height = projector.orthographicSize * 2f;
                areaRect.width = areaRect.height * projector.aspectRatio;
                areaRect.x = projector.transform.position.x - (areaRect.width * 0.5f) - landscapePos.x;
                areaRect.y = projector.transform.position.z - (areaRect.height * 0.5f) - landscapePos.z;
            }
            return areaRect;
        }

        /// <summary>
        /// Move the highlighter rectangle into the viewable scene
        /// NOTE: This does not change the scene view camera position.
        /// </summary>
        public void MoveToSceneView()
        {
            try
            {
                SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;

                if (sceneView != null)
                {
                    // The SceneView pivot is always the centre or the window
                    projector.transform.position = new Vector3(sceneView.pivot.x, projector.transform.position.y, sceneView.pivot.z);
                }
                else { Debug.LogError("LBAreaHightlight.MoveToSceneView - Couldn't find active scene view"); }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("LBAreaHightlight.MoveToSceneView - Couldn't get scene view\n" + ex.Message);
            }
        }

        /// <summary>
        /// Create a new area highlighter and return a reference to the LBAreaHighlight
        /// script attached to the gameobject
        /// </summary>
        /// <returns></returns>
        public static LBAreaHighlight CreateAreaHighLighter()
        {
            LBAreaHighlight highLighter = null;

            GameObject th = new GameObject("Area Highlight");
            Projector p = th.AddComponent<Projector>();
            Material highlighterMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/LandscapeBuilder/Materials/AreaHighlighter.mat", typeof(Material));
            if (highlighterMaterial != null) { p.material = highlighterMaterial; }
            else { Debug.LogWarning("TerrainHighlighter material not found at path: Assets/LandscapeBuilder/Materials/AreaHighlighter. Did you accidentally delete it?"); }
            highLighter = th.AddComponent<LBAreaHighlight>();

            return highLighter;
        }
    }
}
#endif