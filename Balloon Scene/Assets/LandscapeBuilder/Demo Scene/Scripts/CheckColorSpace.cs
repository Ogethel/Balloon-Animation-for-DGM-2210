#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace LandscapeBuilder
{
    [ExecuteInEditMode]
    public class CheckColorSpace : MonoBehaviour
    {

        // Use this for initialization
        void Start()
        {
            if (!Application.isPlaying && PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                string msg = "Your project is using the older Gamma Colour Space. These demo's are designed for Linear colour space. Change Color Space to Linear in the Project Player settings. Then run the scene. If you don't change it, the sky will appear grey.";

                EditorUtility.DisplayDialog("Change Player Project Settings Color Space", msg, "Got it!");

                // NOTE: In 2018.3 betas, Edit/Settings was used. In 2018.3.0f2, it was changed to Edit/Project Settings

                #if UNITY_2018_3_OR_NEWER
                LBEditorHelper.CallMenu("Edit/Project Settings...");
                #else
                LBEditorHelper.CallMenu("Edit/Project Settings/Player");
                #endif

                // The following doesn't work because you'd need to start, stop and restart playing the scene
                //if (EditorUtility.DisplayDialog("Change Project Color Space", msg, "Yes", "No"))
                //{
                //    PlayerSettings.colorSpace = ColorSpace.Linear;
                //}
            }
        }
    }
}

#endif