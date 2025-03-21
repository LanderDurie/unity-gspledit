using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.GsplEdit;

public class DualTest : MonoBehaviour
{
    public Color m_GizmoColor = Color.green;
    public Color m_IntersectionColor = Color.red;
    public Color m_GradientColor = Color.blue;
    public int m_ParticleIterations = 10;
    public float m_ForceScale = 0.05f; // 5% as mentioned in the paper
    public float m_ConvergenceThreshold = 0.001f;
    public int m_BinarySearchIterations = 10;
    public float m_DebugSize = 1.0f;
    public bool m_drawDebug = false;
    public GameObject m_DynamicSplatObject;
    private MeshUtils.SplatData[] m_SplatArray;
    public float m_SplatScale = 4.0f;
    public float m_Threshold = 0.1f;
    public ComputeShader m_IcosahedronComputeShader;

    private readonly Vector3[] m_CubeVertices = new Vector3[8];
    private readonly Vector3[] m_EdgeIntersections = new Vector3[12];
    private readonly Vector3[] m_EdgeGradients = new Vector3[12];
    private readonly bool[] m_HasIntersection = new bool[12];
    private Vector3 m_OptimalVertex;
    private bool m_HasValidVertex;

    private static readonly int[,] edges = new int[,] {
        {0,1}, {1,2}, {2,3}, {3,0},  // bottom edges
        {4,5}, {5,6}, {6,7}, {7,4},  // top edges
        {0,4}, {1,5}, {2,6}, {3,7}   // vertical edges
    };

    unsafe public void Initialize() {
        SharedComputeContext context = m_DynamicSplatObject.GetComponent<DynamicSplat>().GetContext();
        
        Vector3 size = context.gsSplatData.boundsMax - context.gsSplatData.boundsMin;
        Vector3 center = (context.gsSplatData.boundsMax + context.gsSplatData.boundsMin) * 0.5f;

        int splatCount = context.gsSplatData.splatCount;
        int itemsPerDispatch = 65535;

        m_SplatArray = new MeshUtils.SplatData[splatCount];

        using (ComputeBuffer IcosahedronBuffer = new ComputeBuffer(splatCount, sizeof(MeshUtils.SplatData)))
        {
            IcosahedronBuffer.SetData(m_SplatArray);
            m_IcosahedronComputeShader.SetFloat("_GlobalScaleFactor",m_SplatScale);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatPos", context.gsPosData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatOther", context.gsOtherData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatSH", context.gsSHData);
            m_IcosahedronComputeShader.SetBuffer(0, "_SplatChunks", context.gsChunks);
            m_IcosahedronComputeShader.SetInt("_SplatChunkCount", context.gsChunksValid ? context.gsChunks.count : 0);
            uint format = (uint)context.gsSplatData.posFormat | ((uint)context.gsSplatData.scaleFormat << 8) | ((uint)context.gsSplatData.shFormat << 16);
            m_IcosahedronComputeShader.SetInt("_SplatFormat", (int)format);
            m_IcosahedronComputeShader.SetTexture(0, "_SplatColor", context.gsColorData);
            m_IcosahedronComputeShader.SetBuffer(0, "_IcosahedronBuffer", IcosahedronBuffer);

            for (int i = 0; i < Mathf.CeilToInt((float)splatCount / itemsPerDispatch); i++)
            {
                int offset = i * itemsPerDispatch;
                m_IcosahedronComputeShader.SetInt("_Offset", offset);
                int currentDispatchSize = Mathf.Min(splatCount - offset, itemsPerDispatch);
                m_IcosahedronComputeShader.Dispatch(0, currentDispatchSize, 1, 1);
            }

            IcosahedronBuffer.GetData(m_SplatArray);
        }
    }

    public void Bake() {
        FindEdgeIntersections();
        foreach (var i in m_HasIntersection) {
        Debug.Log(i);

        }
        FindOptimalVertex();
    }

