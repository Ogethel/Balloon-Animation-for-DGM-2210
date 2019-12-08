using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LandscapeBuilder
{
    /// <summary>
    /// LBWindZone serializable class used to store WindZone component data
    /// </summary>
    [System.Serializable]
    public class LBWindZone
    {
        #region Public Variables and Proerties
        // Public fields represent the Windzone property values
        public WindZoneMode mode;

        // Radius of the Spherical Wind Zone (only active if the WindZoneMode is set to Spherical).
        public float radius;

        // The primary wind force.
        public float windMain;

        // Defines the frequency of the wind changes.
        public float windPulseFrequency;

        // Defines ow much the wind changes over time.
        public float windPulseMagnitude;

        // The turbulence wind force.
        public float windTurbulence;

        // In LBLighting, a windzone can be used with WeatherFX
        public bool isWeatherFXWindZone;

        #endregion

        #region Constructors

        public LBWindZone()
        {
            mode = WindZoneMode.Directional;
            windMain = 1f;
            windTurbulence = 1f;
            windPulseMagnitude = 0.5f;
            windPulseFrequency = 0.01f;
            isWeatherFXWindZone = false;
        }

        // Constructor to create clone copy
        public LBWindZone(LBWindZone lbWindZone)
        {
            this.mode = lbWindZone.mode;
            this.windMain = lbWindZone.windMain;
            this.windTurbulence = lbWindZone.windTurbulence;
            this.windPulseMagnitude = lbWindZone.windPulseMagnitude;
            this.windPulseFrequency = lbWindZone.windPulseFrequency;
            this.isWeatherFXWindZone = lbWindZone.isWeatherFXWindZone;
            this.radius = lbWindZone.radius;
        }

        #endregion
    }
}