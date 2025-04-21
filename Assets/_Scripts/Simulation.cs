using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Simulation : MonoBehaviour
{
    [SerializeField] Chunk _chunkPrefab;
    [SerializeField] Dictionary<Vector2Int, Chunk> _chunks = new Dictionary<Vector2Int, Chunk>();
    
    [Header("Chunks")]
    [SerializeField] int _chunkSize = 200;
    [SerializeField] int _chunkScale = 4;
    [SerializeField] float _activeChunkDistance = 1.5f;
    [SerializeField] bool _drawBounds = false;

    [Header("Colliders")]
    [SerializeField] bool _generateColliders = false;
    [SerializeField] bool _colliderDebug = false;
    [SerializeField] float _updateColliderFrequency = 0.5f;
    public bool DrawBounds => _drawBounds;
    public float UpdateColliderFrequency => _updateColliderFrequency;

    float _lastColliderUpdate = 0f;

    [Header("Simulation")]
    [SerializeField] float _gravity = 9.81f;
    [SerializeField] float _idleTime = 2.0f;
    [SerializeField] int _seed = 123456;
    public float Gravity => _gravity;
    public float IdleTime => _idleTime;
    public int Seed => _seed;

    [Header("Brush")]
    [SerializeField] int _brushSize = 10;
    public int BrushSize => _brushSize;

    [SerializeField] int _createdType = 1;
    public int CreatedType => _createdType;

    [SerializeField] List<ParticleResource> _particleTypes = new List<ParticleResource>();

    [SerializeField] Player _player;

    [SerializeField] Interface _interface;
    
    float _worldChunkSize = 0f;
    public float WorldChunkSize => _worldChunkSize;
    int _particleCount = 0;
    public int ParticleCount => _particleCount;
    int _activeParticleCount = 0;
    public int ActiveParticleCount => _activeParticleCount;

    public List<ParticleResource> ParticleTypes => _particleTypes;

    ComputeBuffer _particleTypesBuffer;
    public ComputeBuffer ParticleTypesBuffer => _particleTypesBuffer;
    void Start()
    {
        float ratio = 1f / 0.0193f;
        _worldChunkSize = _chunkSize / ratio * _chunkScale;

        InitParticleTypes();

        UpdateChunks();

        SetCreatedType(_createdType);
        SetBrushSize(_brushSize);

    }

    void InitParticleTypes(){
        int particleTypeSize = 
                sizeof(float) * 4 // color
                + sizeof(int) // movement type
                + sizeof(float) // dispersion
                + sizeof(int) // is solid
            ;
            List<ParticleType> particleTypes = new List<ParticleType>();
            foreach(ParticleResource particleType in ParticleTypes)
            {
                ParticleType p = new ParticleType();
                p.color = particleType.color;
                p.movementType = (int)particleType.movementType;
                p.dispersion = particleType.dispersion;
                p.isSolid = particleType.isSolid ? 1 : 0;
                particleTypes.Add(p);
            }
            _particleTypesBuffer = new ComputeBuffer(ParticleTypes.Count, particleTypeSize);
            _particleTypesBuffer.SetData(particleTypes);
    }

    Vector2Int GetScreenChunks()
    {
        Vector3 chunkSize = Camera.main.WorldToScreenPoint(new Vector3(_worldChunkSize, _worldChunkSize, 0));
        return new Vector2Int(
            Mathf.CeilToInt(Screen.width / chunkSize.x),
            Mathf.CeilToInt(Screen.height / chunkSize.y)
        );
    }

    Vector2Int GetActiveChunks()
    {
        return new Vector2Int(
            Mathf.RoundToInt(_activeChunkDistance) * 2,
            Mathf.RoundToInt(_activeChunkDistance) * 2
        );
    }

    void UpdateChunks(){

        Vector3 playerPosition = _player.transform.position / _worldChunkSize;
        Vector2Int activeChunks = GetActiveChunks();
        for(int x = - activeChunks.x; x <= activeChunks.x; x++)
        {
            for (int y = - activeChunks.y; y <= activeChunks.y; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                position += Vector2Int.FloorToInt(playerPosition);
                if(!IsChunkActive(((Vector2)position) * _worldChunkSize)) continue;
                if (!_chunks.ContainsKey(position))
                {
                    Chunk newChunk = CreateChunk(position);
                    AddChunk(position, newChunk);
                }
            }
        }

        int particleCount = 0;
        int activeParticleCount = 0;
        foreach(Chunk chunk in _chunks.Values)
        {
            chunk.gameObject.SetActive(IsChunkActive(chunk.transform.position));
            if(chunk.gameObject.activeInHierarchy) {
                particleCount += chunk.ParticleCount;
                activeParticleCount += chunk.ActiveParticleCount;
            }
        }
        _particleCount = particleCount;
        _activeParticleCount = activeParticleCount;
    }

    bool IsChunkActive(Vector2 position){
        return ((Vector2)_player.transform.position - position).magnitude < _activeChunkDistance * _worldChunkSize * 2f;
    }

    float _scrollSpeed = 1f;
    void Update()
    {
        UpdateChunks();

        if(Input.GetKey(KeyCode.LeftShift)){
            SetBrushSize(Mathf.RoundToInt(_brushSize + Input.GetAxis("Mouse ScrollWheel") * 5f));
        }
        else {
            Camera.main.orthographicSize -= Input.GetAxis("Mouse ScrollWheel") * _scrollSpeed;
        }

        if(Input.GetKeyDown(KeyCode.B))
        {
            // GetActiveChunk()?.UpdateCollider();
            GetActiveChunk()?.Save();
        }
        if(Input.GetKeyDown(KeyCode.N))
        {
            // GetActiveChunk()?.UpdateCollider();
            GetActiveChunk()?.Load();
        }

        if(Input.GetKeyDown(KeyCode.F1)) SetCreatedType(0);
        if(Input.GetKeyDown(KeyCode.F2)) SetCreatedType(1);
        if(Input.GetKeyDown(KeyCode.F3)) SetCreatedType(2);
        if(Input.GetKeyDown(KeyCode.F4)) SetCreatedType(3);

        
        UpdateColliders();
        
    }

    void SetCreatedType(int index){
        if(ParticleTypes.Count <= index) return;
        _interface.CreatedTypeText.text = ParticleTypes[index].name;
        _createdType = index;
    }

    void SetBrushSize(int size){
        _brushSize = Mathf.Clamp(size, 1, 100);
        _interface.BrushSizeText.text = _brushSize.ToString();
    }

    List<Chunk> _sortedChunks;
    void UpdateColliders()
    {
        if(!_generateColliders) return;
        if(Time.time - _lastColliderUpdate < _updateColliderFrequency) return;

        // TODO
        _lastColliderUpdate = Time.time;
        _sortedChunks = new List<Chunk>();
        foreach(Chunk chunk in _chunks.Values){
            chunk.TestImage.enabled = false;
            chunk.DebugText.enabled = false;
            if(!chunk.NeedsUpdate) continue;
            
            chunk.SortValue = 0;
            if(chunk.LastUpdateColliderTime == 0) chunk.SortValue += 100f;
            else chunk.SortValue += chunk.UpdateCollisionValue / 10f;
            float distanceScore = 1f / ((_player.transform.position - chunk.transform.position).magnitude + 1f);


            chunk.DebugText.enabled = _colliderDebug;
            chunk.DebugText.text = chunk.SortValue.ToString();
            chunk.SortValue *= distanceScore * distanceScore;

            if(chunk.SortValue < 0.1f) continue;

            _sortedChunks.Add(chunk);
            
            Color c = Color.red;
            c.a = Mathf.Min(chunk.SortValue / 10.0f, 1) / 100f;
            chunk.TestImage.color = c;
            chunk.TestImage.enabled = _colliderDebug;

        }

        if(_sortedChunks.Count == 0){
            // Debug.Log("No chunks to update");
            return;
        }
        _sortedChunks.Sort((Chunk a, Chunk b)=> Mathf.RoundToInt(b.SortValue * 100f - a.SortValue * 100f));
        Chunk chosen = _sortedChunks.First();

        // Debug.Log("Chunks requesting update : " + _sortedChunks.Count);
        // Debug.Log("Chosen chunk : " + chosen.name + " with value " + chosen.SortValue);

        Color color = Color.green;
        color.a = Mathf.Min(chosen.SortValue / 10.0f, 1) /2f;
        chosen.TestImage.color = color;
        chosen.TestImage.enabled = _colliderDebug;

        chosen.UpdateCollider();

    }

    public Vector2Int WorldToChunkPosition(Vector2 position)
    {
        return Vector2Int.RoundToInt(position / _worldChunkSize);
    }

    public Chunk GetActiveChunk(){
        Vector2Int playerPosition = WorldToChunkPosition(_player.transform.position);
        return _chunks.ContainsKey(playerPosition) ? _chunks[playerPosition] : null;
    }

    public Chunk CreateChunk(Vector2Int chunkPosition)
    {        
        Chunk chunk = Instantiate(_chunkPrefab, new Vector3(chunkPosition.x * _worldChunkSize, chunkPosition.y * _worldChunkSize), Quaternion.identity, transform);
        return chunk;
    }

    public void AddChunk(Vector2Int position, Chunk chunk)
    {
        chunk.SetSize(_chunkSize);
        chunk.SetScale(_chunkScale);
        chunk.Init(this);
        chunk.name = $"Chunk[{position.x},{position.y}]";

        if (_chunks.ContainsKey(position))
        {
            Debug.LogWarning($"Chunk at {position} already exists. Overwriting.");
            Destroy(_chunks[position].gameObject);
            _chunks[position] = chunk;
        }
        else
        {
            _chunks.Add(position, chunk);
            foreach(Chunk neighbor in GetNeighbors(position))
            {
                neighbor.AddNeighbor(chunk);
            }
        }
        // Debug.Log($"Added {chunk.name}, simulation has {_chunks.Count} chunks.");
    }

    public List<Chunk> GetNeighbors(Vector2Int position)
    {
        List<Chunk> neighbors = new List<Chunk>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                Vector2Int neighborPos = new Vector2Int(position.x + x, position.y + y);
                if (_chunks.ContainsKey(neighborPos))
                {
                    neighbors.Add(_chunks[neighborPos]);
                }
            }
        }
        return neighbors;
    }

    public void OnDestroy()
    {
        if (_particleTypesBuffer != null)
        {
            _particleTypesBuffer.Release();
            _particleTypesBuffer = null;
        }   
    }
}

