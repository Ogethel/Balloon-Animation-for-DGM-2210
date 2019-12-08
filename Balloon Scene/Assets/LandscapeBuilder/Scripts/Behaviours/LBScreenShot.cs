using UnityEngine;
using System.Collections;
using System.IO;
using System;

namespace LandscapeBuilder
{
    public class LBScreenShot : MonoBehaviour
    {
        #region Public Variables
        [SerializeField] public KeyCode screenshotKey = KeyCode.F7;
        [SerializeField] public bool StandaloneBuild = false;
        [SerializeField] public bool DisableAntiAliasing = false;
        #endregion

        #region Private Variables
        private bool isTakingSnapshot = false;
        private string ssFolderPath = "LandscapeBuilder/ScreenShots/";
        private int antiAliasing = 0;
        private bool isTimerOn = false;
        private float antiAliasingTimer = 0f;
        #endregion

        #region Initialisation Methods

        // Use this for initialization
        void Awake()
        {
#if !UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
        ssFolderPath = Application.persistentDataPath + "/ScreenShots/";
#endif

            // Create the Snapshot folder if it doesn't already exist 
            if (!Directory.Exists(ssFolderPath))
            {
                Directory.CreateDirectory(ssFolderPath);
            }
        }

        #endregion

        #region Update Methods

        // Update is called once per frame
        void Update()
        {
            // We need to wait for the screen shot to taken and written to disk
            // before restoring anti-aliasing
            if (isTimerOn)
            {
                // This only works when game is not paused and timescale != 0
                antiAliasingTimer -= Time.deltaTime;

                if (antiAliasingTimer < 0.01f)
                {
                    isTimerOn = false;
                    // If Anti-aliasing was disable, restore back to original value
                    if (DisableAntiAliasing) { QualitySettings.antiAliasing = antiAliasing; }
                }
            }

#if UNITY_EDITOR
            if (!isTakingSnapshot && !isTimerOn)
            {
                if (Input.GetKeyDown(screenshotKey))
                {
                    // On DirectX Anti Aliasing can cause screenshots to turn out all black
                    antiAliasing = QualitySettings.antiAliasing;

                    if (DisableAntiAliasing) { QualitySettings.antiAliasing = 0; }
                    isTakingSnapshot = true;
                    StartCoroutine(TakeScreenShot());
                }
            }
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
        if (!isTakingSnapshot && StandaloneBuild && !isTimerOn)
        {
            if (Input.GetKeyDown(screenshotKey))
            {
                // On DirectX Anti Aliasing can cause screenshots to turn out all black
                antiAliasing = QualitySettings.antiAliasing;

                if (DisableAntiAliasing) { QualitySettings.antiAliasing = 0; }

                isTakingSnapshot = true;
                StartCoroutine(TakeScreenShot());  
            }
        }
#endif
        }

        #endregion

        #region Private Methods

        private IEnumerator TakeScreenShot()
        {
            string filePathBase = ssFolderPath + System.DateTime.Now.ToString("yyyy-MMM-dd_HHmmss");
            int fileNumber = 2;

            string filePathFull = filePathBase;

            // If we saved a snapshot within the current second, append a number
            while (System.IO.File.Exists(filePathFull))
            {
                filePathFull = filePathBase + fileNumber.ToString();
            }

            // Wait for one frame so that anti-aliasing can be disabled if need be
            yield return new WaitForEndOfFrame();

            // Wait for another frame so that CaptureScreenshot works
            yield return new WaitForEndOfFrame();

            try
            {
#if UNITY_2017_1_OR_NEWER
                ScreenCapture.CaptureScreenshot(filePathFull + ".png");
#else
            Application.CaptureScreenshot(filePathFull + ".png");
#endif
                Debug.Log("Saved Screenshot to " + filePathFull + ".png");
                //if (StandaloneBuild) { outputFullPath = filePathFull; }
            }
            catch (Exception ex)
            {
                Debug.Log("Error saving screenshot - " + ex.Message);
            }

            if (DisableAntiAliasing)
            {
                antiAliasingTimer = 2f;
                isTimerOn = true;
            }

            isTakingSnapshot = false;

            yield return 0;
        }

        //string outputFullPath = string.Empty;

        //void OnGUI()
        //{
        //    Rect _rect = new Rect(0, Screen.height - 20f, Screen.width, 20f);
        //    //GUI.Box(_rect, outputFullPath);
        //    GUI.Box(_rect, "Anti-Aliasing: " + QualitySettings.antiAliasing);
        //}

        #endregion
    }
}