using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

public class PRM : MonoBehaviour
{
	public static float[,] FREE_SAMPLE;
	public Camera orthogonal_camera;
	public Camera persepctive_camera;
	public Transform Goal;
	public Material completeMaterial;
	public Material sampleMaterial;
	public Material lineMaterial;
	public GameObject Sample;
	public int SAMPLE_COUNT = 5000;
	public float speed = 1f;
	public int K_NN = 3;

	private readonly float collision_check_radius = 0.01f;
	private Matrix POINTS;
	private List<int> shortest_path = new List<int>();
	private int current_step = 0;
	private bool isComplete = false;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		List<int> indexs = new List<int>();
		float[,] samples = new float[SAMPLE_COUNT, 6];              // x, y, z, φ, θ, ψ
		Vector3 initial_pos = this.transform.position;
		Vector3 initial_angle = this.transform.eulerAngles;
		Dictionary<int, List<(int, float)>> graph = new Dictionary<int, List<(int, float)>>();



		// 포인트 정의
		this.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
		float[,] points = new float[3, this.transform.GetChild(0).childCount];
		for (int i = 0; i < this.transform.GetChild(0).childCount; i++) {
			Vector3 child_pos = this.transform.GetChild(0).GetChild(i).position;
			points[0, i] = child_pos.x;
			points[1, i] = child_pos.y;
			points[2, i] = child_pos.z;
		}
		POINTS = new Matrix(points);
		this.transform.eulerAngles = initial_angle;



		// 샘플 뿌리기
		Debug.Log("샘플 뿌리기");
		for (int i = 0; i < SAMPLE_COUNT; i++)
		{
			float x = Random.Range(-1f, 5f);
			float y = Random.Range(0.5f, 4f);
			float z = Random.Range(-4f, 1f);
			float phi = Random.Range(0f, 360f);
			float theta = Random.Range(0f, 360f);
			float psi = Random.Range(0f, 360f);

			Quaternion quaternion = Quaternion.Euler(phi, theta, psi);

			samples[i, 0] = x;
			samples[i, 1] = y;
			samples[i, 2] = z;
			samples[i, 3] = quaternion.eulerAngles.x;
			samples[i, 4] = quaternion.eulerAngles.y;
			samples[i, 5] = quaternion.eulerAngles.z;

			GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			Destroy(obj.GetComponent<SphereCollider>());
			obj.name = obj.name + (i+2);
			obj.GetComponent<MeshRenderer>().material = sampleMaterial;
			obj.transform.parent = Sample.transform;
			obj.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
			obj.transform.position = new Vector3(x, y, z);
			obj.transform.rotation = quaternion;
		}



		// 충돌 찾기
		Debug.Log("충돌 찾기");
		for (int i = 0; i < samples.GetLength(0); i++)
		{
			this.transform.position = new Vector3(samples[i, 0], samples[i, 1], samples[i, 2]);
			this.transform.eulerAngles = new Vector3(samples[i, 3], samples[i, 4], samples[i, 5]);
			bool noCollision = true;
			for (int j = 0; j < this.transform.GetChild(0).childCount; j++)
			{
				Vector3 pos = this.transform.GetChild(0).GetChild(j).transform.position;
				Collider[] hitColliders = Physics.OverlapSphere(pos, collision_check_radius);
				for (int n = 0; n < hitColliders.GetLength(0); n++) {
					if (hitColliders[n].gameObject.CompareTag("Obstacle")) {
						noCollision = false;
						break;
					}
				}
			}

			if (noCollision) {
				indexs.Add(i);
			}
		}

		this.transform.position = initial_pos;
		this.transform.eulerAngles = initial_angle;
		FREE_SAMPLE = new float[indexs.Count + 2, 6];
		FREE_SAMPLE[0, 0] = initial_pos.x;
		FREE_SAMPLE[0, 1] = initial_pos.y;
		FREE_SAMPLE[0, 2] = initial_pos.z;
		FREE_SAMPLE[0, 3] = initial_angle.x;
		FREE_SAMPLE[0, 4] = initial_angle.y;
		FREE_SAMPLE[0, 5] = initial_angle.z;
		FREE_SAMPLE[1, 0] = Goal.position.x;
		FREE_SAMPLE[1, 1] = Goal.position.y;
		FREE_SAMPLE[1, 2] = Goal.position.z;
		FREE_SAMPLE[1, 3] = Goal.eulerAngles.x;
		FREE_SAMPLE[1, 4] = Goal.eulerAngles.y;
		FREE_SAMPLE[1, 5] = Goal.eulerAngles.z;



