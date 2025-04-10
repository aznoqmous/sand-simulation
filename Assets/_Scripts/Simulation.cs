using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    [SerializeField] Chunk _chunkPrefab;
    [SerializeField] Dictionary<Vector2Int, Chunk> _chunks = new Dictionary<Vector2Int, Chunk>();

    [SerializeField] int _chunkSize = 200;
    [SerializeField] int _chunkScale = 4;

    void Start()
    {
        Vector2 screenChunks = new Vector2(
            Screen.width / (float)(_chunkSize * _chunkScale) + 1f,
            Screen.height / (float)(_chunkSize * _chunkScale) + 1f
        );
        float worldChunkSize = _chunkSize / 100f * _chunkScale;
        Debug.Log(screenChunks);
        for(int x = 0; x < screenChunks.x; x++){
            for(int y = 0; y < screenChunks.y; y++){
                Vector3 position = new Vector3((x - screenChunks.x / 2f) * worldChunkSize, (y - screenChunks.y / 2f) * worldChunkSize);
                Chunk newChunk = Instantiate(_chunkPrefab, position, Quaternion.identity, transform);
                newChunk.SetSize(_chunkSize);
                newChunk.SetScale(_chunkScale);
            }
        }   
    }
}

