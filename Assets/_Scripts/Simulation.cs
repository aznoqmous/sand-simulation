using System.Collections.Generic;
using UnityEngine;

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

    public int CreatedType => _createdType;
    public List<ParticleResource> ParticleTypes => _particleTypes;
    public bool DrawBounds => _drawBounds;

    void Start()
    {
        Vector2 screenChunks = new Vector2(
            Screen.width / (float)(_chunkSize * _chunkScale) + 2f,
            Screen.height / (float)(_chunkSize * _chunkScale) + 2f
        );

        Debug.Log(Screen.width + " " + Screen.height + " " + screenChunks.x + " " + screenChunks.y);

        float ratio = 1f / 0.0193f;

        float worldChunkSize = _chunkSize / ratio * _chunkScale;
        for(int x = 0; x < screenChunks.x; x++){
            for(int y = 0; y < screenChunks.y; y++){
                Vector3 position = new Vector3((x - screenChunks.x / 2f) * worldChunkSize, (y - screenChunks.y / 2f) * worldChunkSize);
                Chunk newChunk = Instantiate(_chunkPrefab, position, Quaternion.identity, transform);
                newChunk.SetSize(_chunkSize);
                newChunk.SetScale(_chunkScale);
                newChunk.Init(this);
                newChunk.name = $"Chunk {x} {y}";
                AddChunk(new Vector2Int(x, y), newChunk);
            }
        }
    }

    void Update()
    {
        foreach(Chunk chunk in _chunks.Values)
        {
            chunk.gameObject.SetActive((player.transform.position - chunk.transform.position).magnitude < _activeChunkDistance);
        }
    }

    public void AddChunk(Vector2Int position, Chunk chunk)
    {
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

