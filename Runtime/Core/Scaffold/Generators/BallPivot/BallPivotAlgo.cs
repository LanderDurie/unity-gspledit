using System;
using System.Collections.Generic;
using UnityEngine;

using renge_pcl;
using System.Linq;


public static class BPAProcessor
{
    public static Mesh ComputeMesh(UnityEngine.GsplEdit.OctreeNode[] nodes, float pivotingRadius, List<UnityEngine.GsplEdit.MeshUtils.SplatData> splats)
    {
        float h = 0.001f; // Small step for finite difference
        PointCloud<PointNormal> cloud = new PointCloud<PointNormal>(nodes.Count());

        foreach (var node in nodes)
        {
            Vector3 pos = node.m_Bounds.center;

            // // Finite difference approximation of gradient (central difference)
            // float fx = (node.EvaluateSDF(pos + new Vector3(h, 0, 0), 0, splats) - node.EvaluateSDF(pos - new Vector3(h, 0, 0), 0, splats)) / (2f * h);
            // float fy = (node.EvaluateSDF(pos + new Vector3(0, h, 0), 0, splats) - node.EvaluateSDF(pos - new Vector3(0, h, 0), 0, splats)) / (2f * h);
            // float fz = (node.EvaluateSDF(pos + new Vector3(0, 0, h), 0, splats) - node.EvaluateSDF(pos - new Vector3(0, 0, h), 0, splats)) / (2f * h);

            // Vector3 normal = new Vector3(fx, fy, fz).normalized;

            cloud.Add(new PointNormal(pos.x, pos.y, pos.z, 0, 0, 1));
        }

        BallPivotingAlgorithm bpa = new BallPivotingAlgorithm();
        float[] passes = new float[] { pivotingRadius };
        Debug.Log("start");
        Mesh m = bpa.Run(cloud, passes);
        Debug.Log("end");
        return m;
    }
}



public enum PointMeshType {
	Sphere
}

[RequireComponent(typeof(MeshFilter))]
public class BallPivotingAlgorithm {
	Front f;
	PointCloud<PointNormal> cloud;
	float ballRadius = 3;
	List<Triangle> preMesh;
	Pivoter pivoter;
	Mesh mesh;
	// MeshFilter meshFilter;
	float startTime;
	// public Text text;
	bool running;
	int pivotsPerUpdate = 1;
	int pivotAnimationSteps = 5;
	bool pivotingInAction = false;
	int currentPivotStepNum = 0;
	public GameObject ball;
	public bool debugPivot = false;
	public bool drawNormals = false;

	public BallPivotingAlgorithm() {
		preMesh = new List<Triangle>();
		mesh = new Mesh();
		running = false;
	}

	// private void OnDrawGizmos() {
	// 	if (cloud != null) {
	// 		if (drawNormals) {
	// 			for (int i = 0; i < cloud.Count; i++) {
	// 				Gizmos.color = Color.white;
	// 				Gizmos.DrawLine(cloud[i].AsVector3(), cloud[i].AsVector3() + cloud[i].NormalAsVector3(0.2f));
	// 			}
	// 		}
	// 		if (debugPivot && activePivotEdge != null) {
	// 			Gizmos.color = Color.red;
	// 			Gizmos.DrawLine(activePivotEdge.First.Item1.AsVector3(), activePivotEdge.Second.Item1.AsVector3());
	// 			if (pivoter.stepInitialized) {
	// 				if (pivoter.stepIndicesCurrentIndex < pivoter.stepIndices.Count) {
	// 					Gizmos.DrawSphere(cloud[pivoter.stepIndices[pivoter.stepIndicesCurrentIndex]].AsVector3(), ballRadius / 10);
	// 					Gizmos.color = Color.yellow;
	// 					int scaleMod = 1;
	// 					for (int i = pivoter.stepIndicesCurrentIndex + 1; i < pivoter.stepIndices.Count; i++) {
	// 						Gizmos.DrawSphere(cloud[pivoter.stepIndices[i]].AsVector3(), ballRadius / (10 + scaleMod * 2));
	// 						scaleMod++;
	// 					}
	// 				}
	// 			}
	// 			Gizmos.color = Color.green;
	// 			LinkedListNode<Edge> pos = f.front.First;
	// 			while(pos.Next != null) {
	// 				Gizmos.DrawLine(pos.Value.First.Item1.AsVector3(), pos.Value.Second.Item1.AsVector3());
	// 				pos = pos.Next;
	// 			}
	// 		}
	// 	}
	// }

