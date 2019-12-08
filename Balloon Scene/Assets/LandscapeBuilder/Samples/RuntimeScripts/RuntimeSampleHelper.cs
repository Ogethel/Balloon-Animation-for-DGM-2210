using UnityEngine;
using System.Collections;

namespace LandscapeBuilder
{
    /// <summary>
    /// Runtime Sample Helper script
    /// </summary>
    public class RuntimeSampleHelper : MonoBehaviour
    {
        /// <summary>
        /// Remove the default camera from the scene
        /// </summary>
        public static void RemoveDefaultCamera()
        {
            // Remove the default camera from the scene
            int numCameras = Camera.allCamerasCount;

            Camera[] cameras = new Camera[numCameras];

            int numCamerasFound = Camera.GetAllCameras(cameras);

            if (numCamerasFound > 1)
            {
                for (int cm = 0; cm < cameras.Length; cm++)
                {
                    // If there is no parent, and name is "Main Camera" then this is likely to be the default camera
                    // that is added to the scene when a new scene is created
                    if (cameras[cm].transform.parent == null && cameras[cm].name == "Main Camera") { DestroyImmediate(cameras[cm].gameObject); break; }
                }
            }
        }

        /// <summary>
        /// Remove the default directional light from the scene
        /// </summary>
        public static void RemoveDefaultLight()
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            if (lights != null)
            {
                for (int lt = 0; lt < lights.Length; lt++)
                {
                    if (lights[lt].name == "Directional Light" && lights[lt].transform.parent == null)
                    {
                        DestroyImmediate(lights[lt].gameObject);
                        break;
                    }
                }
            }
        }
    }
}