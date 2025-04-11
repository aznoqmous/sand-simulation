using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Simulation : MonoBehaviour
{
    [SerializeField] Chunk _chunkPrefab;
    [SerializeField] Dictionary<Vector2Int, Chunk> _chunks = new Dictionary<Vector2Int, Chunk>();

    [SerializeField] int _chunkSize = 200;
    [SerializeField] int _chunkScale = 4;
    [SerializeField] int _activeChunkDistance = 10;

    [SerializeField] int _createdType = 1;
    [SerializeField] List<ParticleResource> _particleTypes = new List<ParticleResource>();
    [SerializeField] bool _drawBounds = false;

    [SerializeField] Player player;

    float _worldChunkSize = 0f;

    public int CreatedType => _createdType;
    public List<ParticleResource> ParticleTypes => _particleTypes;
    public bool DrawBounds => _drawBounds;

    void Start()
    {
        float ratio = 1f / 0.0193f;
        _worldChunkSize = _chunkSize / ratio * _chunkScale;

        UpdateChunks();
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
            _activeChunkDistance + 1,
            _activeChunkDistance + 1
        );

    }

    void UpdateChunks(){
        Vector3 playerPosition = player.transform.position / _worldChunkSize;
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
            chunk.gameObject.SetActive((player.transform.position - chunk.transform.position).magnitude < _activeChunkDistance * _worldChunkSize);
        }
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
}

