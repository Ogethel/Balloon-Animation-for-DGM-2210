// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.

// Splines
uint numSplineCentrePoints;
StructuredBuffer<float3> splinePointsCentre;
StructuredBuffer<float3> splinePointsLeft;
StructuredBuffer<float3> splinePointsRight;

// Heightmap variables
int hmapRes;

// Path variables
bool closedCircuit;
uint quadLookAhead;

float GetWidthOnSpline(uint splineWidthPoint)
{
	// The width at this spline point is the distance between the left and right splines
	return sqrt(PlanarSquareDistance(splinePointsLeft[splineWidthPoint], splinePointsRight[splineWidthPoint]));
}

// Get the width between two points on the centre spline based on the 0.0-1.0 normalised position.
float GetWidthOnSpline2D(uint firstMatchPtIdx, uint secondMatchPtIdx, float normalisedPos)
{
	return lerp(sqrt(PlanarSquareDistance(splinePointsLeft[firstMatchPtIdx], splinePointsRight[firstMatchPtIdx])), sqrt(PlanarSquareDistance(splinePointsLeft[secondMatchPtIdx], splinePointsRight[secondMatchPtIdx])), normalisedPos);
}

// Return the normalised height from the supplied height data. Assumes
// heightmap resolution = hmapRes
float LBGetTopoHeightN(uint2 pos2D, RWStructuredBuffer<float> heights)
{
	return heights[(pos2D.y * hmapRes) + pos2D.x];
}

// Returns the Normalised height (0.0-1.0) which is interpolated between
// the nearest heightmap points. pos2DN is normalised position in heights buffer.
// heights buffer can be single terrain or whole landscape.
// PathTopo is per terrain. ObjPathTopo is per landscape
float LBGetInterpolatedHeightN(float2 pos2DN, RWStructuredBuffer<float> heights)
{
	// Get xy heightmap position from heights normalised position.
	uint2 hmapPos2D = uint2(round(pos2DN.x * float(hmapRes - 1)), round(pos2DN.y * float(hmapRes - 1)));

	// TODO Could potentially check some extra neighbouring terrains' edge heights data
	// that are passed into shader - so we get consistent heights along terrain borders.
	// This would only apply to when heights is a single terrain heightmap.

	// Get min/max x,y points
	uint2 minPos2D = uint2(trunc(pos2DN.x * float(hmapRes - 1)), trunc(pos2DN.y * float(hmapRes - 1)));

	// Check if we're on an edge
	if (minPos2D.x == uint(hmapRes - 1)) { --minPos2D.x; }
	if (minPos2D.y == uint(hmapRes - 1)) { --minPos2D.y; }

	uint2 maxPos2D = uint2(minPos2D.x + 1, minPos2D.y + 1);

	// Get surrounding heights
	float h1 = LBGetTopoHeightN(uint2(hmapPos2D.x, minPos2D.y), heights);
	float h2 = LBGetTopoHeightN(uint2(hmapPos2D.x, maxPos2D.y), heights);
	float h3 = LBGetTopoHeightN(uint2(minPos2D.x, hmapPos2D.y), heights);
	float h4 = LBGetTopoHeightN(uint2(maxPos2D.x, hmapPos2D.y), heights);
	// Just avg 4 surrounding points (which is closest to Unity's GetInterpolatedHeight)
	// NOTE: Including diagonals doesn't improve it (slower and more jaggard)
	return (h1 + h2 + h3 + h4) / 4.0;
}

