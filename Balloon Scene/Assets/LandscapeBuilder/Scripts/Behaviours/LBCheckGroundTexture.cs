using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    /// <summary>
    /// Sample Landscape Builder class to be added to a moving object,
    /// like a character controller, to detect the texture at the current
    /// location in the landscape, and to take an appropriate action.
    /// e.g. Play a sound
    /// 
    /// If the world position of the gameobject is not aligned to the ground
    /// and you wish to compare to height of terrain at that point, apply
    /// a verticalOffset.
    /// 
    /// If you have a character controller attached you may wish to take
    /// advantage of the isGround property, rather than checking height of terrain.
    /// 
    /// textureNames are not case-sensitive
    /// The number of texture names should match the number of audio clips
    /// if you want a sound to play when player or object is on a texture.
    /// 
    /// NOTE: This class may be updated with each release of LB. You
    /// may wish to build your own class 
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class LBCheckGroundTexture : MonoBehaviour
    {
        #region Public properties and variables

        public LBLandscape landscape = null;
        [Tooltip("How many seconds to wait between checking the terrain (default 0.5secs)")]
        public float checkInterval = 0.5f;
        public bool startOnWake = true;
        [Tooltip("Will replace any clips already playing on the audio source. To avoid conflicts, place this script on its own child gameobject")]
        public bool overrideOtherClips = false;

        [Header("Height validation")]
        public bool checkTerrainHeight = false;
        [Tooltip("Adjust this if you wish to check terrain height at location but gameobject is offset from ground")]
        [Range(-100f, 100f)]
        public float verticalOffset = 0f;
        [Tooltip("The amount, in metres, of tolerance allowable when comparing the gameobject position (including the verticalOffset) and the height of the terrain")]
        [Range(-5f, 5f)]
        public float heightTolerance = 0f;

        [Header("Editor Only")]
        public bool debuggingMode = false;

        [Header("Textures and matching action sounds")]
        public string[] textureNames;
        public AudioClip[] actionSounds;

        /// <summary>
        /// Start or stop checking
        /// </summary>
        public bool IsPaused { get { return isTimerPaused; } set { isTimerPaused = value; } }

        #endregion

        #region Private properties and variables

        // NOTE: Some variables are declared outside methods to reduce garbarge collection
        private float timer = 0f;
        private bool isTimerPaused = true;
        private bool isTakingAction = false;
        private AudioSource audioSource;
        private Vector3 worldPosition = Vector3.zero;
        private string terrainTextureName = string.Empty;
        private List<string> textureNameList;
        #endregion

        #region Initialise Methods

        private void Start()
        {
            if (startOnWake) { Initialise(); }
        }

        public void Initialise()
        {
            isTimerPaused = true;

            // Setup any pre-requisities for Actions here
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("LBCheckTexture - could not find AudioSource component.");
            }
            else if (landscape == null)
            {
                Debug.LogWarning("LBCheckTexture - a LBLandscape object from the scene has not be added.");
            }
            else
            {
                if (textureNames.Length < 1)
                {
                    Debug.LogWarning("LBCheckTexture - no texture names have been provided to compare with. Did you forget to add some?");
                }
                else
                {
                    textureNameList = new List<string>(textureNames);
                }
                // Get started
                isTimerPaused = false;
            }
        }

        #endregion

        #region Private Methods

        // Update is called once per frame
        void Update()
        {
            // Don't update the timer if the countdown is
            // paused or an action is still running.
            if (!IsPaused && !isTakingAction)
            {
                timer += Time.deltaTime;
                if (timer >= checkInterval)
                {
                    ResetInterval();

                    // Change to TakeAction2() to test alternative method
                    TakeAction();
                }
            }
        }

        /// <summary>
        /// Take whatever action you want to do in this method
        /// </summary>
        private void TakeAction()
        {
            isTakingAction = true;

            // Get the location of the object
            worldPosition = gameObject.transform.position;

            // Check which dominant texture is at this location in the landscape
            terrainTextureName = LBLandscapeTerrain.GetTextureNameAtPosition(landscape, worldPosition, checkTerrainHeight, heightTolerance, true);

#if UNITY_EDITOR
            // Typically, we don't want to be debugging in a build.
            if (debuggingMode) { Debug.Log("INFO: LBCheckTexture - dominate texture at " + worldPosition.ToString() + " is " + (string.IsNullOrEmpty(terrainTextureName) ? "unknown" : terrainTextureName)); }
#endif

            if (textureNameList != null && !string.IsNullOrEmpty(terrainTextureName))
            {
                // Convert to all lowercase for non-case sensitive comparison
                terrainTextureName = terrainTextureName.ToLower();

                // Did we find a match?
                int nameIndex = textureNameList.FindIndex(tn => tn.ToLower() == terrainTextureName);
                if (nameIndex >= 0)
                {
                    PerformAction(terrainTextureName, nameIndex);
                }
            }

            isTakingAction = false;
        }

        /// <summary>
        /// Alternative method of checking the terrain. In this example it must match the first texture in the list
        /// Change the Update() statement to call TakeAction2() rather than TakeAction()
        /// </summary>
        private void TakeAction2()
        {
            isTakingAction = true;

            // Get the location of the object
            worldPosition = gameObject.transform.position;

            // Get the first texture name from the editor
            // You could use any name you like or pass it in as a parameter
            if (textureNameList != null && textureNameList.Count > 0)
            {
                terrainTextureName = textureNameList[0];
                if (!string.IsNullOrEmpty(terrainTextureName))
                {
                    // Check to see if the first texture has a weight of at least 40% at the location
                    if (LBLandscapeTerrain.IsTextureNameAtPosition(landscape, worldPosition, terrainTextureName, checkTerrainHeight, heightTolerance, 0.4f, true))
                    {
                        PerformAction(terrainTextureName, 0);
                    }
                }
            }

            isTakingAction = false;
        }

        /// <summary>
        /// Perform an action based on the texture and it's position in the list of texture names
        /// textureName is all lowercase for non-case sensitive comparision
        /// </summary>
        /// <param name="textureName"></param>
        /// <param name="nameIndex"></param>
        private void PerformAction(string textureName, int nameIndex)
        {
            // Is there a matching actionSound for this texture?
            if (actionSounds != null && nameIndex < actionSounds.Length)
            {
                if (audioSource != null)
                {
                    if (actionSounds[nameIndex] != null)
                    {
                        // Check if a audio clip is already playing or just play it anyway
                        if (overrideOtherClips || !audioSource.isPlaying)
                        {
                            // Play the clip
                            audioSource.clip = actionSounds[nameIndex];
                            audioSource.Play();
                        }
                    }
#if UNITY_EDITOR
                    else
                    {
                        Debug.Log("INFO: LBCheckTexture - actionSound for " + textureName + " at element " + nameIndex + " is empty. Did you added it in the inspector?");
                    }
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
                if (debuggingMode)
                {
                    Debug.Log("INFO: LBCheckTexture - could not find matching actionSound for " + textureName + " for element " + nameIndex + ". Did you added it in the inspector?");
                }
#endif
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Used to reset the timer. 
        /// </summary>
        public void ResetInterval()
        {
            timer = 0f;
        }

        #endregion
    }
}