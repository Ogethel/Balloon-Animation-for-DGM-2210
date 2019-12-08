// Landscape Builder. Copyright (c) 2016-2019 SCSM Pty Ltd. All rights reserved.
// Common calc methods and variables used in LBCGLib.cginc, LBCSxx.compute shaders

// ------------ GENERAL MATHS METHODS ------------

// Normalise the value "x" to return values between 0 and 1
// given the potential range between "a" and "b"
// If "b" less than or equal to "a" this funtion will always return 0
float LBNormalise(float x, float a, float b)
{
	if (b <= a) { return 0.0; }
	else { return ((x - a) * (1.0 / (b - a))); }
}

// Faster version of Mathf.Pow for integer exponents
float IntPow(float num, int pow)
{
	if (pow != 0)
	{
		float ans = num;
		for (int i = 1; i < pow; i++)
		{
			ans *= num;
		}
		return ans;
	}
	else { return 1.0; }
}

// Calculates the linear paramater time that creates the interpolated value between a and b
float LBInverseLerp(float a, float b, float v)
{
	if (a >= b) { return 0.0; }
	else if (v < a) { v = a; }
	else if (v > b) { v = b; }
	return (v - a) / (b - a);
}

// ------------ CURVE METHODS ------------

// Evaluates a point on a curve, given a set of packed curve keyframes
float LBEvaluate(float time, StructuredBuffer<float4> pkKeyFrames, uint numKeyFrames)
{
	// pkKeyFrame format: x = time, y = value, z = inTangent, w = outTangent
	// afaik Unity doesn't normalise the time range of the curve

	// Leave the input value unchanged if there are less than two curve keys
	if (numKeyFrames < 2) { return time; }

	// TODO: How does unity handle this?
	// If the input value is outside of the curve's time range, simply output the first or last value
	if (time <= pkKeyFrames[0].x) { return pkKeyFrames[0].y; }
	else if (time >= pkKeyFrames[numKeyFrames - 1].x) { return pkKeyFrames[numKeyFrames - 1].y; }

	uint p1kIdx = 0;
	uint p2kIdx = 1;

	for (uint kIdx = 1; kIdx < numKeyFrames; kIdx++)
	{
		// Find the first keyframe greater than or equal to time
		if (time <= pkKeyFrames[kIdx].x) { p2kIdx = kIdx; p1kIdx = kIdx - 1; break; }
	}

	// Get normalised t value - we need this for the cubic hermite spline
	float T = LBInverseLerp(pkKeyFrames[p1kIdx].x, pkKeyFrames[p2kIdx].x, time);
	// Get t squared (T2) and t cubed (T3)
	float T2 = T * T;
	float T3 = T * T2;
	// Get start and end points
	float p1 = pkKeyFrames[p1kIdx].y;
	float p2 = pkKeyFrames[p2kIdx].y;
	// Get start and end gradients - these are scaled by the t-range
	// We need to scale the gradients because we have normalised t
	float m1 = pkKeyFrames[p1kIdx].w * (pkKeyFrames[p2kIdx].x - pkKeyFrames[p1kIdx].x);
	float m2 = pkKeyFrames[p2kIdx].z * (pkKeyFrames[p2kIdx].x - pkKeyFrames[p1kIdx].x);
	// Use cubic hermite spline
	return (2.0*T3 - 3.0*T2 + 1.0)*p1 + (T3 - 2.0*T2 + T)*m1 + (-2.0*T3 + 3.0*T2)*p2 + (T3 - T2)*m2;
}

