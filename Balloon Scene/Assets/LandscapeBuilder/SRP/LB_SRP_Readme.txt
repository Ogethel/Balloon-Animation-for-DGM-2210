Scriptable Render Pipeline assets in LB require Unity 2018.3.0f2 or newer.
IMPORTANT: Do not import these packages without URP/LWRP/HDRP installed, else you will get errors. 

1. Import the entire asset (e.g. Landscape Builder) into a project setup for LRWP or HDRP.
2. If using LWRP, from package manager, import Lightweight Render Pipeline 4.0.1 or newer (U2018.3 or newer is required)
3. If using HDRP, from package manager, import High Definition Render Pipeline 4.9.0 or newer (U2018.3 or newer is required)
4. If using URP, from package manager, import Universal Render Pipeline 7.1.2 or newer (U2019.3 or newer is required)
5. From the Unity Editor double-click** on the LB_URP_[version], LB_LWRP_[version] or LB_HDRP_[version] package within this folder

NOTES:

a) Currently the LBImageFX, LB Lighting, and weather features are not supported in HDRP or LRWP/URP.
b) From the LB Editor, on the Landscape tab, change the Material Type in Terrain Settings to LWRP or HDRP and Apply Terrain Settings.
c) HDRP 6.9.0 requires U2019.2.0 or newer

** If double-click does not work, from the Unity Editor menu, Assets/Import Package/Custom Package... navigate to the folder within your project where the package is located to import the package.