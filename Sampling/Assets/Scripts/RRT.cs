using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Color = UnityEngine.Color;

public class RRT : MonoBehaviour
{
	public Camera orthogonal_camera;
	public Camera persepctive_camera;
	public Transform Goal;
	public Material completeMaterial;
	public Material sampleMaterial;
	public Material lineMaterial;
	public GameObject Sample;
	public int iteration = 1000;
	public float speed = 1f;
	public float step = 0.01f;

	private readonly Dictionary<int, int> tree = new Dictionary<int, int>();
	private readonly float[] q_goal_coor = new float[9];
	private readonly List<float[]> FREE_SAMPLE = new List<float[]>();
	private readonly float collision_check_radius = 0.01f;
	private readonly List<int> shortest_path = new List<int>();
	private Matrix POINTS;
	private int current_step = 0;
	private bool isComplete = false;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		bool hasPath = false;

		Vector3 initial_pos = this.transform.position;
		Vector3 initial_angle = this.transform.eulerAngles;
		Dictionary<int, List<(int, float)>> graph = new Dictionary<int, List<(int, float)>>();
		q_goal_coor[0] = Goal.position.x;
		q_goal_coor[1] = Goal.position.y;
		q_goal_coor[2] = Goal.position.z;
		q_goal_coor[3] = Mathf.Cos(Goal.eulerAngles.x * Mathf.Deg2Rad);
		q_goal_coor[4] = Mathf.Sin(Goal.eulerAngles.x * Mathf.Deg2Rad);
		q_goal_coor[5] = Mathf.Cos(Goal.eulerAngles.y * Mathf.Deg2Rad);
		q_goal_coor[6] = Mathf.Sin(Goal.eulerAngles.y * Mathf.Deg2Rad);
		q_goal_coor[7] = Mathf.Cos(Goal.eulerAngles.z * Mathf.Deg2Rad);
		q_goal_coor[8] = Mathf.Sin(Goal.eulerAngles.z * Mathf.Deg2Rad);



		// 포인트 정의
		this.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
		float[,] points = new float[3, this.transform.GetChild(0).childCount];
		for (int i = 0; i < this.transform.GetChild(0).childCount; i++)
		{
			Vector3 child_pos = this.transform.GetChild(0).GetChild(i).position;
			points[0, i] = child_pos.x;
			points[1, i] = child_pos.y;
			points[2, i] = child_pos.z;
		}
		POINTS = new Matrix(points);
		this.transform.eulerAngles = initial_angle;

		FREE_SAMPLE.Add(new float[6] { initial_pos.x, initial_pos.y, initial_pos.z, initial_angle.x, initial_angle.y, initial_angle.z });

