using UnityEngine;
using System.Collections;
using LandscapeBuilder;

/// <summary>
/// Sample to start gameplay after the scene has faded in using LBLighting
/// This is only a sample and can easily be deleted from the LB Asset folder when releasing a game
/// </summary>
public class LBStartAfterFadeIn : MonoBehaviour
{
    #region Public Variables
    public bool isFadeOnWake = true;
    [Range(0,60)] public float fadeInDuration = 30f;
    #endregion

    #region Private Variables
    private LBLighting lbLighting;
    private bool isFadingInStarted = false;
    #endregion

    // Use this for initialization
    void Awake()
    {
        lbLighting = GameObject.FindObjectOfType<LBLighting>();
        if (lbLighting == null) { Debug.LogWarning("ERROR: Cannot find LBLighting in the scene"); }

        if (isFadeOnWake)
        {
            lbLighting.fadeInDuration = fadeInDuration;
            lbLighting.StartScreenFade(true);
            isFadingInStarted = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Although this runs every frame the isFadingInStarted bool test should fail very quickly
        // once the screen has finished fading in. Could potentially disable or destroy this script
        // in the scene from the code that begins the gameplay.
        if (isFadingInStarted && lbLighting != null)
        {
            if (!lbLighting.IsScreenFadingIn)
            {
                isFadingInStarted = false;
                Debug.Log("Fade-in has finished - Start GamePlay");
                // Call your start game code here..
            }
        }
    }
}