		// 샘플 거르기
		Debug.Log("샘플 거르기");
		for (int i = 2; i < FREE_SAMPLE.GetLength(0); i++)
		{
			for (int j = 0; j < FREE_SAMPLE.GetLength(1); j++) {
				FREE_SAMPLE[i, j] = samples[indexs[i - 2], j];
			}
		}



		// K-NN으로 그래프 구성
		Debug.Log("K-NN 그래프 구성");
		for (int i = 0; i < FREE_SAMPLE.GetLength(0); i++)
		{
			graph[i] = new List<(int, float)>();
		}
		for (int i = 0; i < FREE_SAMPLE.GetLength(0); i++)
		{
			List<(float, int)> distances = new List<(float, int)>();
			float[] vec1 = new float[] { FREE_SAMPLE[i, 0], FREE_SAMPLE[i, 1], FREE_SAMPLE[i, 2], Mathf.Cos(FREE_SAMPLE[i, 3] * Mathf.Deg2Rad), Mathf.Sin(FREE_SAMPLE[i, 3] * Mathf.Deg2Rad), Mathf.Cos(FREE_SAMPLE[i, 4] * Mathf.Deg2Rad), Mathf.Sin(FREE_SAMPLE[i, 4] * Mathf.Deg2Rad), Mathf.Cos(FREE_SAMPLE[i, 5] * Mathf.Deg2Rad), Mathf.Sin(FREE_SAMPLE[i, 5] * Mathf.Deg2Rad) };
			for (int j = 0; j < FREE_SAMPLE.GetLength(0); j++)
			{
				if (i == j) continue;
				float[] vec2 = new float[] { FREE_SAMPLE[j, 0], FREE_SAMPLE[j, 1], FREE_SAMPLE[j, 2], Mathf.Cos(FREE_SAMPLE[j, 3] * Mathf.Deg2Rad), Mathf.Sin(FREE_SAMPLE[j, 3] * Mathf.Deg2Rad), Mathf.Cos(FREE_SAMPLE[j, 4] * Mathf.Deg2Rad), Mathf.Sin(FREE_SAMPLE[j, 4] * Mathf.Deg2Rad), Mathf.Cos(FREE_SAMPLE[j, 5] * Mathf.Deg2Rad), Mathf.Sin(FREE_SAMPLE[j, 5] * Mathf.Deg2Rad) };
				float dist = Distance(vec1, vec2);
				distances.Add((dist, j));
			}
			distances.Sort((a, b) => a.Item1.CompareTo(b.Item1));

			for (int j = 0; j < Mathf.Min(K_NN, distances.Count); j++)
			{
				int neighborIdx = distances[j].Item2;
				bool alreadyHas = false;
				for (int n = 0; n < graph[i].Count; n++)
				{
					if (graph[i][n].Item1 == neighborIdx)
					{
						alreadyHas = true;
						break;
					}
				}
				if (alreadyHas) continue;
				float dist = distances[j].Item1;

				float[] vec2 = new float[] { FREE_SAMPLE[neighborIdx, 0], FREE_SAMPLE[neighborIdx, 1], FREE_SAMPLE[neighborIdx, 2], Mathf.Cos(FREE_SAMPLE[neighborIdx, 3] * Mathf.Deg2Rad), Mathf.Sin(FREE_SAMPLE[neighborIdx, 3] * Mathf.Deg2Rad), Mathf.Cos(FREE_SAMPLE[neighborIdx, 4] * Mathf.Deg2Rad), Mathf.Sin(FREE_SAMPLE[neighborIdx, 4] * Mathf.Deg2Rad), Mathf.Cos(FREE_SAMPLE[neighborIdx, 5] * Mathf.Deg2Rad), Mathf.Sin(FREE_SAMPLE[neighborIdx, 5] * Mathf.Deg2Rad) };

				// 보간 충돌 체크 (Local Planning)
				if (!IsCollision(vec1, vec2))
				{
					graph[i].Add((neighborIdx, dist));
					graph[neighborIdx].Add((i, dist));

					GameObject obj = new GameObject();
					obj.name = "line" + i + "_" + neighborIdx;
					LineRenderer line = obj.AddComponent<LineRenderer>();
					line.positionCount = 2;
					line.startWidth = 0.005f;
					line.endWidth = 0.005f;
					line.material = lineMaterial;
					line.startColor = new Color(255f/255f, 128f/255f, 128f/255f);
					line.endColor = new Color(255f/255f, 128f/255f, 128f/255f);
					line.transform.parent = Sample.transform;
					line.SetPosition(0, new Vector3(vec1[0], vec1[1], vec1[2]));
					line.SetPosition(1, new Vector3(vec2[0], vec2[1], vec2[2]));
				}
			}
		}



