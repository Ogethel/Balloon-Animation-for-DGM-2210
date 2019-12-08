#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEditor;

namespace LandscapeBuilder
{
    [ExecuteInEditMode]
    public class LBVolumeHighlight : MonoBehaviour
    {
        #region Private Variables
        private Vector3 landscapePos;
        private Vector3 currentPosition = Vector3.zero;
        private float currentYRotation = 0f;
        private Vector3 currentScale = Vector3.one;
        private bool volumeRectSet = false;
        private Mesh volumePreviewMesh;
        private bool volumePreviewMeshSet = false;
        private bool clampPositions = false;
        private bool clampScales = false;
        private Vector3 minClampPosition = Vector3.zero;
        private Vector3 maxClampPosition = Vector3.one;
        private Vector3 minClampScale = Vector3.zero;
        private Vector3 maxClampScale = Vector3.one;
        #endregion

        #region Event Methods

        private void OnDrawGizmos()
        {
            //Color gizmoColour = new Color(190f/255f, 90f/255f, 45f/255f, 1f);
            Color gizmoColour = new Color(204f / 255f, 68f / 255f, 11f / 255f, 0.5f);
            Gizmos.color = gizmoColour;
            if (volumePreviewMesh != null)
            {
                // Draw the preview mesh
                Quaternion previewRot = Quaternion.Euler(0f, currentYRotation, 0f);
                Gizmos.DrawWireMesh(volumePreviewMesh, currentPosition, previewRot, currentScale);
            }
            else
            {
                // If volume preview mesh is not defined, draw a cube instead
                Vector3 previewPos = currentPosition;
                previewPos.y += currentScale.y * 0.5f;
                Gizmos.DrawWireCube(previewPos, currentScale);
            }
        }

        private void OnScene(SceneView sceneView)
        {
            // Draw a position handle
            Vector3 handlePos = currentPosition;
            Quaternion handleRotation = Quaternion.Euler(0f, currentYRotation, 0f);

            // Let user change size of area in scene view using the Scaling tool
            if (Tools.current == Tool.Scale)
            {
                // Display the scale / resizing handles
                currentScale = Handles.ScaleHandle(currentScale, handlePos, handleRotation, HandleUtility.GetHandleSize(handlePos));
                // Limit scales if specified
                if (clampScales)
                {
                    currentScale.x = Mathf.Clamp(currentScale.x, minClampScale.x, maxClampScale.x);
                    currentScale.y = Mathf.Clamp(currentScale.y, minClampScale.y, maxClampScale.y);
                    currentScale.z = Mathf.Clamp(currentScale.z, minClampScale.z, maxClampScale.z);
                }
            }
            else if (Tools.current == Tool.Rotate)
            {
                // Display rotation handles
                handleRotation = Handles.RotationHandle(handleRotation, handlePos);
                currentYRotation = handleRotation.eulerAngles.y;
            }
            else
            {
                // Display position handles
                currentPosition = Handles.PositionHandle(handlePos, Quaternion.identity);
                // Limit positions if specified
                if (clampPositions)
                {
                    currentPosition.x = Mathf.Clamp(currentPosition.x, minClampPosition.x, maxClampPosition.x);
                    currentPosition.y = Mathf.Clamp(currentPosition.y, minClampPosition.y, maxClampPosition.y);
                    currentPosition.z = Mathf.Clamp(currentPosition.z, minClampPosition.z, maxClampPosition.z);
                }
            }
        }

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
        /// Called automatically by Unity when the gameobject loses focus
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
            DestroyImmediate(volumePreviewMesh);
        }

        #endregion

        #region Public Non-Static Methods

        public void SetVolume(Rect areaRect, float yOffset, float yScale, float yRotation, Vector3 landscapePosition)
        {
            landscapePos = landscapePosition;
            currentPosition.x = areaRect.x + landscapePos.x;
            // Y offset is position of the bottom of the volume
            currentPosition.y = yOffset + landscapePos.y;
            currentPosition.z = areaRect.y + landscapePos.z;
            currentScale.x = areaRect.width;
            currentScale.y = yScale;
            currentScale.z = areaRect.height;
            currentYRotation = yRotation;
            volumeRectSet = true;
        }

        public Rect GetAreaRect()
        {
            Rect areaRect = new Rect();
            areaRect.x = currentPosition.x - landscapePos.x;
            areaRect.y = currentPosition.z - landscapePos.z;
            areaRect.width = currentScale.x;
            areaRect.height = currentScale.z;
            return areaRect;
        }

        // Re-build the preview mesh based on a specified RAW file
        public void UpdatePreviewMesh(LBRaw rawFile)
        {
            if (rawFile != null) { volumePreviewMesh = rawFile.CreatePreviewMesh(new Vector3(-0.5f, 0f, -0.5f), 65); volumePreviewMeshSet = true; }
            else { volumePreviewMesh = null; volumePreviewMeshSet = false; }
        }

        public bool VolumePreviewMeshSet() { return volumePreviewMeshSet; }

        public float GetYOffset() { return currentPosition.y - landscapePos.y; }

        public float GetYScale() { return currentScale.y; }

        public float GetYRotation() { return currentYRotation; }

        public bool VolumeRectSet() { return volumeRectSet; }

        // Set min and max positions and scales
        public void SetLimits(bool limitPositions, Vector3 minPosition, Vector3 maxPosition, bool limitScales, Vector3 minScale, Vector3 maxScale)
        {
            clampPositions = limitPositions;
            minClampPosition = minPosition;
            maxClampPosition = maxPosition;
            clampScales = limitScales;
            minClampScale = minScale;
            maxClampScale = maxScale;
        }

        #endregion

        #region Public Static Methods
        /// <summary>
        /// Create a new volume highlighter and return a reference to the LBVolumeHighlight
        /// script attached to the gameobject
        /// </summary>
        /// <returns></returns>
        public static LBVolumeHighlight CreateVolumeHighLighter()
        {
            LBVolumeHighlight highLighter = null;

            GameObject th = new GameObject("Volume Highlight");
            highLighter = th.AddComponent<LBVolumeHighlight>();

            return highLighter;
        }

        #endregion
    }
}
#endif