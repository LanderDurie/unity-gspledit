using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.GsplEdit
{
    public struct EdgeKey : IEquatable<EdgeKey>
{
    public Vector3 Start { get; }
    public Vector3 End { get; }

    public EdgeKey(Vector3 start, Vector3 end)
    {
        // Store the points as-is (no reordering)
        Start = start;
        End = end;
    }

    // Override Equals to compare two EdgeKey instances
    public bool Equals(EdgeKey other)
    {
        return Start == other.Start && End == other.End;
    }

    public bool IsSubsection(EdgeKey other, float tolerance = 0.0001f)
    {
        // Edge vectors
        Vector3 selfDir = End - Start;
        Vector3 otherStartDir = other.Start - Start;
        Vector3 otherEndDir = other.End - Start;

        // Cross product to check collinearity (should be near zero)
        bool areCollinear = 
            Vector3.Cross(selfDir, otherStartDir).sqrMagnitude <= tolerance &&
            Vector3.Cross(selfDir, otherEndDir).sqrMagnitude <= tolerance;

        if (!areCollinear)
            return false;

        // Projection factors (0 = Start, 1 = End)
        float selfLengthSq = selfDir.sqrMagnitude;
        float tStart = Vector3.Dot(selfDir, otherStartDir) / selfLengthSq;
        float tEnd = Vector3.Dot(selfDir, otherEndDir) / selfLengthSq;

        // Check if both points lie within [0, 1] (with tolerance)
        bool startInRange = tStart >= -tolerance && tStart <= 1 + tolerance;
        bool endInRange = tEnd >= -tolerance && tEnd <= 1 + tolerance;

        return startInRange && endInRange;
    }

    // Override GetHashCode to generate a hash code for the EdgeKey
    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + Start.GetHashCode();
            hash = hash * 23 + End.GetHashCode();
            return hash;
        }
    }

    // Override Equals for general object comparison
    public override bool Equals(object obj)
    {
        return obj is EdgeKey other && Equals(other);
    }

    // Override == and != operators for convenience
    public static bool operator ==(EdgeKey left, EdgeKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EdgeKey left, EdgeKey right)
    {
        return !left.Equals(right);
    }
}
}