		// 다익스트라 경로
		Debug.Log("다익스트라 경로 찾기");
		float[] DijkstraDist = new float[FREE_SAMPLE.GetLength(0)];
		int[] DijkstraPath = new int[FREE_SAMPLE.GetLength(0)];
		DijkstraDist[0] = 0f;
		DijkstraPath[0] = -1;
		for (int i = 1; i < DijkstraDist.GetLength(0); i++)
		{
			DijkstraDist[i] = int.MaxValue;
			DijkstraPath[i] = -1;
		}

		PriorityQueue<int, float> queue = new PriorityQueue<int, float>();
		queue.Enqueue(0, DijkstraDist[0]);

		int current_node;
		float prev_dist;
		Dictionary<int, bool> visited = new Dictionary<int, bool>();
		List<(int, float)> neighbor_node;
		while (queue.Count > 0)
		{
			queue.TryDequeue(out current_node, out prev_dist);
			neighbor_node = graph[current_node];
			visited[current_node] = true;

			for (int i = 0; i < neighbor_node.Count; i++)
			{
				int next_node = neighbor_node[i].Item1;
				if (visited.ContainsKey(next_node)) continue;
				float next_dist = neighbor_node[i].Item2;
				if (prev_dist + next_dist < DijkstraDist[next_node])
				{
					DijkstraDist[next_node] = prev_dist + next_node;
					DijkstraPath[next_node] = current_node;
					queue.Enqueue(next_node, DijkstraDist[next_node]);
				}
			}
		}

		int idx = 1;
		if (DijkstraPath[idx] == -1) {
			isComplete = true;
			Debug.Log("경로 없음 - 종료");
			return;
		}

