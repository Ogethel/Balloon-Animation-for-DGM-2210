using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LandscapeBuilder;

#if UNITY_EDITOR
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
#endif

// Keep LBGrassSetup class outside the namespace to ensure backward compatibility with LB v1.x

[System.Serializable]
public class LBGrassSetup
{
    /// <summary>
    /// This class is used to contain a list of LBGrassConfig class instances
    /// that can be read/written to disk
    /// </summary>

    public List<LBGrassConfig> lbGrassConfigList;

    [System.NonSerialized] public List<string> sourceList;

    public static string DefaultSourceFilter { get { return "Show All"; } }
    public static string UserDefinedSourceFilter { get { return "User Defined"; } }

    // Basic constructor
    public LBGrassSetup()
    {
        sourceList = new List<string>();
    }

    /// <summary>
    /// Populate the SourceList with Unique sourcenames.
    /// Sort in alphabetic order
    /// </summary>
    public void PopulateSourceList()
    {
        if (lbGrassConfigList != null && sourceList != null)
        {
            sourceList.Clear();
            foreach (LBGrassConfig lbGrassConfig in lbGrassConfigList)
            {
                if (lbGrassConfig.sourceName != UserDefinedSourceFilter && sourceList.FindIndex(s => s == lbGrassConfig.sourceName) < 0)
                {
                    sourceList.Add(lbGrassConfig.sourceName);
                }
            }

            // Sort the list in alphabetic order
            if (sourceList.Count > 1)
            {
                sourceList.Sort();
            }
            // Include the SHOW ALL option at the top of the list
            sourceList.Insert(0, DefaultSourceFilter);
            sourceList.Insert(1, UserDefinedSourceFilter);
        }
    }


#if UNITY_EDITOR

    public void Save(bool useAssetFolder = false)
    {
        // Default path - this is the working copy of LBGrassSetup and is used to edit settings
        // and is used by the user via the LandscapeBuilderGrassSelector
        string pathLBGrassSetup = "LandscapeBuilder/LBGrassSetup.dat";

        // The Asset folder stores the latest setup file downloaded with the latest build of LB.
        // This should only be used under special circumstances by like when updating the primary
        // LBGrassSetup.dat file located at LandscapeBuilder/LBGrassSetup.dat
        if (useAssetFolder)
        {
            pathLBGrassSetup = "Assets/LandscapeBuilder/LBGrassSetup.dat";
        }

        if (lbGrassConfigList != null)
        {
            // Attempt to save the saved data to disk
            BinaryFormatter binaryFormatter = null;
            FileStream fs = null;
            try
            {
                binaryFormatter = new BinaryFormatter();
                fs = File.Open(pathLBGrassSetup, FileMode.OpenOrCreate);

                binaryFormatter.Serialize(fs, this);
                fs.Close();
            }
            catch (Exception ex)
            {
                Debug.Log("ERROR: LBGrassSetup.Save Exception - " + ex.Message);
            }
            finally
            {
                // Cleanup
                if (binaryFormatter != null) { binaryFormatter = null; }
                if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
            }
        }
        else { Debug.Log("ERROR: LBGrassSetup.Save nothing to save"); }
    }

    public void Retrieve(bool useAssetFolder = false)
    {
        // Default path - this is the working copy of LBGrassSetup and is used to edit settings
        // and is used by the user via the LandscapeBuilderGrassSelector
        string pathLBGrassSetup = "LandscapeBuilder/LBGrassSetup.dat";

        // The Asset folder stores the latest setup file downloaded with the latest build of LB.
        // This should only be used under special circumstances by like when updating the primary
        // LBGrassSetup.dat file located at LandscapeBuilder/LBGrassSetup.dat
        if (useAssetFolder)
        {
            pathLBGrassSetup = "Assets/LandscapeBuilder/Setup/LBGrassSetup.dat";
        }

        bool isFileFound = File.Exists(pathLBGrassSetup);

        if (!useAssetFolder && !isFileFound)
        {
            // Attempt to restore file from Asset folder
            LBSetup.SetupGrass();
            isFileFound = File.Exists(pathLBGrassSetup);
        }

        if (isFileFound)
        {
            BinaryFormatter binaryFormatter = null;
            FileStream fs = null;
            try
            {
                // Read the binary data from file in the application default data folder
                binaryFormatter = new BinaryFormatter();

                fs = File.Open(pathLBGrassSetup, FileMode.Open);
                LBGrassSetup tmplbGrassSetup = (LBGrassSetup)binaryFormatter.Deserialize(fs);
                fs.Close();
                if (tmplbGrassSetup == null) { Debug.Log("INFO: LBGrassSetup.Retrieve tmplbGrassSetup is null"); }
                else
                {
                    if (tmplbGrassSetup.lbGrassConfigList == null) { Debug.LogWarning("INFO: LBGrassSetup.Retrieve lbGrassConfigList is empty"); }
                    else
                    {
                        // Create a new list if it doesn't already exist
                        if (lbGrassConfigList == null) { lbGrassConfigList = new List<LBGrassConfig>(); }
                        // Remove anything from the list
                        lbGrassConfigList.Clear();
                        lbGrassConfigList.AddRange(tmplbGrassSetup.lbGrassConfigList);
                        //Debug.Log("LBGrassSetup.Retrieve lbGrassConfigList " + tmplbGrassSetup.lbGrassConfigList.Count.ToString() + " retrieved. Set: " + lbGrassConfigList.Count.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("LBGrassSetup.Retrieve Exception - " + ex.Message + " Path: " + pathLBGrassSetup);
            }
            finally
            {
                // Cleanup
                if (binaryFormatter != null) { binaryFormatter = null; }
                if (fs != null) { fs.Close(); fs.Dispose(); fs = null; }
            }
        }
        else { Debug.LogWarning("INFO: LBGrassSetup.Retrieve(" + useAssetFolder.ToString() + ") - could not find " + pathLBGrassSetup); }
    }

#endif
}
