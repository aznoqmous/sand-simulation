using System.Collections.Generic;
using DigitalRuby.AdvancedPolygonCollider;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Chunk : MonoBehaviour
{
    [SerializeField] private ComputeShader _computeShader;
    [SerializeField] RawImage _renderImage;
    [SerializeField] private AdvancedPolygonCollider _collider;
    RenderTexture _renderTexture;
    [SerializeField] SpriteRenderer _spriteRenderer;

    int _size = 200;
    int _scale = 4;

    int _kernel;
    Vector2Int _dispatchCount;
    
    ComputeBuffer _particlesBuffer;
    ComputeBuffer _particleTypesBuffer;

    Simulation _simulation;

    [SerializeField] GameObject _colliderGameObject;
    [SerializeField] Canvas _canvas;

    public void Init(Simulation simulation)
    {
        _simulation = simulation;
        _computeShader = Instantiate(_computeShader);

        uint threadX = 0;
        uint threadY = 0;
        uint threadZ = 0;
        _kernel = _computeShader.FindKernel("CSMain");
        _computeShader.GetKernelThreadGroupSizes(_kernel, out threadX, out threadY, out threadZ);
        _dispatchCount.x = Mathf.CeilToInt(_size / threadX);
        _dispatchCount.y = Mathf.CeilToInt(_size / threadY);

        _renderTexture = new RenderTexture(_size, _size, 0, RenderTextureFormat.ARGBFloat);
        _renderTexture.enableRandomWrite = true;
        _renderTexture.wrapMode = TextureWrapMode.Clamp;
        _renderTexture.filterMode = FilterMode.Point;
        _renderTexture.Create();
        _renderImage.texture = _renderTexture;
        _renderImage.rectTransform.sizeDelta = new Vector2(_size, _size);
        
        _computeShader.SetTexture(_kernel, "Result", _renderTexture);

        _computeShader.SetFloat("Size", _size);

        ComputeBuffer defaultBuffer = new ComputeBuffer(1, 4); 
        _computeShader.SetBuffer(_kernel, "LeftChunkParticles", defaultBuffer);
        _computeShader.SetBuffer(_kernel, "TopChunkParticles", defaultBuffer);
        _computeShader.SetBuffer(_kernel, "BottomChunkParticles", defaultBuffer);
        _computeShader.SetBuffer(_kernel, "RightChunkParticles", defaultBuffer);

        _colliderGameObject.transform.localScale = _canvas.transform.localScale * _scale;
        _colliderGameObject.transform.position = -Vector3.one * _simulation.WorldChunkSize / 2f;

        InitParticles();
    }

    void InitParticles()
    {
        List<Particle> _particles = new List<Particle>();
        for(int x = 0; x < _size; x++){
            for(int y = 0; y < _size; y++){
                Particle p = new Particle();
                p.position = new Vector2(x, y);
                p.realPosition = new Vector2(x, y);
                
                _particles.Add(p);
            }
        }

        int particleSize = 
            sizeof(float) * 2 // position
            + sizeof(float) * 2 // position
            + sizeof(float) * 2 // direction
            + sizeof(float) // speed
            + sizeof(int) // type
        ;
        _particlesBuffer = new ComputeBuffer(Mathf.FloorToInt(Mathf.Pow(_size, 2f)), particleSize);
        _particlesBuffer.SetData(_particles);
        _computeShader.SetBuffer(_kernel, "Particles", _particlesBuffer);

        int particleTypeSize = sizeof(float) * 4 // color
            + sizeof(int) // movement type
            + sizeof(float) // dispersion
        ;
        List<ParticleType> particleTypes = new List<ParticleType>();
        foreach(ParticleResource particleType in _simulation.ParticleTypes)
        {
            ParticleType p = new ParticleType();
            p.color = particleType.color;
            p.movementType = (int)particleType.movementType;
            p.dispersion = particleType.dispersion;
            particleTypes.Add(p);
        }
        _particleTypesBuffer = new ComputeBuffer(_simulation.ParticleTypes.Count, particleTypeSize);
        _particleTypesBuffer.SetData(particleTypes);
        _computeShader.SetBuffer(_kernel, "Types", _particleTypesBuffer);
    }

    void Update()
    {
        if(_kernel != 0) return;
        float ratio = 1f / 0.0193f;

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition = ((mousePosition - (Vector2)transform.position) / _scale / (_size / ratio / 2f) + Vector2.one) * _size / 2f;
        _computeShader.SetVector("MousePosition", mousePosition);
        _computeShader.SetBool("DrawBounds", _simulation.DrawBounds);
        _computeShader.SetBool("MouseDown", Input.GetMouseButton(0));
        _computeShader.SetInt("MouseType", _simulation.CreatedType);
        _computeShader.SetInt("BrushSize", _simulation.BrushSize);
        _computeShader.SetFloat("Time", Time.time);
        _computeShader.SetFloat("DeltaTime", Time.deltaTime);
        _computeShader.SetFloat("Gravity", _simulation.Gravity);
        _computeShader.Dispatch(_kernel, _dispatchCount.x, _dispatchCount.y, 1);
    }

    public void SetSize(int size){
        _size = size;
    }

    public void SetScale(int scale){
        _scale = scale;
        transform.localScale = Vector3.one * scale;
    }

    public Chunk LeftChunk;
    public Chunk TopChunk;
    public Chunk BottomChunk;
    public Chunk RightChunk;

    public void SetLeftChunk(Chunk chunk)
    {
        if(LeftChunk != null) return;
        _computeShader.SetBuffer(_kernel, "LeftChunkParticles", chunk.GetParticlesBuffer());
        LeftChunk = chunk;
        chunk.SetRightChunk(this);
    }
    public void SetTopChunk(Chunk chunk)
    {
        if(TopChunk != null) return;
        _computeShader.SetBuffer(_kernel, "TopChunkParticles", chunk.GetParticlesBuffer());
        TopChunk = chunk;
        chunk.SetBottomChunk(this);
    }
    public void SetBottomChunk(Chunk chunk)
    {
        if(BottomChunk != null) return;
        _computeShader.SetBuffer(_kernel, "BottomChunkParticles", chunk.GetParticlesBuffer());
        BottomChunk = chunk;
        chunk.SetTopChunk(this);
    }
    public void SetRightChunk(Chunk chunk)
    {
        if(RightChunk != null) return;
        _computeShader.SetBuffer(_kernel, "RightChunkParticles", chunk.GetParticlesBuffer());
        RightChunk = chunk;
        chunk.SetLeftChunk(this);
    }

    public void AddNeighbor(Chunk chunk)
    {
        if (chunk.transform.position.x < transform.position.x && chunk.transform.position.y == transform.position.y) SetLeftChunk(chunk);
        if (chunk.transform.position.x > transform.position.x && chunk.transform.position.y == transform.position.y) SetRightChunk(chunk);
        if (chunk.transform.position.x == transform.position.x && chunk.transform.position.y > transform.position.y) SetTopChunk(chunk);
        if (chunk.transform.position.x == transform.position.x && chunk.transform.position.y < transform.position.y) SetBottomChunk(chunk);
    }

    public ComputeBuffer GetParticlesBuffer()
    {
        return _particlesBuffer;
    }

    Texture2D _texture;
    public void UpdateCollider()
    {
        

        if(_texture == null) _texture = new Texture2D(_size, _size, TextureFormat.RGBA32, false);
        
        AsyncGPUReadback.Request(_renderTexture, 0, TextureFormat.RGBA32, (req) =>
        {
            if (!req.hasError)
            {
                var rawData = req.GetData<Color32>();
                _texture.LoadRawTextureData(rawData);
                _texture.Apply();

                _spriteRenderer.sprite = Sprite.Create(_texture, new Rect(0, 0, _size, _size), Vector2.zero, _scale);
                _collider.RecalculatePolygon();
            }
        });
    
    }

    Texture2D ToTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(64, 64);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }
}


public struct Particle {
    public Vector2 position;
    public Vector2 realPosition;
    public Vector2 direction;
    public float speed;
    public int particleType;
}

public struct ParticleType 
{
    public Color color;
    public int movementType;

    public float dispersion;
}
