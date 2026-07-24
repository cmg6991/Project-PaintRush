using System.Collections.Generic;
using Unity.VisualScripting;
//using UnityEditor;
//using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
//using UnityEngine.WSA;
using static UnityEngine.RuleTile.TilingRuleOutput;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(EdgeCollider2D))]
[RequireComponent(typeof(WaterTriggerHandler))]
public class InteractableWater : MonoBehaviour
{
    [Header("Springs")]
    [SerializeField] private float _spriteConstant = 1.4f;
    [SerializeField] private float _damping = 1.1f;
    [SerializeField] private float _spread = 6.5f;
    [SerializeField, Range(1, 10)] private int _wavePropogationIterations = 8;
    [SerializeField, Range(0f, 20f)] private float _speedMult = 5.5f;

    [Header("Force")]
    public float ForceMultiplier = 0.2f;
    [Range(1f, 50f)] public float MaxForce = 5f;

    [Header("Collision")]
    [SerializeField, Range(1f, 10f)] private float _playerCollisionRadiusMult = 4.15f;


    [Header("Mesh Gernerator")]
    [Range(2, 500)] public int NumOfVertices = 70;
    public float width = 10f;
    public float height = 4f;
    public Material waterMaterial;
    private const int NUM_OF_Y_VERTICES = 2;

    [Header("Gizmo")]
    public Color GizmoColor = Color.white;

    private Mesh _mesh;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private Vector3[] _vertices;
    private int[] topVerticesindex;
    private EdgeCollider2D _coll;

    [SerializeField] private ObjectPool splashPool;

    private class WaterPoint
    {
        public float velocity, pos, targetHeight;
    }

    private List<WaterPoint> _waterPoints = new List<WaterPoint>();
    private void Awake()
    {
        _coll = GetComponent<EdgeCollider2D>();
        GenerateMesh();
        CreateWaterPoints();
    }
    private void Reset()
    {
        _coll = GetComponent<EdgeCollider2D>();
        _coll.isTrigger = true;
    }

    public float GetSurfaceY()
    {
        return transform.TransformPoint(_vertices[topVerticesindex[0]]).y;
    }

    public void SpawnSplashParticle(float worldX)
    {
        Vector3 pos = new Vector3(worldX, GetSurfaceY(), 0);

        WaterPool splash = splashPool.Get<WaterPool>();
        splash.transform.position = pos;
    }

    private void FixedUpdate()
    {
        for (int i = 1; i < _waterPoints.Count - 1; i++)
        {
            WaterPoint point = _waterPoints[i];

            float x = point.pos - point.targetHeight;
            float accelration = -_spriteConstant * x - _damping * point.velocity;
            point.pos += point.velocity * _speedMult * Time.fixedDeltaTime;
            _vertices[topVerticesindex[i]].y = point.pos;
            point.velocity += accelration * _speedMult * Time.fixedDeltaTime;
        }

        for (int j = 0; j < _wavePropogationIterations; j++)
        {
            for(int i=1;i<_waterPoints.Count - 1;i++)
            {
                float leftDelta = _spread * (_waterPoints[i].pos - _waterPoints[i - 1].pos) * _speedMult * Time.fixedDeltaTime;
                _waterPoints[i - 1].velocity += leftDelta;

                float rightDelta = _spread * (_waterPoints[i].pos - _waterPoints[i + 1].pos) * _speedMult * Time.fixedDeltaTime;
                _waterPoints[i + 1].velocity += rightDelta;
            }
        }

        _mesh.vertices = _vertices;
        _mesh.RecalculateBounds();

        ResetEdgeCollider();
    }

    //public void Splash(Collider2D collision, float force)
    //{
    //    Debug.Log("Splash È£ÃâµÊ, force = " + force);
    //    float radius = collision.bounds.extents.x * _playerCollisionRadiusMult;
    //    Vector2 center = collision.transform.position;

    //    for (int i = 0; i < _waterPoints.Count; i++)
    //    {
    //        Vector2 vertexWorldPos = transform.TransformPoint(_vertices[topVerticesindex[i]]);

    //        if (IsPointInsideCircle(vertexWorldPos, center, radius))
    //        {
    //            _waterPoints[i].velocity = force;
    //        }
    //    }
    //}
    public void Splash(float worldX, float force)
    {
        int closest = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < _waterPoints.Count; i++)
        {
            float vertexX = transform.TransformPoint(_vertices[topVerticesindex[i]]).x;
            float distance = Mathf.Abs(worldX - vertexX);

            if (distance < 0.5f) // ¹Ý°æ 0.5 À¯´Ö
            {
                float falloff = 1f - (distance / 0.5f);
                _waterPoints[i].velocity += force * falloff;
            }
        }