	// private void Update() {
	// 	if (running) {
	// 		if (debugPivot) {
	// 			if (SubStepBallPivot()) {
	// 				MakeStepMesh();
	// 			}
	// 		} else if (StepBallPivot()) {
	// 			MakeStepMesh();
	// 		}
	// 	}
	// }

	// public PointCloud<PointNormal> GetPointCloud() {
	// 	return cloud;
	// }

	// public Mesh GetMesh() {
	// 	return mesh;
	// }

	// public void RunInUpdate(bool fromShape, PointMeshType shape, int pivotsPerUpdate, int pivotAnimationSteps, int numPoints, float scale, float radius) {
	// 	this.pivotsPerUpdate = pivotsPerUpdate;
	// 	this.pivotAnimationSteps = pivotAnimationSteps;
	// 	running = true;
	// 	ballRadius = radius;
	// 	mesh = meshFilter.mesh;
	// 	ball.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
	// 	//generate a sphere of points for testing purposes

	// 	if (fromShape) {
	// 		cloud = new PointCloud<PointNormal>(numPoints);

	// 		if (shape == PointMeshType.Sphere) {
	// 			for (int i = 0; i < numPoints; i++) {
	// 				Vector3 normal = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f).normalized;
	// 				Vector3 point = normal * scale;
	// 				cloud.Add(new PointNormal(point.x, point.y, point.z, normal.x, normal.y, normal.z));
	// 			}
	// 		}
	// 	} else {
	// 		cloud = new PointCloud<PointNormal>(mesh.vertexCount);
	// 		var vertices = mesh.vertices;
	// 		var normals = mesh.normals;
	// 		for (int i = 0; i < mesh.vertexCount; i++) {
	// 			Vector3 v = vertices[i];
	// 			Vector3 n = normals[i];
	// 			cloud.Add(new PointNormal(v.x, v.y, v.z, n.x, n.y, n.z));
	// 		}
	// 	}

	// 	// GetComponent<VoxelRenderer>().SetFromPointCloud(cloud);
	// 	pivoter = new Pivoter(cloud, ballRadius);
	// 	f = new Front();
	// }

    public Mesh Run(PointCloud<PointNormal> inCloud, float[] passes)
    {
        startTime = Time.realtimeSinceStartup;
        cloud = inCloud;
        // if (fromShape)
        // {
        //     // switch (type)
        //     // {
        //     //     case PointMeshType.Sphere:
        //     //         {
        //     //             //generate a sphere of points for testing purposes
        //     //             cloud = new PointCloud<PointNormal>(numPoints);


        //     //             for (int i = 0; i < numPoints; i++)
        //     //             {
        //     //                 var normal = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f).normalized;
        //     //                 var point = normal * scale;
        //     //                 cloud.Add(new PointNormal(point.x, point.y, point.z, normal.x, normal.y, normal.z));
        //     //             }
        //     //             break;
        //     //         }
        //     //     default:
        //     //         return new Mesh();

        //     // }
        // }
        // else
        // {
        //     mesh = meshFilter.mesh;
        //     cloud = new PointCloud<PointNormal>(mesh.vertexCount);
        //     var vertices = mesh.vertices;
        //     var normals = mesh.normals;
        //     for (int i = 0; i < mesh.vertexCount; i++)
        //     {
        //         Vector3 v = vertices[i];
        //         Vector3 n = normals[i];
        //         cloud.Add(new PointNormal(v.x, v.y, v.z, n.x, n.y, n.z));
        //     }
        // }


        float initTime = (Time.realtimeSinceStartup - startTime);
        Debug.Log("Point Cloud loaded in: " + initTime + "s");
        startTime = Time.realtimeSinceStartup;

        // GetComponent<VoxelRenderer>().SetFromPointCloud(cloud);

        float voxTime = (Time.realtimeSinceStartup - startTime);
        Debug.Log("Renderer initialized in: " + voxTime + "s");
        startTime = Time.realtimeSinceStartup;

        RunBallPivot(passes);
        float triangTime = (Time.realtimeSinceStartup - startTime);
        Debug.Log("Triangulation completed in: " + triangTime + "s");
        Debug.Log("Total Searches: " + pivoter.totalSearches);

        startTime = Time.realtimeSinceStartup;
        MakeMesh();
        float meshTime = Time.realtimeSinceStartup - startTime;
        Debug.Log("Mesh created in: " + meshTime + "s");
        Debug.Log("Tris:" + preMesh.Count);

        // text.text = "Point Cloud initialized in: " + initTime + "s\n" +
        //             "Triangulation completed in: " + triangTime + "s\n" +
        //             "Mesh created in: " + meshTime + "s";

        return mesh;
	}