// Check to see if the supplied worldpace heightmap position is within the current path defined by the centre, left and right spline buffers.
// The spline and height buffers must be assigned to the kernal method that calls this function.
// The 4 points that define the matching quad are returned as out params along with the closed centre spline piont used for the width.
bool IsPointInPath(float3 hmapWorldPos3D, out float3 quadP1, out float3 quadP2, out float3 quadP3, out float3 quadP4, out uint widthPoint, out uint firstMatchPtIdx, out uint secondMatchPtIdx)
{
	// Find the closest central spline point
	uint closestPoint = FindClosestPoint(splinePointsCentre, numSplineCentrePoints, hmapWorldPos3D);

	// Find the closest of its consecutive points
	uint secondclosestPoint = FindClosestConsecutivePoint(splinePointsCentre, numSplineCentrePoints, hmapWorldPos3D, closestPoint);

	uint firstPtIdx = (closestPoint < secondclosestPoint ? closestPoint : secondclosestPoint);
	uint secondPtIdx = (closestPoint < secondclosestPoint ? secondclosestPoint : closestPoint);

	// Initialise the out params
	quadP1 = float3(splinePointsLeft[firstPtIdx].x, 0.0, splinePointsLeft[firstPtIdx].z);
	quadP2 = float3(splinePointsLeft[secondPtIdx].x, 0.0, splinePointsLeft[secondPtIdx].z);
	quadP3 = float3(splinePointsRight[firstPtIdx].x, 0.0, splinePointsRight[firstPtIdx].z);
	quadP4 = float3(splinePointsRight[secondPtIdx].x, 0.0, splinePointsRight[secondPtIdx].z);

	widthPoint = firstPtIdx;
	firstMatchPtIdx = firstPtIdx;
	secondMatchPtIdx = secondPtIdx;

	// Now check if the world position is in this section of the path
	bool isMatch = IsInQuad(quadP1, quadP2, quadP3, quadP4, hmapWorldPos3D);

	// This however, isn't enough to entirely disprove that this point isn't in the path - the path section it is in isn't necessarily
	// the one with the closest path points. So we need to check the other path sections

	// Check the previous quads to get all edge fragments from the outside of corners and the inside of the last curve
	for (uint prevIdx = 0; !isMatch && firstPtIdx > prevIdx && prevIdx < quadLookAhead; prevIdx++)
	{
		quadP1.x = splinePointsLeft[firstPtIdx - prevIdx - 1].x;
		quadP1.z = splinePointsLeft[firstPtIdx - prevIdx - 1].z;
		quadP2.x = splinePointsLeft[firstPtIdx - prevIdx].x;
		quadP2.z = splinePointsLeft[firstPtIdx - prevIdx].z;
		quadP3.x = splinePointsRight[firstPtIdx - prevIdx - 1].x;
		quadP3.z = splinePointsRight[firstPtIdx - prevIdx - 1].z;
		quadP4.x = splinePointsRight[firstPtIdx - prevIdx].x;
		quadP4.z = splinePointsRight[firstPtIdx - prevIdx].z;

		widthPoint = firstPtIdx - prevIdx;
		firstMatchPtIdx = firstPtIdx - prevIdx - 1;
		secondMatchPtIdx = firstPtIdx - prevIdx;

		isMatch = IsInQuad(quadP1, quadP2, quadP3, quadP4, hmapWorldPos3D);
	}

	// Check the next quads to get all edge fragments from the inside of corners and the outside of the last curve
	for (uint nextIdx = 0; !isMatch && secondPtIdx + nextIdx + 1 < numSplineCentrePoints && nextIdx < quadLookAhead; nextIdx++)
	{
		quadP1.x = splinePointsLeft[secondPtIdx + nextIdx].x;
		quadP1.z = splinePointsLeft[secondPtIdx + nextIdx].z;
		quadP2.x = splinePointsLeft[secondPtIdx + nextIdx + 1].x;
		quadP2.z = splinePointsLeft[secondPtIdx + nextIdx + 1].z;
		quadP3.x = splinePointsRight[secondPtIdx + nextIdx].x;
		quadP3.z = splinePointsRight[secondPtIdx + nextIdx].z;
		quadP4.x = splinePointsRight[secondPtIdx + nextIdx + 1].x;
		quadP4.z = splinePointsRight[secondPtIdx + nextIdx + 1].z;

		widthPoint = secondPtIdx + nextIdx;
		firstMatchPtIdx = secondPtIdx + nextIdx;
		secondMatchPtIdx = secondPtIdx + nextIdx + 1;

		isMatch = IsInQuad(quadP1, quadP2, quadP3, quadP4, hmapWorldPos3D);
	}

	// Fill in the gap halfway between the second last spline point and the
	// last spline point (which should be the same as the first spline point).
	if (!isMatch && closedCircuit)
	{
		quadP1.x = splinePointsLeft[numSplineCentrePoints - 2].x;
		quadP1.z = splinePointsLeft[numSplineCentrePoints - 2].z;
		quadP2.x = splinePointsLeft[0].x;
		quadP2.z = splinePointsLeft[0].z;
		quadP3.x = splinePointsRight[numSplineCentrePoints - 2].x;
		quadP3.z = splinePointsRight[numSplineCentrePoints - 2].z;
		quadP4.x = splinePointsRight[0].x;
		quadP4.z = splinePointsRight[0].z;

		widthPoint = 0;
		firstMatchPtIdx = numSplineCentrePoints - 2;
		secondMatchPtIdx = numSplineCentrePoints - 1;

		isMatch = IsInQuad(quadP1, quadP2, quadP3, quadP4, hmapWorldPos3D);
	}

	return isMatch;
}