    private void OnDrawGizmos()
    {
        InitializeCubeVertices();

        Gizmos.color = m_GizmoColor;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
        if (m_drawDebug) {
            Gizmos.DrawWireCube(Vector3.zero, transform.localScale);
        }
        Gizmos.matrix = Matrix4x4.identity;

        if (m_drawDebug)
        {
            Vector3? centroid = GetCentroid();
            if (centroid == null)
                return;
                
            Vector3[] gridForces = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                gridForces[i] = CalculateGridPointForce(m_CubeVertices[i]);
                Handles.DrawBezier(
                    m_CubeVertices[i],
                    m_CubeVertices[i] + gridForces[i],
                    m_CubeVertices[i],
                    m_CubeVertices[i] + gridForces[i],
                    Color.magenta,
                    null,
                    7
                );
            }

            Gizmos.DrawSphere(centroid.Value, 0.1f * m_DebugSize);

            Vector3 particlePos = centroid.Value;
            Vector3 prevPos = particlePos;

            for (int iter = 0; iter < m_ParticleIterations; iter++)
            {
                Vector3 force = InterpolateForce(particlePos, gridForces) * m_ForceScale;
                Handles.DrawBezier(particlePos, particlePos + force, particlePos, particlePos + force, Color.red, null, 3);

                particlePos += force;
                particlePos = ClampWithinCube(particlePos);

                if ((particlePos - prevPos).sqrMagnitude < m_ConvergenceThreshold * m_ConvergenceThreshold)
                    break;

                prevPos = particlePos;
            }

            Gizmos.color = m_IntersectionColor;
            for (int i = 0; i < 12; i++)
            {
                if (m_HasIntersection[i])
                {
                    Gizmos.DrawSphere(m_EdgeIntersections[i], 0.05f * m_DebugSize);

                    Gizmos.color = m_GradientColor;
                    Handles.DrawBezier(m_EdgeIntersections[i], m_EdgeIntersections[i] + m_EdgeGradients[i], m_EdgeIntersections[i], m_EdgeIntersections[i] + m_EdgeGradients[i], Color.blue, null, 7);
                    Gizmos.color = m_IntersectionColor;
                }
            }
        }