		// RRT
		for (int i = 1; i <= iteration; i++)
		{
			// 랜덤 샘플 뿌리기
			float x = Random.Range(-1f, 5f);
			float y = Random.Range(0f, 4f);
			float z = Random.Range(-4f, 1f);
			float phi = Random.Range(0f, 360f);
			float theta = Random.Range(0f, 360f);
			float psi = Random.Range(0f, 360f);

			Quaternion quat = Quaternion.Euler(phi, theta, psi);
			float[] q_rand_ang = new float[6] { x, y, z, quat.eulerAngles.x, quat.eulerAngles.y, quat.eulerAngles.z };
			float[] q_rand_coor = Angle2Coor(q_rand_ang);

			float minimumDist = float.MaxValue;
			int near_idx = -1;

			// 최근접 노드 찾기
			for (int k = 0; k < FREE_SAMPLE.Count; k++)
			{
				float dist = Distance(Angle2Coor(FREE_SAMPLE[k]), q_rand_coor);
				if (dist < minimumDist) {
					minimumDist = dist;
					near_idx = k;
				}
			}
			float[] q_nearest_coor = Angle2Coor(FREE_SAMPLE[near_idx]);

			// step만큼 축소
			Vector3 vec1 = new Vector3(q_rand_coor[0], q_rand_coor[1], q_rand_coor[2]);
			Vector3 vec2 = new Vector3(q_nearest_coor[0], q_nearest_coor[1], q_nearest_coor[2]);
			Vector2 ang_x1 = new Vector2(q_rand_coor[3], q_rand_coor[4]);
			Vector2 ang_x2 = new Vector2(q_nearest_coor[3], q_nearest_coor[4]);
			Vector2 ang_y1 = new Vector2(q_rand_coor[5], q_rand_coor[6]);
			Vector2 ang_y2 = new Vector2(q_nearest_coor[5], q_nearest_coor[6]);
			Vector2 ang_z1 = new Vector2(q_rand_coor[7], q_rand_coor[8]);
			Vector2 ang_z2 = new Vector2(q_nearest_coor[7], q_nearest_coor[8]);

			Vector3 vec_interp = Vector3.Lerp(vec1, vec2, step);
			Vector2 ang_x_interp = Vector2.Lerp(ang_x1, ang_x2, step).normalized;
			Vector2 ang_y_interp = Vector2.Lerp(ang_y1, ang_y2, step).normalized;
			Vector2 ang_z_interp = Vector2.Lerp(ang_z1, ang_z2, step).normalized;

			float[] q_new_coor = new float[9] { vec_interp.x, vec_interp.y, vec_interp.z, ang_x_interp.x, ang_x_interp.y, ang_y_interp.x, ang_y_interp.y, ang_z_interp.x, ang_z_interp.y };
			float[] q_new_ang = Coor2Angle(q_new_coor);

			if (!IsCollision(q_nearest_coor, q_new_coor))
			{
				FREE_SAMPLE.Add(q_new_ang);
				tree.Add(FREE_SAMPLE.Count - 1, near_idx);

				GameObject pointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				Destroy(pointObj.GetComponent<SphereCollider>());
				pointObj.name = pointObj.name + i;
				pointObj.GetComponent<MeshRenderer>().material = sampleMaterial;
				pointObj.transform.parent = Sample.transform;
				pointObj.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
				pointObj.transform.position = new Vector3(q_new_ang[0], q_new_ang[1], q_new_ang[2]);
				pointObj.transform.rotation = Quaternion.Euler(q_new_ang[3], q_new_ang[4], q_new_ang[5]);

				GameObject lineObj = new GameObject();
				lineObj.name = "line" + near_idx + "_" + i;
				LineRenderer line = lineObj.AddComponent<LineRenderer>();
				line.positionCount = 2;
				line.startWidth = 0.005f;
				line.endWidth = 0.005f;
				line.material = lineMaterial;
				line.startColor = new Color(255f / 255f, 128f / 255f, 128f / 255f);
				line.endColor = new Color(255f / 255f, 128f / 255f, 128f / 255f);
				line.transform.parent = Sample.transform;
				line.SetPosition(0, new Vector3(vec1[0], vec1[1], vec1[2]));
				line.SetPosition(1, new Vector3(vec2[0], vec2[1], vec2[2]));

				if (Distance(q_new_coor, q_goal_coor) <= 1f) {
					Debug.Log("트리 생성 종료");
					hasPath = true;
					break;
				}
			}
		}

		// 경로 찾기
		if (!hasPath) {
			Debug.Log("경로 미존재");
			isComplete = true;
			return;
		}

