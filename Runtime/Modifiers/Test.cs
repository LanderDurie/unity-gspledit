// using UnityEditor;

// namespace UnityEngine.GsplEdit
// {
//     [CreateAssetMenu(fileName = "SinModifier", menuName = "GsplEdit/Modifiers/Sin")]
//     public class SinModifier : Modifier
//     {
//         [Header("Sine Wave Parameters")]
//         [SerializeField] private float amplitudeX = 0.0f;
//         [SerializeField] private float amplitudeY = 0.0f;
//         [SerializeField] private float amplitudeZ = 0.0f;

//         [SerializeField] private float frequencyX = 1.0f;
//         [SerializeField] private float frequencyY = 1.0f;
//         [SerializeField] private float frequencyZ = 1.0f;

//         [SerializeField] private float phaseX = 0.0f;
//         [SerializeField] private float phaseY = 0.0f;
//         [SerializeField] private float phaseZ = 0.0f;

//         [Header("Animation Settings")]
//         [SerializeField] private bool animateX = false;
//         [SerializeField] private bool animateY = false;
//         [SerializeField] private bool animateZ = false;
//         [SerializeField] private float animationSpeed = 1.0f;

//         private float startTime;
//         private Vector3[] originalVertices;

//         private void OnEnable()
//         {
//             startTime = (Application.isPlaying) ? Time.time : (float)EditorApplication.timeSinceStartup;
//         }

//         public override void Initialize(Mesh mesh)
//         {
//             if (mesh == null)
//             {
//                 Debug.LogError("SinModifier: Mesh is null!");
//                 return;
//             }
//             originalVertices = mesh.vertices.Clone() as Vector3[];
//         }

//         public override void Run(Mesh mesh)
//         {
//             if (mesh == null || mesh.vertexCount == 0)
//                 throw new System.InvalidOperationException("Invalid mesh!");

//             if (originalVertices == null || originalVertices.Length != mesh.vertexCount)
//                 Initialize(mesh);

//             Vector3[] vertices = new Vector3[originalVertices.Length];
//             float time = (Application.isPlaying) ? Time.time - startTime : (float)EditorApplication.timeSinceStartup - startTime;
//             time *= animationSpeed;

//             for (int i = 0; i < originalVertices.Length; i++)
//             {
//                 Vector3 basePos = originalVertices[i]; // Unmodified original position
//                 Vector3 modPos = Vector3.zero;         // Reset mod position

//                 // Apply sine wave deformation just like your compute shader logic:
//                 float timeValueX = animateX ? time : 0;
//                 modPos.x = Mathf.Sin((timeValueX + basePos.x * frequencyX) + phaseX) * amplitudeX;

//                 float timeValueY = animateY ? time : 0;
//                 modPos.y = Mathf.Sin((timeValueY + basePos.z * frequencyY) + phaseY) * amplitudeY;

//                 float timeValueZ = animateZ ? time : 0;
//                 modPos.z = Mathf.Sin((timeValueZ + basePos.y * frequencyZ) + phaseZ) * amplitudeZ;

//                 vertices[i] = basePos + modPos;
//             }

//             mesh.vertices = vertices;
//             mesh.RecalculateNormals();
//             mesh.RecalculateBounds();
//         }

//         public override void DrawSettings()
//         {
//             GUILayout.Label("Sine Wave Deformation", EditorStyles.boldLabel);

//             // Amplitude settings
//             EditorGUILayout.Space();
//             EditorGUILayout.LabelField("Amplitude", EditorStyles.boldLabel);
//             amplitudeX = EditorGUILayout.FloatField("X Amplitude", amplitudeX);
//             amplitudeY = EditorGUILayout.FloatField("Y Amplitude", amplitudeY);
//             amplitudeZ = EditorGUILayout.FloatField("Z Amplitude", amplitudeZ);

//             // Frequency settings
//             EditorGUILayout.Space();
//             EditorGUILayout.LabelField("Frequency", EditorStyles.boldLabel);
//             frequencyX = EditorGUILayout.FloatField("X Frequency", frequencyX);
//             frequencyY = EditorGUILayout.FloatField("Y Frequency", frequencyY);
//             frequencyZ = EditorGUILayout.FloatField("Z Frequency", frequencyZ);

//             // Phase settings
//             EditorGUILayout.Space();
//             EditorGUILayout.LabelField("Phase", EditorStyles.boldLabel);
//             phaseX = EditorGUILayout.FloatField("X Phase", phaseX);
//             phaseY = EditorGUILayout.FloatField("Y Phase", phaseY);
//             phaseZ = EditorGUILayout.FloatField("Z Phase", phaseZ);

//             // Animation settings
//             EditorGUILayout.Space();
//             EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
//             animateX = EditorGUILayout.Toggle("Animate X", animateX);
//             animateY = EditorGUILayout.Toggle("Animate Y", animateY);
//             animateZ = EditorGUILayout.Toggle("Animate Z", animateZ);
//             animationSpeed = EditorGUILayout.FloatField("Animation Speed", animationSpeed);

//             // Apply button
//             EditorGUILayout.Space();
//             if (GUILayout.Button("Apply Deformation"))
//             {
//                 if (Selection.activeGameObject != null && Selection.activeGameObject.TryGetComponent(out MeshFilter mf))
//                 {
//                     Run(mf.sharedMesh);
//                 }
//             }
//         }
//     }
// }
