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
    [SerializeField] float _updateColliderFrequency = 0.5f;
    float _lastColliderUpdate = 0f;

    [Header("Simulation")]
    [SerializeField] float _gravity = 9.81f;
    [SerializeField] float _idleTime = 2.0f;
    [SerializeField] int _seed = 123456;

    [Header("Brush")]
    [SerializeField] int _brushSize = 10;
    [SerializeField] int _createdType = 1;

    [SerializeField] List<ParticleResource> _particleTypes = new List<ParticleResource>();

    [SerializeField] Player _player;

    public int BrushSize => _brushSize;
    public float Gravity => _gravity;
    public float IdleTime => _idleTime;
    public int Seed => _seed;
    float _worldChunkSize = 0f;
    public float WorldChunkSize => _worldChunkSize;
    public float UpdateColliderFrequency => _updateColliderFrequency;
    public int CreatedType => _createdType;
    public List<ParticleResource> ParticleTypes => _particleTypes;
    public bool DrawBounds => _drawBounds;


    ComputeBuffer _particleTypesBuffer;
    public ComputeBuffer ParticleTypesBuffer => _particleTypesBuffer;

    void Start()
    {
        float ratio = 1f / 0.0193f;
        _worldChunkSize = _chunkSize / ratio * _chunkScale;

        InitParticleTypes();

        UpdateChunks();
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
            Mathf.RoundToInt(_activeChunkDistance) + 1,
            Mathf.RoundToInt(_activeChunkDistance) + 1
        );
    }

    void UpdateChunks(){
        Vector3 playerPosition = _player.transform.position / _worldChunkSize;
        Vector2Int activeChunks = GetActiveChunks();
        for(int x = Mathf.FloorToInt(playerPosition.x) - activeChunks.x; x <= activeChunks.x; x++)
        {
            for (int y = Mathf.FloorToInt(playerPosition.y) - activeChunks.y; y <= activeChunks.y; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                if (!_chunks.ContainsKey(position))
                {
                    Chunk newChunk = CreateChunk(position);
                    AddChunk(position, newChunk);
                }
            }
        }
    }

    void Update()
    {
        UpdateChunks();
        foreach(Chunk chunk in _chunks.Values)
        {
            chunk.gameObject.SetActive((_player.transform.position - chunk.transform.position).magnitude < _activeChunkDistance * _worldChunkSize * 2f);
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            _chunks[Vector2Int.zero].UpdateCollider();
        }

        
        UpdateColliders();
        // GetActiveChunk()?.UpdateCollider();
        // Debug.Log(GetActiveChunk().name);
        // _lastColliderUpdate = Time.time;
        
    }

    List<Chunk> _sortedChunks;
    void UpdateColliders()
    {
        if(Time.time - _lastColliderUpdate < _updateColliderFrequency) return;

        // TODO
        _lastColliderUpdate = Time.time;
        _sortedChunks = new List<Chunk>();
        foreach(Chunk chunk in _chunks.Values){
            chunk.TestImage.enabled = false;
            chunk.DebugText.enabled = false;
            // if(!chunk.gameObject.activeInHierarchy) return;
            if(!chunk.NeedsUpdate) continue;
            
            chunk.SortValue = chunk.UpdateCollisionValue / 100f;
            // chunk.SortValue += chunk.TimeSinceLastUpdateColliderTime / 5f;
            if(chunk.LastUpdateColliderTime == 0) chunk.SortValue += 10f;
            chunk.SortValue *= 1f / ((_player.transform.position - chunk.transform.position).magnitude / 5f + 1f);

            if(chunk.SortValue < 0.1f) continue;

            _sortedChunks.Add(chunk);
            
            Color c = Color.red;
            c.a = Mathf.Min(chunk.SortValue / 10.0f, 1) / 10f;
            chunk.TestImage.color = c;
            chunk.TestImage.enabled = true;
            chunk.DebugText.enabled = true;
            chunk.DebugText.text = chunk.SortValue.ToString();
        }

        if(_sortedChunks.Count == 0){
            Debug.Log("No chunks to update");
            return;
        }
        _sortedChunks.Sort((Chunk a, Chunk b)=> Mathf.RoundToInt(b.SortValue * 100f - a.SortValue * 100f));
        Chunk chosen = _sortedChunks.First();

        Debug.Log("Chunks requesting update : " + _sortedChunks.Count);
        Debug.Log("Chosen chunk : " + chosen.name + " with value " + chosen.SortValue);

        Color color = Color.green;
        color.a = Mathf.Min(chosen.SortValue / 10.0f, 1) /2f;
        chosen.TestImage.color = color;
        chosen.TestImage.enabled = true;

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
        chunk.name = $"Chunk {position.x} {position.y}";

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