		int backtracking = FREE_SAMPLE.Count - 1;
		while (backtracking > 0) {
			GameObject lineObj = new GameObject();
			lineObj.name = "Path" + backtracking;
			LineRenderer line = lineObj.AddComponent<LineRenderer>();
			line.positionCount = 2;
			line.startWidth = 0.015f;
			line.endWidth = 0.015f;
			line.material = lineMaterial;
			line.startColor = new Color(0f, 0f, 1f);
			line.endColor = new Color(0f, 0f, 1f);
			line.transform.parent = Sample.transform;
			line.SetPosition(0, new Vector3(FREE_SAMPLE[tree[backtracking]][0], FREE_SAMPLE[tree[backtracking]][1], FREE_SAMPLE[tree[backtracking]][2]));
			line.SetPosition(1, new Vector3(FREE_SAMPLE[backtracking][0], FREE_SAMPLE[backtracking][1], FREE_SAMPLE[backtracking][2]));

			shortest_path.Add(backtracking);
			backtracking = tree[backtracking];
		}
		shortest_path.Add(0);
		shortest_path.Reverse();
	}

	private float[] Coor2Angle(float[] vec)
	{
		float ang_x = Mathf.Atan2(vec[4], vec[3]) * Mathf.Rad2Deg;
		float ang_y = Mathf.Atan2(vec[6], vec[5]) * Mathf.Rad2Deg;
		float ang_z = Mathf.Atan2(vec[8], vec[7]) * Mathf.Rad2Deg;
		float[] result = new float[6] { vec[0], vec[1], vec[2], ang_x, ang_y, ang_z };
		return result;
	}

	private float[] Angle2Coor(float[] vec)
	{
		float[] result = new float[9] { vec[0], vec[1], vec[2], Mathf.Cos(vec[3] * Mathf.Deg2Rad), Mathf.Sin(vec[3] * Mathf.Deg2Rad), Mathf.Cos(vec[4] * Mathf.Deg2Rad), Mathf.Sin(vec[4] * Mathf.Deg2Rad), Mathf.Cos(vec[5] * Mathf.Deg2Rad), Mathf.Sin(vec[5] * Mathf.Deg2Rad) };
		return result;
	}

	private bool IsCollision(float[] v1, float[] v2)
	{
		int steps = (int)(1f / Time.fixedDeltaTime);
		Vector3 vec1 = new Vector3(v1[0], v1[1], v1[2]);
		Vector3 vec2 = new Vector3(v2[0], v2[1], v2[2]);
		Vector2 ang_x1 = new Vector2(v1[3], v1[4]);
		Vector2 ang_x2 = new Vector2(v2[3], v2[4]);
		Vector2 ang_y1 = new Vector2(v1[5], v1[6]);
		Vector2 ang_y2 = new Vector2(v2[5], v2[6]);
		Vector2 ang_z1 = new Vector2(v1[7], v1[8]);
		Vector2 ang_z2 = new Vector2(v2[7], v2[8]);

		for (int i = 1; i <= steps; i++)
		{
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

			for (int k = 0; k < POINTS.matrix.GetLength(1); k++)
			{
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

	private float Distance(float[] vec1, float[] vec2)
	{
		if (vec1.GetLength(0) != vec2.GetLength(0))
		{
			throw new UnityException("두 벡터의 차원이 맞지 않습니다.");
		}

		float sum = 0f;
		for (int i = 0; i < vec1.GetLength(0); i++)
		{
			sum += Mathf.Pow((vec1[i] - vec2[i]), 2);
		}

		return Mathf.Sqrt(sum);
	}

	// Update is called once per frame
	void Update()
	{
		orthogonal_camera.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + 1.711263f, this.transform.position.z);
		persepctive_camera.transform.position = new Vector3(this.transform.position.x - 0.75f, this.transform.position.y + 0.75f, this.transform.position.z + 0.75f);
	}

	private void FixedUpdate()
	{
		if (isComplete) return;

		if (current_step > shortest_path.Count - 1 || shortest_path[current_step] < 0f)
		{
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

		Vector3 pos = new Vector3(FREE_SAMPLE[current_node][0], FREE_SAMPLE[current_node][1], FREE_SAMPLE[current_node][2]);
		Quaternion ang = Quaternion.Euler(FREE_SAMPLE[current_node][3], FREE_SAMPLE[current_node][4], FREE_SAMPLE[current_node][5]);

		Vector3 dir_vec = pos - this.transform.position;
		Quaternion dir_ang = Quaternion.Slerp(this.transform.rotation, ang, speed * Time.fixedDeltaTime);
		if (dir_vec.magnitude > 0.0075f)
		{
			this.transform.position += dir_vec * speed * Time.fixedDeltaTime;
			this.transform.rotation = dir_ang;
		}
		else
		{
			this.transform.rotation = ang;
			current_step++;
		}
	}
}