        _waterPoints[closest].velocity += force;
    }
    private bool IsPointInsideCircle(Vector2 point, Vector2 center, float radius)
    {
        float distance = (point - center).sqrMagnitude;
        return distance <= radius * radius;
    }
    public void ResetEdgeCollider()
    {
        _coll = GetComponent<EdgeCollider2D>();
        Vector2[] newPoints = new Vector2[2];
        Vector2 firstPoint = new Vector2(_vertices[topVerticesindex[0]].x, _vertices[topVerticesindex[0]].y);
        newPoints[0] = firstPoint;

        Vector2 secondPoint = new Vector2(_vertices[topVerticesindex[topVerticesindex.Length - 1]].x, _vertices[topVerticesindex[topVerticesindex.Length - 1]].y);
        newPoints[1] = secondPoint;

        _coll.offset = Vector2.zero;
        _coll.points = newPoints;
    }

    public void GenerateMesh()
    {
        _mesh = new Mesh();
        _vertices = new Vector3[NumOfVertices * NUM_OF_Y_VERTICES];
        topVerticesindex = new int[NumOfVertices];
        for (int y = 0; y < NUM_OF_Y_VERTICES; y++)
        {
            for (int x = 0; x < NumOfVertices; x++)
            {
                float xPos = (x / (float)(NumOfVertices - 1)) * width - width / 2;
                float yPos = (y / (float)(NUM_OF_Y_VERTICES - 1)) * height - height / 2;
                _vertices[y * NumOfVertices + x] = new Vector3(xPos, yPos, 0f);

                if (y == NUM_OF_Y_VERTICES - 1)
                {
                    topVerticesindex[x] = y * NumOfVertices + x;
                }
            }
        }
        int[] triangles = new int[(NumOfVertices - 1) * (NUM_OF_Y_VERTICES - 1) * 6];
        int index = 0;

        for(int y=0;y<NUM_OF_Y_VERTICES-1;y++)
        {
            for(int x=0;x<NumOfVertices-1;x++)
            {
                int bottomLeft = y * NumOfVertices + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + NumOfVertices;
                int topRight = topLeft + 1;

                triangles[index++] = bottomLeft;
                triangles[index++] = topLeft;
                triangles[index++] = bottomRight;
                
                triangles[index++] = bottomRight;
                triangles[index++] = topLeft;
                triangles[index++] = topRight;
            }
        }
        Vector2[] uvs = new Vector2[_vertices.Length];
        for(int i=0;i<_vertices.Length;i++)
        {
            uvs[i] = new Vector2((_vertices[i].x + width / 2) / width, (_vertices[i].y + height / 2) / height);
        }
        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        _meshRenderer.material = waterMaterial;

        _mesh.vertices = _vertices;
        _mesh.triangles = triangles;
        _mesh.uv = uvs;

        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _meshFilter.mesh = _mesh;
    }
    private void CreateWaterPoints()
    {
        _waterPoints.Clear();

        for(int i=0;i<topVerticesindex.Length;i++)
        {
            _waterPoints.Add(new WaterPoint
            {
                pos = _vertices[topVerticesindex[i]].y,
                targetHeight = _vertices[topVerticesindex[i]].y,
            });
        }
    }
}
//[CustomEditor(typeof(InteractableWater))]
//public class InteractableWaterEditor : Editor
//{
//    private InteractableWater _water;

//    private void OnEnable()
//    {
//        _water = (InteractableWater)target;
//    }
//    public override VisualElement CreateInspectorGUI()
//    {
//        VisualElement root = new VisualElement();
//        InspectorElement.FillDefaultInspector(root, serializedObject, this);
//        root.Add(new VisualElement { style = { height = 10 } });
//        Button generateMeshButton = new Button(() => _water.GenerateMesh())
//        {
//            text = "Generate Mesh"
//        };
//        root.Add(generateMeshButton);
//        Button placeEdgeColliderButton = new Button(() => _water.ResetEdgeCollider())
//        {
//            text = "Place Edge Collider"
//        };
//        root.Add(placeEdgeColliderButton);
//        return root;
//    }

//    private void ChangeDimensions(ref float width, ref float height, float calculatedWidthMax, float calculateHeightMax)
//    {
//        width = Mathf.Max(0.1f, calculatedWidthMax);
//        height = Mathf.Max(0.1f, calculateHeightMax);
//    }

//}