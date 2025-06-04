using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using renge_pcl;
using renge_pcl.octree;
using UnityEngine.UI;



class Pivoter {
	//KDTree<PointNormal> kdtree;
	//OcTree<PointNormal> octree;
	VoxelGrid<PointNormal> vgrid;
	PointCloud<PointNormal> cloud;
	float ballRadius;
	Dictionary<int, bool> notUsed;
	public int totalSearches;

	public Pivoter(PointCloud<PointNormal> cloud, float ballRadius) {
		this.cloud = cloud;
		totalSearches = 0;
		//kdtree = new KDTree<PointNormal>();
		//kdtree.SetInputCloud(cloud);
		//octree = new OcTree<PointNormal>();
		//octree.SetInputCloud(cloud, 80);
		SetBallRadius(ballRadius);
		notUsed = new Dictionary<int, bool>();

		for (int i = 0; i < cloud.Count; i++) {
			notUsed[i] = false;
		}
	}

	public void SetBallRadius(float radius) {
		if (radius == 0 || cloud == null) return;
		this.ballRadius = radius;
		vgrid = new VoxelGrid<PointNormal>(cloud, ballRadius);
	}

	public bool stepInitialized = false;
	public Vector3 stepNormal;
	public HyperPlane stepPlane;
	public Vector3 stepZeroAngle;
	public float stepCurrentAngle;
	public Tuple<int, Triangle> stepOutput;
	public Vector3 stepVij;
	public List<int> stepIndices;
	public int stepIndicesCurrentIndex;

	/// <returns>true if done, false if not done</returns>
	public bool StepPivot(Edge e) {
		if (!stepInitialized) {
			stepInitialized = true;
			stepNormal = (e.First.Item1 - e.MiddlePoint).normalized;
			stepPlane = new HyperPlane(stepNormal, e.MiddlePoint);
			stepZeroAngle = e.OppositeVertex.Item1 - e.MiddlePoint;
			stepZeroAngle = stepPlane.Projection(stepZeroAngle);
			stepZeroAngle.Normalize();

			stepCurrentAngle = Mathf.PI;
			stepOutput = null;

			stepVij = e.Second.Item1 - e.First.Item1;
			stepIndices = GetNeighbors(e.MiddlePoint, ballRadius * 2);

			stepIndicesCurrentIndex = 0;
		}
		if (stepIndicesCurrentIndex >= stepIndices.Count) {
			stepInitialized = false;
			return true;
		}

		int index = stepIndices[stepIndicesCurrentIndex];
		if (e.First.Item2 == index || e.Second.Item2 == index || e.OppositeVertex.Item2 == index) {
			stepIndicesCurrentIndex++;
			return false;
		}
		PointNormal point = cloud[index];

		if (stepPlane.AbsDistance(point) <= ballRadius) {
			Vector3 center;
			if (GetBallCenter(e.First.Item2, e.Second.Item2, index, out center, out _)) {

				Vector3 Vik = point - e.First.Item1;
				Vector3 faceNormal = Vector3.Cross(Vik, stepVij).normalized;

				if (!IsOriented(faceNormal, e.First.Item1, e.Second.Item1, cloud[index])) {
					stepIndicesCurrentIndex++;
					return false;
				}


				List<int> neighborhood = GetNeighbors(center, ballRadius);
				if (!isEmpty(neighborhood, e.First.Item2, e.Second.Item2, index, center)) {
					stepIndicesCurrentIndex++;
					return false;
				}


				float cosAngle = stepZeroAngle.Dot(stepPlane.Projection(center).normalized);
				if (Mathf.Abs(cosAngle) > 1.0f) {
					cosAngle = Mathf.Sign(cosAngle);
				}

				float angle = Mathf.Acos(cosAngle);

				if (stepOutput == null || stepCurrentAngle > angle) {
					stepCurrentAngle = angle;
					stepOutput = new Tuple<int, Triangle>(index, new Triangle(e.First.Item1, point, e.Second.Item1, e.First.Item2, index, e.Second.Item2, center, ballRadius));
				}
			}
		}
		stepIndicesCurrentIndex++;
		return false;
	}