	// Triangle toAdd;
	// Vector3 oldPos;
	// Vector3 newPos;
	// float pivotedAngle;
	// Edge pivotEdge;

	// bool StepBallPivot() {
	// 	bool updated = false;
	// 	Edge e;
	// 	int i;


	// 	if (pivotingInAction) {
	// 		if (currentPivotStepNum < pivotAnimationSteps) {
	// 			currentPivotStepNum++;
	// 			if (currentPivotStepNum == pivotAnimationSteps) {
	// 				pivotingInAction = false;
	// 				currentPivotStepNum = 0;
	// 				preMesh.Add(toAdd);
	// 				updated = true;
	// 				ball.transform.position = newPos;
	// 			} else {
	// 				//ball.transform.position = oldPos + (newPos - oldPos) * (1.0f / pivotAnimationSteps) * currentPivotStepNum;
	// 				ball.transform.RotateAround(pivotEdge.MiddlePoint, (pivotEdge.Second.Item1 - pivotEdge.MiddlePoint).normalized, Mathf.Rad2Deg * pivotedAngle);
	// 			}
	// 		}
	// 	} else {
	// 		for (i = 0; i < pivotsPerUpdate && (e = f.GetActiveEdge()) != null; i++) {
	// 			Tuple<int, Triangle> t = pivoter.Pivot(e);
	// 			if (t != null && (!pivoter.IsUsed(t.Item1) || f.InFront(t.Item1))) {
	// 				if (pivotsPerUpdate == 1) {
	// 					pivotingInAction = true;
	// 					toAdd = t.Item2;
	// 					oldPos = e.BallCenter;
	// 					newPos = t.Item2.BallCenter;
	// 					ball.transform.position = oldPos;
	// 					//pivotedAngle = (pivoter.PivotedAngle) * (1.0f / pivotAnimationSteps);
	// 					Vector3 a = (oldPos - e.MiddlePoint);
	// 					Vector3 b = newPos - e.MiddlePoint;
	// 					pivotedAngle = Mathf.Acos(a.Dot(b) / (a.magnitude * b.magnitude)) * (1.0f / pivotAnimationSteps);
	// 					pivotEdge = e;
	// 				} else {
	// 					ball.transform.position = t.Item2.BallCenter;
	// 					if ((t.Item2.First.Item1 - t.Item2.Second.Item1).magnitude > 2 || (t.Item2.First.Item1 - t.Item2.Third.Item1).magnitude > 2 || (t.Item2.Second.Item1 - t.Item2.Third.Item1).magnitude > 2) {
	// 						int asdf = 0;
	// 					}
	// 					preMesh.Add(t.Item2);
	// 					updated = true;
	// 				}
	// 				f.JoinAndGlue(t, pivoter);
	// 			} else {
	// 				f.SetInactive(e);
	// 			}
	// 		}

	// 		if (i == 0) {
	// 			Triangle tri;
	// 			if ((tri = pivoter.FindSeed()) != null) {
	// 				preMesh.Add(tri);
	// 				updated = true;
	// 				ball.transform.position = tri.BallCenter;
	// 				f.AddEdges(tri);
	// 			} else {
	// 				running = false;
	// 			}
	// 		}
	// 	}
	// 	return updated;
	// }

	// Edge activePivotEdge;
	// bool activeEdgeQueried = false;
	// bool SubStepBallPivot() {
	// 	bool updated = false;