// Evaluates a point on a curve, given a set of packed curve keyframes within a larger set of keyframes.
// The curve starts at position keyFrameOffset within the larger buffer. 
float LBEvaluate(float time, StructuredBuffer<float4> pkKeyFrames, uint numKeyFrames, uint keyFrameOffset)
{
	// pkKeyFrame format: x = time, y = value, z = inTangent, w = outTangent
	// afaik Unity doesn't normalise the time range of the curve

	// Leave the input value unchanged if there are less than two curve keys
	if (numKeyFrames < 2) { return time; }

	// TODO: How does unity handle this?
	// If the input value is outside of the curve's time range, simply output the first or last value
	if (time <= pkKeyFrames[keyFrameOffset].x) { return pkKeyFrames[keyFrameOffset].y; }
	else if (time >= pkKeyFrames[keyFrameOffset + numKeyFrames - 1].x) { return pkKeyFrames[keyFrameOffset + numKeyFrames - 1].y; }

	uint p1kIdx = keyFrameOffset;
	uint p2kIdx = keyFrameOffset + 1;

	for (uint kIdx = keyFrameOffset + 1; kIdx < keyFrameOffset + numKeyFrames; kIdx++)
	{
		// Find the first keyframe greater than or equal to time
		if (time <= pkKeyFrames[kIdx].x) { p2kIdx = kIdx; p1kIdx = kIdx - 1; break; }
	}

	// Get normalised t value - we need this for the cubic hermite spline
	float T = LBInverseLerp(pkKeyFrames[p1kIdx].x, pkKeyFrames[p2kIdx].x, time);
	// Get t squared (T2) and t cubed (T3)
	float T2 = T * T;
	float T3 = T * T2;
	// Get start and end points
	float p1 = pkKeyFrames[p1kIdx].y;
	float p2 = pkKeyFrames[p2kIdx].y;
	// Get start and end gradients - these are scaled by the t-range
	// We need to scale the gradients because we have normalised t
	float m1 = pkKeyFrames[p1kIdx].w * (pkKeyFrames[p2kIdx].x - pkKeyFrames[p1kIdx].x);
	float m2 = pkKeyFrames[p2kIdx].z * (pkKeyFrames[p2kIdx].x - pkKeyFrames[p1kIdx].x);
	// Use cubic hermite spline
	return (2.0*T3 - 3.0*T2 + 1.0)*p1 + (T3 - 2.0*T2 + T)*m1 + (-2.0*T3 + 3.0*T2)*p2 + (T3 - T2)*m2;
}

// Evaluates a point on a "wide range" curve
// This curve is 0 at the start and ends and 1 between t = 0.25 and t = 0.75, 
// and linearly interpolates between the points in the first and last quarters
float LBEvaluateWideRangeCurve(float t)
{
	// Middle range: all values are 1
	if (t >= 0.25 && t <= 0.75) { return 1.0; }
	else if (t < 0.25)
	{
		// Don't allow values less than 0
		if (t < 0.0) { t = 0.0; }
		// First quarter: Gradient of +4
		return 4.0 * t;
	}
	else
	{
		// Don't allow values more than 1
		if (t > 1.0) { t = 1.0; }
		// Final quarter: Gradient of -4
		return 4.0 * (1.0 - t);
	}
}


// ------------ SHAPE AND POINT MATHS METHODS ------------

// Square distance calculation ignoring y distance
float PlanarSquareDistance(float3 p1, float3 p2)
{
	// Basically pythagoras but without y and without final square root
	return (((p1.x - p2.x) * (p1.x - p2.x)) + ((p1.z - p2.z) * (p1.z - p2.z)));
}

float HalfPlaneSideSign(float3 p1, float3 p2, float3 p3)
{
	return (p1.x - p3.x) * (p2.z - p3.z) - (p2.x - p3.x) * (p1.z - p3.z);
}

// The square distance from a point samplept to a line from sp1 to sp2
float SquareDistanceToSide(float3 sp1, float3 sp2, float3 samplept)
{
	float squareSideLength = PlanarSquareDistance(sp1, sp2);
	float dotProduct = ((samplept.x - sp1.x) * (sp2.x - sp1.x) + (samplept.z - sp1.z) * (sp2.z - sp1.z)) / squareSideLength;
	if (dotProduct < 0.0)
	{
		return PlanarSquareDistance(samplept, sp1);
	}
	else if (dotProduct <= 1.0)
	{
		return PlanarSquareDistance(samplept, sp1) - dotProduct * dotProduct * squareSideLength;
	}
	else
	{
		return PlanarSquareDistance(samplept, sp2);
	}
}

// Is the sample point inside the triangle which has points: p1, p2 & p3
bool IsInTriangle(float3 p1, float3 p2, float3 p3, float3 samplept)
{
	bool halfPlaneSide1 = HalfPlaneSideSign(samplept, p1, p2) < 0.0;
	bool halfPlaneSide2 = HalfPlaneSideSign(samplept, p2, p3) < 0.0;
	bool halfPlaneSide3 = HalfPlaneSideSign(samplept, p3, p1) < 0.0;

	return ((halfPlaneSide1 == halfPlaneSide2) && (halfPlaneSide2 == halfPlaneSide3));
}