// Given a world position and a path section (defined by two path point indices)
// returns whether the world position is within that section of the path
// Also returns the centre interpolated path point (i.e. the world position projected onto the centre spline), 
// the 0-1 normalised position of the centre interpolated path point between the provided path points (0 = first point, 1 = second point),
// the XZ distance from the world position to the centre interpolated path point and the full path width at the world position
float3 GetCentreInterpolatedPathPoint(float3 hmapWorldPos3D, uint firstMatchPtIdx, uint secondMatchPtIdx, 
	out float centreNormalisedPos, out float hmapWorldPosToInterpolatedDist, out float fullPathWidth)
{
	float3 firstMatchCentrePt = splinePointsCentre[firstMatchPtIdx];
	float3 secondMatchCentrePt = splinePointsCentre[secondMatchPtIdx];

	// Get vectors from first to second point and from first to sample point
	float3 firstToSecondPt = secondMatchCentrePt - firstMatchCentrePt;
	float3 firstToSamplePt = hmapWorldPos3D - firstMatchCentrePt;
	// Discard y componentry
	firstToSecondPt.y = 0.0;
	firstToSamplePt.y = 0.0;
	// Get the length from first to second point
	float firstToSecondDist = length(firstToSecondPt);

	//            _Sample Pt
	//          _/    |
	//        _/      |
	//      _/      __|
	//    _/ 0     |  |<------ right angle
	// Pt 1 ------ Centre Pt ------------------ Pt 2
	//  <------------>
	//     First to 
	//   interpolated
	//  <---------------------------------------->
	//                First to second
	//
	// Now for the clever part: Calculate the distance from first point to the interpolated centre point
	// using the fact that the magnitude of the dot product of two vectors is the product of the magnitude of
	// the two vectors and the cosine of the angle between them
	// Since we know from trigonometry that the distance from the first point to the interpolated centre point is
	// the distance from the first point to the sample point times the cosine of the angle between the two vectors
	// (designated 0 in the diagram), we just take the magnitude of the dot product of firstToSecondPt and firstToSamplePt
	// and divide it by the magnitude of firstToSecondPt
	float firstToInterpolatedDist = length(dot(firstToSecondPt, firstToSamplePt)) / firstToSecondDist;
	// Now use this distance to calculate the how far we are along the line between the first and second points as a 0-1 value
	centreNormalisedPos = firstToInterpolatedDist / firstToSecondDist;
	// From this, we can directly calculate the position of the interpolated centre point
	float3 centreInterpolatedPathPt = firstMatchCentrePt + ((secondMatchCentrePt - firstMatchCentrePt) * centreNormalisedPos);

	// Calculate the XZ distance from the sample point to the interpolated centre point
	hmapWorldPosToInterpolatedDist = length(centreInterpolatedPathPt.xz - hmapWorldPos3D.xz);
	// Get the width of the path at this point (including the surroundings)
	fullPathWidth = GetWidthOnSpline2D(firstMatchPtIdx, secondMatchPtIdx, centreNormalisedPos);

	// Finally, return the position of the interpolated centre point
	return centreInterpolatedPathPt;
}