	// 	if (!activeEdgeQueried) {
	// 		activePivotEdge = f.GetActiveEdge();
	// 		activeEdgeQueried = true;
	// 	}
	// 	if (activePivotEdge != null) {
	// 		bool pivotRes = pivoter.StepPivot(activePivotEdge);
	// 		Tuple<int, Triangle> t = null;
	// 		if (pivotRes) {
	// 			t = pivoter.stepOutput;
	// 			bool addToMesh = false;
	// 			if (t != null) addToMesh = true;
	// 			if (addToMesh && !pivoter.IsUsed(t.Item1)) addToMesh = true;
	// 			if (addToMesh && f.InFront(t.Item1)) addToMesh = true;
	// 			//bool otherAddToMesh = t != null && (!pivoter.IsUsed(t.Item1) || f.InFront(t.Item1));
	// 			if (addToMesh/*t != null && (!pivoter.IsUsed(t.Item1) || f.InFront(t.Item1))*/) {
	// 				ball.transform.position = t.Item2.BallCenter;
	// 				preMesh.Add(t.Item2);
	// 				updated = true;
	// 				f.JoinAndGlue(t, pivoter);
	// 				activeEdgeQueried = false;
	// 			} else {
	// 				f.SetInactive(activePivotEdge);
	// 				activeEdgeQueried = false;
	// 			}
	// 		}
	// 	}

	// 	//only attempt findSeed if an active edge was searched but not found
	// 	if (activePivotEdge == null && activeEdgeQueried) {
	// 		Triangle tri;
	// 		if ((tri = pivoter.FindSeed()) != null) {
	// 			preMesh.Add(tri);
	// 			updated = true;
	// 			ball.transform.position = tri.BallCenter;
	// 			f.AddEdges(tri);
	// 		} else {
	// 			running = false;
	// 		}
	// 	}
	// 	return updated;
	// }

	void RunBallPivot(float[] passes) {

		startTime = Time.realtimeSinceStartup;
		f = new Front();

		ballRadius = passes[0];
		pivoter = new Pivoter(cloud, ballRadius);
		Debug.Log("Pivoter initialized in: " + (Time.realtimeSinceStartup - startTime) + "s");

		while (true) {
			Edge e;
			while ((e = f.GetActiveEdge()) != null) {
				Tuple<int, Triangle> t = pivoter.Pivot(e);
				if (t != null && (!pivoter.IsUsed(t.Item1) || f.InFront(t.Item1))) {
					preMesh.Add(t.Item2);
					f.JoinAndGlue(t, pivoter);
				} else {
					f.SetInactive(e);
				}
			}

			Triangle tri;
			if ((tri = pivoter.FindSeed()) != null) {
				preMesh.Add(tri);
				f.AddEdges(tri);
			} else {
				pivoter.FindSeed();
				break;
			}
		}
	}

	void MakeMesh() {
		// if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
		if (mesh == null) mesh = new Mesh();

		Vector3[] vertices = new Vector3[cloud.Count];
		int[] tris = new int[preMesh.Count * 3];

		for (int i = 0; i < vertices.Length; i++) {
			vertices[i] = cloud[i].AsVector3();
		}
		for (int i = 0; i < tris.Length - 2; i += 3) {
			tris[i] = preMesh[i / 3].First.Item2;
			tris[i + 1] = preMesh[i / 3].Second.Item2;
			tris[i + 2] = preMesh[i / 3].Third.Item2;
		}

		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = tris;
		mesh.RecalculateNormals();

		// meshFilter.mesh = mesh;
	}

	// Vector3[] vertices;
	// List<int> tris;
	// //what are you doing step-mesh??
	// void MakeStepMesh() {
	// 	if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
	// 	if (mesh == null) mesh = new Mesh();
	// 	if (vertices == null) {
	// 		vertices = new Vector3[cloud.Count];
	// 		for (int i = 0; i < vertices.Length; i++) {
	// 			vertices[i] = cloud[i].AsVector3();
	// 		}
	// 	}
	// 	if (tris == null) tris = new List<int>();
	// 	for (int i = 0; i < (preMesh.Count * 3) - 2; i += 3) {
	// 		tris.Add(preMesh[i / 3].First.Item2);
	// 		tris.Add(preMesh[i / 3].Second.Item2);
	// 		tris.Add(preMesh[i / 3].Third.Item2);
	// 	}

	// 	mesh.Clear();
	// 	mesh.vertices = vertices;
	// 	mesh.triangles = tris.ToArray();
	// 	mesh.RecalculateNormals();

	// 	meshFilter.mesh = mesh;

	// 	preMesh.Clear();
	// }
}