		shortest_path.Add(idx);
		while (idx > 0)
		{
			GameObject lineObj = new GameObject();
			lineObj.name = "Path" + idx;
			LineRenderer line = lineObj.AddComponent<LineRenderer>();
			line.positionCount = 2;
			line.startWidth = 0.01f;
			line.endWidth = 0.01f;
			line.material = lineMaterial;
			line.startColor = new Color(0f, 0f, 1f);
			line.endColor = new Color(0f, 0f, 1f);
			line.transform.parent = Sample.transform;
			line.SetPosition(0, new Vector3(FREE_SAMPLE[DijkstraPath[idx], 0], FREE_SAMPLE[DijkstraPath[idx], 1], FREE_SAMPLE[DijkstraPath[idx], 2]));
			line.SetPosition(1, new Vector3(FREE_SAMPLE[idx, 0], FREE_SAMPLE[idx, 1], FREE_SAMPLE[idx, 2]));

			idx = DijkstraPath[idx];
			shortest_path.Add(idx);
		}
		shortest_path.Reverse();
	}

	private bool IsCollision(float[] v1, float[] v2) {
		int steps = (int)(1f / Time.fixedDeltaTime);
		Vector3 vec1 = new Vector3(v1[0], v1[1], v1[2]);
		Vector3 vec2 = new Vector3(v2[0], v2[1], v2[2]);
		Vector2 ang_x1 = new Vector2(v1[3], v1[4]);
		Vector2 ang_x2 = new Vector2(v2[3], v2[4]);
		Vector2 ang_y1 = new Vector2(v1[5], v1[6]);
		Vector2 ang_y2 = new Vector2(v2[5], v2[6]);
		Vector2 ang_z1 = new Vector2(v1[7], v1[8]);
		Vector2 ang_z2 = new Vector2(v2[7], v2[8]);

		for (int i = 1; i <= steps; i++) {
			float t = i / ((float)steps);
			Vector3 vec_interp = Vector3.Lerp(vec1, vec2, t);
			Vector2 ang_x_interp = Vector2.Lerp(ang_x1, ang_x2, t).normalized;
			Vector2 ang_y_interp = Vector2.Lerp(ang_y1, ang_y2, t).normalized;
			Vector2 ang_z_interp = Vector2.Lerp(ang_z1, ang_z2, t).normalized;

			float rad_x = Mathf.Atan2(ang_x_interp.y, ang_x_interp.x);
			float rad_y = Mathf.Atan2(ang_y_interp.y, ang_y_interp.x);
			float rad_z = Mathf.Atan2(ang_z_interp.y, ang_z_interp.x);

			// 회전행렬
			float[,] r_x = new float[3, 3] { { 1f, 0f, 0f }, { 0f, Mathf.Cos(rad_x), -Mathf.Sin(rad_x) }, { 0f, Mathf.Sin(rad_x), Mathf.Cos(rad_x) } };
			Matrix rotate_x = new Matrix(r_x);

			float[,] r_y = new float[3, 3] { { Mathf.Cos(rad_y), 0f, Mathf.Sin(rad_y) }, { 0f, 1f, 0f }, { -Mathf.Sin(rad_y), 0f, Mathf.Cos(rad_y) } };
			Matrix rotate_y = new Matrix(r_y);

			float[,] r_z = new float[3, 3] { { Mathf.Cos(rad_z), -Mathf.Sin(rad_z), 0f }, { Mathf.Sin(rad_z), Mathf.Cos(rad_z), 0f }, { 0f, 0f, 1f } };
			Matrix rotate_z = new Matrix(r_z);

			// 오프셋
			Matrix offset = rotate_y.Multiply(rotate_x).Multiply(rotate_z).Multiply(POINTS);

			for (int k = 0; k < POINTS.matrix.GetLength(1); k++) {
				Vector3 rotated_vec_interp = new Vector3(vec_interp.x + offset.matrix[0, k], vec_interp.y + offset.matrix[1, k], vec_interp.z + offset.matrix[2, k]);
				Collider[] hitColliders = Physics.OverlapSphere(rotated_vec_interp, collision_check_radius);
				for (int j = 0; j < hitColliders.GetLength(0); j++)
				{
					if (hitColliders[j].gameObject.CompareTag("Obstacle")) return true;
				}
			}
		}
		return false;
	}

	private float Distance(float[] vec1, float[] vec2) {
		if (vec1.GetLength(0) != vec2.GetLength(0)) {
			throw new UnityException("두 벡터의 차원이 맞지 않습니다.");
		}

		float sum = 0f;
		for (int i = 0; i < vec1.GetLength(0); i++) {
			sum += Mathf.Pow((vec1[i] - vec2[i]), 2);
		}

		return Mathf.Sqrt(sum);
	}

	// Update is called once per frame
	void Update() {
		orthogonal_camera.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + 1.711263f, this.transform.position.z);
		persepctive_camera.transform.position = new Vector3(this.transform.position.x - 0.75f, this.transform.position.y + 0.75f, this.transform.position.z + 0.75f);
	}

	private void FixedUpdate()
	{
		if (isComplete) return;

		if (current_step > shortest_path.Count - 1 || shortest_path[current_step] < 0f) {
			isComplete = true;
			Debug.Log("종료");
		}

		if (isComplete)
		{
			//for (int i = 0; i < this.transform.GetChild(0).childCount; i++)
			//{
			//	this.transform.GetChild(0).GetChild(i).GetComponent<MeshRenderer>().material = completeMaterial;
			//}
			return;
		}

		int current_node = shortest_path[current_step];

		Vector3 pos = new Vector3(FREE_SAMPLE[current_node, 0], FREE_SAMPLE[current_node, 1], FREE_SAMPLE[current_node, 2]);
		Quaternion ang = Quaternion.Euler(new Vector3(FREE_SAMPLE[current_node, 3], FREE_SAMPLE[current_node, 4], FREE_SAMPLE[current_node, 5]));

		Vector3 dir_vec = pos - this.transform.position;
		Quaternion dir_ang = Quaternion.Slerp(this.transform.rotation, ang, speed * Time.fixedDeltaTime);
		if (dir_vec.magnitude > 0.0075f) {
			this.transform.position += dir_vec * speed * Time.fixedDeltaTime;
			this.transform.rotation = dir_ang;
		} else {
			this.transform.rotation = ang;
			current_step++;
		}
	}
}
