using System;

namespace UnityEngine.GsplEdit
{
    public struct EdgeKey : IEquatable<EdgeKey>
{
    public Vector3 Start { get; }
    public Vector3 End { get; }

    public EdgeKey(Vector3 start, Vector3 end)
    {
        Start = start;
        End = end;
    }

    public bool Equals(EdgeKey other)
    {
        const float epsilon = 1e-3f;

        bool directMatch = Vector3.Distance(Start, other.Start) < epsilon && Vector3.Distance(End, other.End) < epsilon;
        bool reverseMatch = Vector3.Distance(Start, other.End) < epsilon && Vector3.Distance(End, other.Start) < epsilon;

        return directMatch || reverseMatch;
    }

    public override bool Equals(object obj)
    {
        return obj is EdgeKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        // Order-independent hash: use XOR of component-wise hashes
        // Optional: sort the points first to ensure stable results
        int h1 = Start.GetHashCode();
        int h2 = End.GetHashCode();
        return h1 ^ h2;
    }

    public static bool operator ==(EdgeKey left, EdgeKey right) => left.Equals(right);
    public static bool operator !=(EdgeKey left, EdgeKey right) => !left.Equals(right);
}

}