	internal Tuple<int, Triangle> Pivot(Edge e) {
		Tuple<PointNormal, int> v0 = e.First;
		Tuple<PointNormal, int> v1 = e.Second;
		Tuple<PointNormal, int> op = e.OppositeVertex;

		//Vector3 diff1 = 100 * (v0.Item1.AsVector3() - middle);
		//Vector3 diff2 = 100 * (e.BallCenter.AsVector3() - middle);

		//Vector3 y = Vector3.Cross(diff1, diff2).normalized;
		//Vector3 normal = Vector3.Cross(diff2, y).normalized;
		Vector3 normal = (v0.Item1 - e.MiddlePoint).normalized;
		HyperPlane plane = new HyperPlane(normal, e.MiddlePoint);

		Vector3 zeroAngle = op.Item1 - e.MiddlePoint;
		zeroAngle = plane.Projection(zeroAngle);
		zeroAngle.Normalize();

		float currentAngle = Mathf.PI;
		Tuple<int, Triangle> output = null;

		Vector3 Vij = v1.Item1 - v0.Item1;

		int[] indices = GetNeighbors(e.MiddlePoint, ballRadius * 2).ToArray();
		for (int t = 0; t < indices.Length; t++) {
			int index = indices[t];
			if (v0.Item2 == index || v1.Item2 == index || op.Item2 == index)
				continue;

			PointNormal point = cloud[index];
			if (plane.AbsDistance(point) <= ballRadius) {
				Vector3 center;
				if (GetBallCenter(v0.Item2, v1.Item2, index, out center, out _)) {

					Vector3 Vik = point - v0.Item1;
					Vector3 faceNormal = Vector3.Cross(Vik, Vij).normalized;

					if (!IsOriented(faceNormal, v0.Item1, v1.Item1, cloud[index]))
						continue;

					List<int> neighborhood = GetNeighbors(center, ballRadius);
					if (!isEmpty(neighborhood, v0.Item2, v1.Item2, index, center))
						continue;


					float cosAngle = zeroAngle.Dot(plane.Projection(center).normalized);
					if (Mathf.Abs(cosAngle) > 1.0f) {
						cosAngle = Mathf.Sign(cosAngle);
					}

					float angle = Mathf.Acos(cosAngle);

					if (output == null || currentAngle > angle) {
						currentAngle = angle;
						output = new Tuple<int, Triangle>(index, new Triangle(v0.Item1, cloud[index], v1.Item1, v0.Item2, index, v1.Item2, center, ballRadius));
						//return output;
					}
				}
			}
		}

		return output;
	}

	internal Triangle FindSeed() {
		float neighborhoodSize = 1.3f;
		Triangle seed = null;
		bool found = false;
		Dictionary<ulong, bool> tested = new Dictionary<ulong, bool>();
		List<int> removeIndices = new List<int>();

		foreach (KeyValuePair<int, bool> pair in notUsed)
		{
			if (found) break;

			int index0 = pair.Key;
			if (removeIndices.Contains(index0)) continue;

			int[] indices = GetNeighbors(cloud[index0].AsVector3(), ballRadius * neighborhoodSize).ToArray();
			if (indices.Length < 3)
				continue;

			for (int j = 0; j < indices.Length && !found; j++)
			{
				int index1 = indices[j];

				if (index1 == index0 || !notUsed.ContainsKey(index1) || removeIndices.Contains(index1))
					continue;

				for (int k = j + 1; k < indices.Length && !found; k++)
				{
					int index2 = indices[k];

					if (index1 == index2 || index2 == index0 || !notUsed.ContainsKey(index2) || removeIndices.Contains(index2))
						continue;

					List<int> trio = new List<int>();
					trio.Add(index0);
					trio.Add(index1);
					trio.Add(index2);
					//trio.Sort();
					ulong code = Convert.ToUInt64(trio[0]) + Convert.ToUInt64(1e6 * trio[1]) + Convert.ToUInt64(1e12 * trio[2]);

					bool toContinue = false;

					if (tested.ContainsKey(code)) toContinue = true;
					else tested[code] = true;

					if (toContinue) continue;

					Vector3 center;
					Tuple<PointNormal, int>[] sequence;

					if (!found && GetBallCenter(index0, index1, index2, out center, out sequence))
					{
						List<int> neighborhood = GetNeighbors(center, ballRadius);

						if (!found && isEmpty(neighborhood, index0, index1, index2, center))
						{

							seed = new Triangle(sequence[0], sequence[1], sequence[2], center, ballRadius);

							removeIndices.Add(index0);
							removeIndices.Add(index1);
							removeIndices.Add(index2);

							found = true;
						}
					}
				}
			}
		}
		foreach (var index in removeIndices) {
			notUsed.Remove(index);
		}

		return seed;
	}

	internal PointNormal GetPoint(int index) {
		return cloud[index];
	}

	internal bool IsUsed(int index) {
		return !notUsed.ContainsKey(index);
	}

