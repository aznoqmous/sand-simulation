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
        Debug.Log(Screen.width + " " + Screen.height);
        for(int x = 0; x < Screen.width / (_chunkSize * _chunkScale) + 1; x++){
            for(int y = 0; y < Screen.height / (_chunkSize * _chunkScale) + 1; y++){
                Vector3 position = new Vector3(x * _chunkSize / 100f * _chunkScale, y * _chunkSize / 100f * _chunkScale);
                Chunk newChunk = Instantiate(_chunkPrefab, position, Quaternion.identity, transform);
                newChunk.SetSize(_chunkSize);
                newChunk.SetScale(_chunkScale);
            }
        }   
    }
}

