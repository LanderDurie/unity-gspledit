using UnityEditor;
using System;

namespace UnityEngine.GsplEdit
{
    [CreateAssetMenu(fileName = "SoftBodyDeform", menuName = "GsplEdit/Modifiers/SoftBody")]
    [Serializable]
    public class SoftBodyDeform : Modifier
    {
        [Header("Soft Body Physics")]
        [SerializeField] private float stiffness = 0.8f;
        [SerializeField] private float mass = 1.0f;
        [SerializeField] private float damping = 0.7f;
        
        [Header("External Forces")]
        [SerializeField] private Vector3 gravity = new Vector3(0, -0.5f, 0);
        [SerializeField] private float externalForce = 0.0f;
        [SerializeField] private Vector3 forceDirection = Vector3.up;
        [SerializeField] private bool usePulseForce = false;
        [SerializeField] private float pulseFrequency = 1.0f;
        
        [Header("Deformation Constraints")]
        [SerializeField] private float maxDeformation = 1.0f;
        [SerializeField] private bool useLocalSpace = true;
        [SerializeField] private bool useVertexMass = false;
        
        [Header("Collisions")]
        [SerializeField] private bool enableFloorCollision = false;
        [SerializeField] private float floorHeight = 0.0f;
        [SerializeField] private float floorBounciness = 0.5f;
        
        [SerializeField, HideInInspector] private ComputeShader computeShader;
        
        // Buffers to hold physics state
        private GraphicsBuffer _velocityBuffer;
        private GraphicsBuffer _positionBuffer;
        private bool _initialized = false;
        private int _vertexCount = 0;
        
        public override void Initialize(Mesh mesh)
        {
            _vertexCount = mesh.vertexCount;
            
            // Clean up if already initialized
            CleanUp();
            
            // Initialize physics buffers
            _velocityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _vertexCount, sizeof(float) * 3);
            _positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _vertexCount, sizeof(float) * 3);
            
            // Initialize with zeros to start from rest state
            Vector3[] zeros = new Vector3[_vertexCount];
            _velocityBuffer.SetData(zeros);
            
            _initialized = true;
        }
        
        private void CleanUp()
        {
            if (_velocityBuffer != null)
            {
                _velocityBuffer.Release();
                _velocityBuffer = null;
            }
            
            if (_positionBuffer != null)
            {
                _positionBuffer.Release();
                _positionBuffer = null;
            }
            
            _initialized = false;
        }
        
        public override void Run(ref GraphicsBuffer baseVertices, ref GraphicsBuffer modVertices)
        {
            if (computeShader == null)
            {
                Debug.LogError("SoftBodyDeform: Missing compute shader!");
                return;
            }
            
            // Ensure initialization
            if (!_initialized || _vertexCount == 0)
            {
                Debug.LogWarning("SoftBodyDeform: Not properly initialized. Skipping run.");
                return;
            }
            
            // Calculate time
            float time = (Application.isPlaying) 
                ? Time.time 
                : (float)UnityEditor.EditorApplication.timeSinceStartup;
            
            float deltaTime = Time.deltaTime;
            if (!Application.isPlaying)
            {
                // Use a fixed time step when in editor
                deltaTime = 1.0f / 60.0f;
            }
            
            // Clamp deltaTime to avoid instability
            deltaTime = Mathf.Min(deltaTime, 0.03f);
            
            // Calculate external force based on pulse if enabled
            float currentForce = externalForce;
            if (usePulseForce)
            {
                currentForce *= Mathf.Abs(Mathf.Sin(time * pulseFrequency * Mathf.PI));
            }
            
            // Set compute shader parameters
            computeShader.SetBuffer(0, "_VertexBasePos", baseVertices);
            computeShader.SetBuffer(0, "_VertexModPos", modVertices);
            computeShader.SetBuffer(0, "_VelocityBuffer", _velocityBuffer);
            computeShader.SetBuffer(0, "_PositionBuffer", _positionBuffer);
            
            computeShader.SetFloat("deltaTime", deltaTime);
            computeShader.SetFloat("stiffness", stiffness);
            computeShader.SetFloat("mass", mass);
            computeShader.SetFloat("damping", damping);
            computeShader.SetVector("gravity", gravity);
            computeShader.SetVector("externalForceDir", forceDirection.normalized);
            computeShader.SetFloat("externalForceMag", currentForce);
            computeShader.SetFloat("maxDeformation", maxDeformation);
            computeShader.SetBool("useLocalSpace", useLocalSpace);
            computeShader.SetBool("useVertexMass", useVertexMass);
            computeShader.SetBool("enableFloorCollision", enableFloorCollision);
            computeShader.SetFloat("floorHeight", floorHeight);
            computeShader.SetFloat("floorBounciness", floorBounciness);
            
            // Dispatch the compute shader
            int threadGroups = Mathf.CeilToInt(_vertexCount / 256.0f);
            computeShader.Dispatch(0, threadGroups, 1, 1);
        }
        
        public override void DrawSettings()
        {
            GUILayout.Label("Soft Body Deformation", EditorStyles.boldLabel);
            
            // Physics parameters
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Physics Parameters", EditorStyles.boldLabel);
            stiffness = EditorGUILayout.Slider("Stiffness", stiffness, 0f, 1f);
            mass = EditorGUILayout.FloatField("Mass", mass);
            damping = EditorGUILayout.Slider("Damping", damping, 0f, 1f);
            
            // External forces
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("External Forces", EditorStyles.boldLabel);
            gravity = EditorGUILayout.Vector3Field("Gravity", gravity);
            externalForce = EditorGUILayout.FloatField("External Force", externalForce);
            forceDirection = EditorGUILayout.Vector3Field("Force Direction", forceDirection);
            usePulseForce = EditorGUILayout.Toggle("Pulse Force", usePulseForce);
            
            if (usePulseForce)
            {
                EditorGUI.indentLevel++;
                pulseFrequency = EditorGUILayout.FloatField("Pulse Frequency", pulseFrequency);
                EditorGUI.indentLevel--;
            }
            
            // Deformation constraints
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Deformation Constraints", EditorStyles.boldLabel);
            maxDeformation = EditorGUILayout.FloatField("Max Deformation", maxDeformation);
            useLocalSpace = EditorGUILayout.Toggle("Use Local Space", useLocalSpace);
            useVertexMass = EditorGUILayout.Toggle("Use Vertex Mass", useVertexMass);
            
            // Collision
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collision", EditorStyles.boldLabel);
            enableFloorCollision = EditorGUILayout.Toggle("Floor Collision", enableFloorCollision);
            
            if (enableFloorCollision)
            {
                EditorGUI.indentLevel++;
                floorHeight = EditorGUILayout.FloatField("Floor Height", floorHeight);
                floorBounciness = EditorGUILayout.Slider("Floor Bounciness", floorBounciness, 0f, 1f);
                EditorGUI.indentLevel--;
            }
            
            // Add a button to reset simulation
            EditorGUILayout.Space();
            if (GUILayout.Button("Reset Simulation"))
            {
                if (_velocityBuffer != null && _initialized)
                {
                    Vector3[] zeros = new Vector3[_vertexCount];
                    _velocityBuffer.SetData(zeros);
                }
            }
        }
        
        private void OnDisable()
        {
            CleanUp();
        }
    }
}