        if (m_HasValidVertex)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(m_OptimalVertex, 0.1f * m_DebugSize);
        }

        Gizmos.matrix = oldMatrix;
    }

    private float ScalarField(Vector3 position)
    {
        float accumulatedOpacity = 0f;
            float minDistance = float.MaxValue;

            foreach (var splat in m_SplatArray)
            {
                // Calculate the combined inverse rotation and scale matrix
                Matrix4x4 invSplatRot_ScaleMat = Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(splat.rot), Vector3.one)
                                                * Matrix4x4.Scale(new Vector3(1.0f / splat.scale.x, 1.0f / splat.scale.y, 1.0f / splat.scale.z));

                // Apply the transformation to the offset
                Vector3 offset = position - splat.center;
                Vector3 transformedPos = invSplatRot_ScaleMat.MultiplyPoint3x4(offset);

                // Calculate squared distance
                float distanceSquared = transformedPos.sqrMagnitude;

                // Adjust opacity calculation to include the / 2.0f factor
                float opacity = splat.opacity * Mathf.Exp(-distanceSquared / 2.0f);
                accumulatedOpacity += opacity;

                float actualDistance = offset.sqrMagnitude;
                minDistance = Mathf.Min(minDistance, actualDistance);
            }
        
            return accumulatedOpacity > 0.01 ? accumulatedOpacity : -minDistance;
    }

    private Vector3 CalculateGridPointForce(Vector3 gridPoint)
    {
        Vector3 totalForce = Vector3.zero;
        int intersectionCount = 0;

        for (int i = 0; i < 12; i++)
        {
            if (m_HasIntersection[i])
            {
                Vector3 normal = m_EdgeGradients[i];
                float distance = Vector3.Dot(normal, m_EdgeIntersections[i] - gridPoint);
                totalForce += normal * distance;
                intersectionCount++;
            }
        }

        return intersectionCount > 0 ? totalForce / intersectionCount : Vector3.zero;
    }

    private Vector3 InterpolateForce(Vector3 position, Vector3[] gridForces)
    {
        Vector3 C = transform.InverseTransformPoint(position) + transform.localScale / 2;
        Vector3 L = transform.localScale;

        float ratioX = C.x / L.x;
        float ratioY = C.y / L.y;
        float ratioZ = C.z / L.z;

        Vector3 Fl1 = (1 - ratioX) * gridForces[0] + ratioX * gridForces[1];
        Vector3 Fl2 = (1 - ratioX) * gridForces[3] + ratioX * gridForces[2];
        Vector3 Fl3 = (1 - ratioX) * gridForces[4] + ratioX * gridForces[5];
        Vector3 Fl4 = (1 - ratioX) * gridForces[7] + ratioX * gridForces[6];

        Vector3 Fb1 = (1 - ratioY) * Fl1 + ratioY * Fl3;
        Vector3 Fb2 = (1 - ratioY) * Fl2 + ratioY * Fl4;

        return (1 - ratioZ) * Fb1 + ratioZ * Fb2;
    }

    private void InitializeCubeVertices()
    {
        Vector3 halfSize = Vector3.one * 0.5f;
        m_CubeVertices[0] = TransformPoint(new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));
        m_CubeVertices[1] = TransformPoint(new Vector3(halfSize.x, -halfSize.y, -halfSize.z));
        m_CubeVertices[2] = TransformPoint(new Vector3(halfSize.x, -halfSize.y, halfSize.z));
        m_CubeVertices[3] = TransformPoint(new Vector3(-halfSize.x, -halfSize.y, halfSize.z));
        m_CubeVertices[4] = TransformPoint(new Vector3(-halfSize.x, halfSize.y, -halfSize.z));
        m_CubeVertices[5] = TransformPoint(new Vector3(halfSize.x, halfSize.y, -halfSize.z));
        m_CubeVertices[6] = TransformPoint(new Vector3(halfSize.x, halfSize.y, halfSize.z));
        m_CubeVertices[7] = TransformPoint(new Vector3(-halfSize.x, halfSize.y, halfSize.z));
    }

    private Vector3? GetCentroid()
    {
        int intersectionCount = 0;
        Vector3 centroid = Vector3.zero;

        for (int i = 0; i < 12; i++)
        {
            if (m_HasIntersection[i])
            {
                centroid += m_EdgeIntersections[i];
                intersectionCount++;
            }
        }

        if (intersectionCount == 0)
        {
            m_HasValidVertex = false;
            return null;
        }

        centroid /= intersectionCount;
        return centroid;
    }

    private Vector3 ClampWithinCube(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        Vector3 halfExtents = transform.localScale / 2;

        localPos.x = Mathf.Clamp(localPos.x, -halfExtents.x, halfExtents.x);
        localPos.y = Mathf.Clamp(localPos.y, -halfExtents.y, halfExtents.y);
        localPos.z = Mathf.Clamp(localPos.z, -halfExtents.z, halfExtents.z);

        return transform.TransformPoint(localPos);
    }

    private void FindOptimalVertex()
    {
        if (GetCentroid() is not Vector3 centroid)
            return;

        Vector3[] gridForces = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            gridForces[i] = CalculateGridPointForce(m_CubeVertices[i]);
        }

        Vector3 particlePos = centroid;
        Vector3 prevPos = particlePos;

        for (int iter = 0; iter < m_ParticleIterations; iter++)
        {
            Vector3 force = InterpolateForce(particlePos, gridForces) * m_ForceScale;
            particlePos += force;
            particlePos = ClampWithinCube(particlePos);

            if ((particlePos - prevPos).sqrMagnitude < m_ConvergenceThreshold * m_ConvergenceThreshold)
                break;

            prevPos = particlePos;
        }

        m_OptimalVertex = particlePos;
        m_HasValidVertex = true;
    }

    private Vector3 CalculateGradient(Vector3 position)
    {
        float epsilon = 0.0001f;
        return new Vector3(
            (ScalarField(position + new Vector3(epsilon, 0, 0)) - ScalarField(position - new Vector3(epsilon, 0, 0))) / (2 * epsilon),
            (ScalarField(position + new Vector3(0, epsilon, 0)) - ScalarField(position - new Vector3(0, epsilon, 0))) / (2 * epsilon),
            (ScalarField(position + new Vector3(0, 0, epsilon)) - ScalarField(position - new Vector3(0, 0, epsilon))) / (2 * epsilon)
        ).normalized;
    }

    private Vector3 TransformPoint(Vector3 localPoint)
    {
        return transform.TransformPoint(Vector3.Scale(localPoint, transform.localScale));
    }

    private Vector3 BinarySearchIntersection(Vector3 v1, Vector3 v2)
    {
        float f1 = ScalarField(v1);
        float f2 = ScalarField(v2);

        Vector3 a = v1;
        Vector3 b = v2;

        for (int i = 0; i < m_BinarySearchIterations; i++)
        {
            Vector3 mid = (a + b) * 0.5f;
            float fmid = ScalarField(mid);

            if (Mathf.Abs(fmid) < 1e-6f)
                return mid;

            if (fmid * f1 < 0)
            {
                b = mid;
                f2 = fmid;
            }
            else
            {
                a = mid;
                f1 = fmid;
            }
        }

        return (a + b) * 0.5f;
    }

    private void FindEdgeIntersections()
    {
        for (int i = 0; i < 12; i++)
        {
            Vector3 v1 = m_CubeVertices[edges[i, 0]];
            Vector3 v2 = m_CubeVertices[edges[i, 1]];

            float f1 = ScalarField(v1);
            float f2 = ScalarField(v2);
            Debug.Log($"{f1}, {f2}");

            if (f1 * f2 < 0)
            {
                m_EdgeIntersections[i] = BinarySearchIntersection(v1, v2);
                m_EdgeGradients[i] = CalculateGradient(m_EdgeIntersections[i]);
                m_HasIntersection[i] = true;
            }
            else
            {
                m_HasIntersection[i] = false;
            }
        }
    }
}




[CustomEditor(typeof(DualTest))]
public class DualTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DualTest script = (DualTest)target;
        if (GUILayout.Button("Initialize"))
        {
            script.Initialize();
        }

        if (GUILayout.Button("Bake"))
        {
            script.Bake();
        }
    }
}