	internal void SetUsed(int index) {
		notUsed.Remove(index);
	}

	Tuple<Vector3, float> GetCircumscribedCircle(PointNormal p0, PointNormal p1, PointNormal p2) {
		Vector3 d10 = p1 - p0;
		Vector3 d20 = p2 - p0;
		Vector3 d01 = p0 - p1;
		Vector3 d12 = p1 - p2;
		Vector3 d21 = p2 - p1;
		Vector3 d02 = p0 - p2;

		float mag01 = d01.magnitude;
		float mag12 = d12.magnitude;
		float mag02 = d02.magnitude;

		float mag01C12 = Vector3.Cross(d01, d12).magnitude;

		float alpha = (mag12 * mag12 * d01.Dot(d02)) / (2 * mag01C12 * mag01C12);
		float beta = (mag02 * mag02 * d10.Dot(d12)) / (2 * mag01C12 * mag01C12);
		float gamma = (mag01 * mag01 * d20.Dot(d21)) / (2 * mag01C12 * mag01C12);

		Vector3 circumscribedCircleCenter = alpha * p0 + beta * p1 + gamma * p2;
		float circumscribedCircleRadius = (mag01 * mag12 * mag02) / (2 * mag01C12);

		return new Tuple<Vector3, float>(circumscribedCircleCenter, circumscribedCircleRadius);
	}

	bool GetBallCenter(int index0, int index1, int index2, out Vector3 center, out Tuple<PointNormal, int>[] sequence) {
		bool status = false;
		center = new Vector3();

		PointNormal p0 = cloud[index0];
		PointNormal p1 = cloud[index1];
		PointNormal p2 = cloud[index2];
		sequence = new Tuple<PointNormal, int>[3];
		sequence[0] = new Tuple<PointNormal, int>(p0, index0);
		sequence[1] = new Tuple<PointNormal, int>(p1, index1);
		sequence[2] = new Tuple<PointNormal, int>(p2, index2);

		Vector3 v10 = p1 - p0;
		Vector3 v20 = p2 - p0;
		Vector3 normal = Vector3.Cross(v10, v20);

		if (normal.magnitude > 0.0000000001) {
			normal.Normalize();
			if (!IsOriented(normal, p0, p1, p2)) {
				p0 = cloud[index1];
				p1 = cloud[index0];
				//sequence = new Vector3Int(index1, index0, index2);
				Tuple<PointNormal, int> tmp = sequence[0];
				sequence[0] = sequence[1];
				sequence[1] = tmp;

				v10 = p1 - p0;
				v20 = p2 - p0;
				//v10 = sequence[1].Item1 - sequence[0].Item1;
				//v20 = sequence[2].Item1 - sequence[0].Item1;
				normal = Vector3.Cross(v10, v20).normalized;
			}

			Tuple<Vector3, float> circle = GetCircumscribedCircle(p0, p1, p2);
			//Tuple<Vector3, float> circle = GetCircumscribedCircle(sequence[0].Item1, sequence[1].Item1, sequence[2].Item1);
			float squaredDistance = ballRadius * ballRadius - circle.Item2 * circle.Item2;
			if (squaredDistance > 0) {
				float distance = Mathf.Sqrt(Mathf.Abs(squaredDistance));
				center = circle.Item1 + distance * normal;
				status = true;
			}
		}

		return status;
	}


	bool IsOriented(Vector3 normal, PointNormal normal0, PointNormal normal1, PointNormal normal2) {
		int count = 0;
		count = normal.NormalDot(normal0) < 0 ? count + 1 : count;
		count = normal.NormalDot(normal1) < 0 ? count + 1 : count;
		count = normal.NormalDot(normal2) < 0 ? count + 1 : count;
		return count <= 1;
	}

	bool isEmpty(List<int> data, int index0, int index1, int index2, in Vector3 ballCenter) {
		if (data == null || data.Count <= 0)
			return true;

		for (int i = 0; i < data.Count; i++) {
			if (data[i] == index0 || data[i] == index1 || data[i] == index2)
				continue;
			Vector3 dist = cloud[data[i]] - ballCenter;
			if (Mathf.Abs(dist.magnitude - ballRadius) < 0.0000001)
				continue;

			return false;
		}

		return true;
	}

	List<int> GetNeighbors(Vector3 point, float radius) {
		List<int> indices;
		//kdtree.RadiusSearch(point, radius, out indices, out _);
		totalSearches++;
		//octree.RadiusSearch(point, radius, out indices);
		vgrid.RadiusSearch(point, radius, out indices);
		return indices;
	}
}