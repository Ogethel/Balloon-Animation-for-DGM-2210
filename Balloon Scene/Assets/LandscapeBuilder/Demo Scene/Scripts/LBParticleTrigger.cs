using System.Collections.Generic;
using UnityEngine;

namespace LandscapeBuilder
{
    [RequireComponent(typeof(ParticleSystem))]
    public class LBParticleTrigger : MonoBehaviour
    {
        #region Public variables

        /// <summary>
        /// List of Unity tags.
        /// </summary>
        public List<string> tagList;

        #endregion

        #region Private variables
        private ParticleSystem particleSys;
        private int _numTags = 0;

        #endregion

        #region Initialisation methods

        // Use this for initialization
        void Awake()
        {
            _numTags = tagList == null ? 0 : tagList.Count;
            particleSys = GetComponent<ParticleSystem>();
        }

        #endregion

        #region Event Methods

        private void OnTriggerEnter(Collider other)
        {
            if (particleSys != null)
            {
                bool isStartParticles = false;

                // If not tags in the list, always start particles
                if (_numTags == 0) { isStartParticles = true; }
                else
                {
                    // Check all the tags
                    for (int t = 0; t < _numTags; t++)
                    {
                        if (other.gameObject.CompareTag(tagList[t]))
                        {
                            isStartParticles = true;
                            break;
                        }
                    }
                }

                if (isStartParticles)
                {
                    particleSys.Play(true);
                }
            }
        }


        #endregion
    }
}