// TODO: Clean up this new function
// Is the sample point inside the quad which has points p1, p2, p3 and p4?
bool IsInQuad(float3 p1, float3 p2, float3 p3, float3 p4, float3 samplept)
{
	// This is the orientation of named vertices i.e. not named by moving in a circular direction around the quad
	// p1 --- p2
	// |      |
	// |      |
	// p3 --- p4

	// This is a valid quad, so check it in the usual way
	return (IsInTriangle(p1, p2, p3, samplept) || IsInTriangle(p4, p2, p3, samplept));

	/*return (IsInTriangle(p1, p2, p3, samplept) || IsInTriangle(p2, p3, p4, samplept) ||
		IsInTriangle(p1, p3, p4, samplept) || IsInTriangle(p1, p2, p4, samplept));*/

	//// First check that this is a valid quad i.e. that the sides don't cross
	//// Easiest way to check this is to check that p1 and p4 are on opposite sides of the line formed by p2 and p3
	//if (HalfPlaneSideSign(p1, p2, p3) < 0.0 != HalfPlaneSideSign(p4, p2, p3) < 0.0)
	//{
	//	// This is a valid quad, so check it in the usual way
	//	return (IsInTriangle(p1, p2, p3, samplept) || IsInTriangle(p4, p2, p3, samplept));
	//}
	//else
	//{
	//	// This is not a valid quad, so we need to check it a bit differently
	//	// First we need to find which sides intersect, and where it occurs
	//	// HACKY METHOD:
	//	//return (IsInTriangle(p1, p2, p3, samplept) || IsInTriangle(p2, p3, p4, samplept) ||
	//	//	    IsInTriangle(p1, p3, p4, samplept) || IsInTriangle(p1, p2, p4, samplept));
	//	return false;
	//}
}

// Find the closest point to pointToMatch in a buffer (array) of points
uint FindClosestPoint(StructuredBuffer<float3> pointsBuf, uint numPoints, float3 pointToMatch)
{
	float sqrDist = 0.0;
	float closestSqrDist = 1.0e+12;
	int closestPoint = 0;

	for (uint i = 0; i < numPoints; i++)
	{
		sqrDist = PlanarSquareDistance(pointsBuf[i], pointToMatch);
		if (sqrDist < closestSqrDist) { closestSqrDist = sqrDist; closestPoint = i; }
	}

	return closestPoint;
}

// Find closest consecutive path point to this one
uint FindClosestConsecutivePoint(StructuredBuffer<float3> pointsBuf, uint numPoints, float3 pointToMatch, uint consecutiveTo)
{
	uint closestPoint = 0;

	if (numPoints > 0)
	{
		// Check if the consecutive points exist
		bool c1Exists = consecutiveTo - (uint)1 >= 0;
		bool c2Exists = numPoints > consecutiveTo + 1;
		if (c1Exists && c2Exists)
		{
			// Compare the distances to both of the consecutive points, return the closest point
			if (PlanarSquareDistance(pointsBuf[consecutiveTo - (uint)1], pointToMatch) < PlanarSquareDistance(pointsBuf[consecutiveTo + (uint)1], pointToMatch))
			{
				closestPoint = consecutiveTo - (uint)1;
			}
			else { closestPoint = consecutiveTo + (uint)1; }
		}
		// Return any point that exists
		else if (c1Exists) { closestPoint = consecutiveTo - (uint)1; }
		else if (c2Exists) { closestPoint = consecutiveTo + (uint)1; }
	}

	return closestPoint;
}

// 3D distance between 2 points (uses a slow sqrt)
float LBDistance3D(float3 p1, float3 p2)
{
	float3 v = float3(p1.x - p2.x, p1.y - p2.y, p1.z - p2.z);
	return sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
}

// Get the square length or magnitude of a vector
float LBSquareMagnitude(float3 v)
{
	return (v.x * v.x + v.y * v.y + v.z * v.z);
}

// Get the length or magnitude of a vector
float LBMagnitude(float3 v)
{
	return sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
}

// Normalise a vector
float3 LBNormalizeV(float3 v)
{
	// Get the magnitude of the vector
	float m = LBMagnitude(v);
	if (m > 0.00001) { return float3(v.x / m, v.y / m, v.z / m); }
	else { return float3(0.0, 0.0, 0.0); }
}

// ------------ COLOUR METHODS ------------

// Gets the grayscale value of a RGB colour. Alpha is not required.
float LBGrayScale(float3 colour)
{
	return (colour.x * 0.299) + (colour.y * 0.587) + (colour.z * 0.114);
}
