using System.Collections.Generic;
using DigitalRuby.AdvancedPolygonCollider;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System;
using TMPro;
using System.IO;
using NUnit.Framework;
public class Chunk : MonoBehaviour
{
    [SerializeField] ComputeShader _computeShader;
    [SerializeField] ComputeShader _dataShader;
    [SerializeField] RawImage _renderImage;
    [SerializeField] RawImage _colliderImage;
    [SerializeField] AdvancedPolygonCollider _collider;

    RenderTexture _renderTexture;
    RenderTexture _colliderTexture;    
    [SerializeField] SpriteRenderer _colliderSpriteRenderer;
    [SerializeField] SpriteRenderer _spriteRenderer;
    [SerializeField] Material _material;
    [SerializeField] MeshRenderer _meshRenderer;
    [SerializeField] TextMeshProUGUI _debugText;
    int _size = 200;
    int _scale = 4;

    int _kernel;
    Vector2Int _dispatchCount;
    
    ComputeBuffer _particlesBuffer;
    ComputeBuffer _statesBuffer;

    bool _isActiveState = true;

    Simulation _simulation;

    [SerializeField] GameObject _colliderGameObject;
    [SerializeField] Canvas _canvas;

    [SerializeField] Image _testImage;
    public Image TestImage => _testImage;

    int _colliderParticleCount = 0; // number of active particles on last update
    int _activeParticleCount = 0;
    int _solidParticleCount = 0;
    int _particleCount = 0;

    public bool NeedsUpdate => _solidParticleCount != _colliderParticleCount;
    public int UpdateCollisionValue => Math.Abs(_colliderParticleCount - _solidParticleCount);

    public int SolidParticleCount => _solidParticleCount;
    public int ParticleCount => _particleCount;
    public int ActiveParticleCount => _activeParticleCount;
    public float SortValue = 0f;
    public TextMeshProUGUI DebugText => _debugText;

    int _step = 0;    
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

        /**
         * RENDER IMAGE
         */
        _renderTexture = new RenderTexture(_size, _size, 0, RenderTextureFormat.ARGBFloat);
        _renderTexture.enableRandomWrite = true;
        _renderTexture.wrapMode = TextureWrapMode.Clamp;
        _renderTexture.filterMode = FilterMode.Point;
        _renderTexture.Create();

        _renderImage.texture = _renderTexture;
        _renderImage.rectTransform.sizeDelta = new Vector2(_size, _size);
        _computeShader.SetTexture(_kernel, "Result", _renderTexture);

        _material = new Material(_material);
        _material.SetTexture("_MainTex", _renderTexture);
        _meshRenderer.materials = new Material[] { _material };
        _meshRenderer.transform.localScale = new Vector3(_simulation.WorldChunkSize / 2f, _simulation.WorldChunkSize / 2f, 1f);

        /**
         * COLLIDER IMAGE
         */
        _colliderTexture = new RenderTexture(_size, _size, 0, RenderTextureFormat.ARGBFloat);
        _colliderTexture.enableRandomWrite = true;
        _colliderTexture.wrapMode = TextureWrapMode.Clamp;
        _colliderTexture.filterMode = FilterMode.Point;
        _colliderTexture.Create();

        _colliderImage.texture = _colliderTexture;
        _colliderImage.rectTransform.sizeDelta = new Vector2(_size, _size);
        _computeShader.SetTexture(_kernel, "ColliderTexture", _colliderTexture);

        _colliderGameObject.transform.localScale = _canvas.transform.localScale * _scale;
        _colliderGameObject.transform.position = -Vector3.one * _simulation.WorldChunkSize / 2f + transform.position;

        /**
         * TEST IMAGE
         */
        _testImage.rectTransform.sizeDelta = new Vector2(_size, _size);

        _computeShader.SetFloat("Size", _size);

        _statesBuffer = new ComputeBuffer(7, sizeof(int));
        _statesBuffer.SetData(new int[7] { 0, 0, 0, 0, 0, 0, 0 });
        // [self state, ...neighbors states]

        ComputeBuffer defaultBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Structured, ComputeBufferMode.Immutable); 
        _computeShader.SetBuffer(_kernel, "LeftChunkParticles", defaultBuffer);
        _computeShader.SetBuffer(_kernel, "TopChunkParticles", defaultBuffer);
        _computeShader.SetBuffer(_kernel, "BottomChunkParticles", defaultBuffer);
        _computeShader.SetBuffer(_kernel, "RightChunkParticles", defaultBuffer);
        _computeShader.SetBuffer(_kernel, "States", _statesBuffer);
        _computeShader.SetVector("ChunkPosition", transform.position / _simulation.WorldChunkSize);
        defaultBuffer.Release();
        
        InitParticles();
        Load();
    }

    void InitParticles()
    {
        bool generateParticles = !(_simulation.UseStorage && HasSaveFile());
        List<Particle> _particles = new List<Particle>();
        for(int x = 0; x < _size; x++){
            for(int y = 0; y < _size; y++){
                Particle p = new Particle();
                p.position = new Vector2(x, y);
                p.realPosition = new Vector2(x, y);
                p.idleTime = 0f;

                if(generateParticles){
                    // continue; // skip generation
                    float density = Mathf.PerlinNoise(
                        (_simulation.Seed + x + transform.position.x / _simulation.WorldChunkSize * _size) / 100f,
                        (_simulation.Seed + y + transform.position.y / _simulation.WorldChunkSize * _size) / 100f
                    );
                    float moist = Mathf.PerlinNoise(
                        (_simulation.Seed  + 12345 + x + transform.position.x / _simulation.WorldChunkSize * _size) / 100f,
                        (_simulation.Seed  + 12345 + y + transform.position.y / _simulation.WorldChunkSize * _size) / 100f
                    );
                    if(density < 0.2f && moist > 0.5f) p.particleType = 3; // water
                    else if(density > 0.8) p.particleType = 1; // stone
                    else if(density > 0.5) {
                        if(moist < 0.2f) p.particleType = 2; // sand
                        else p.particleType = 8; // earth
                    }
                }
                
                _particles.Add(p);
            }
        }

        int particleSize = 
            sizeof(float) * 2 // position
            + sizeof(float) * 2 // real position
            + sizeof(float) * 2 // direction
            + sizeof(float) // speed
            + sizeof(int) // type
            + sizeof(float) // idle time
            + sizeof(float) // birth
            + sizeof(float) // wetness
        ;
        _particlesBuffer = new ComputeBuffer(Mathf.FloorToInt(Mathf.Pow(_size, 2f)), particleSize, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        
        _particlesBuffer.SetData(_particles);
        _computeShader.SetBuffer(_kernel, "Particles", _particlesBuffer);
        _computeShader.SetBuffer(_kernel, "Types", _simulation.ParticleTypesBuffer);

        UpdateNeighborBuffer();
    }

    void Update()
    {
        if(_particlesBuffer == null) return;

        float ratio = 1f / 0.0193f;
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition = ((mousePosition - (Vector2)transform.position) / _scale / (_size / ratio / 2f) + Vector2.one) * _size / 2f;
        float mouseSize = _simulation.BrushSize / _size * _simulation.WorldChunkSize;

        if(!_isActiveState && Input.GetMouseButton(0)
        && mousePosition.x + mouseSize > 0
        && mousePosition.x - mouseSize < _size
        && mousePosition.y + mouseSize > 0
        && mousePosition.y - mouseSize < _size)
        {
            SetActiveState();
        }
        
        if(!_isActiveState) return;

        _step = (_step + 1) % 4;

        _statesBuffer.SetData(new int[7] { 0, 0, 0, 0, 0, 0, 0 });
        _computeShader.SetVector("MousePosition", mousePosition);
        _computeShader.SetBool("DrawBounds", _simulation.DrawBounds);
        _computeShader.SetBool("MouseDown", Input.GetMouseButton(0));
        _computeShader.SetInt("MouseType", _simulation.CreatedType);
        _computeShader.SetInt("BrushSize", _simulation.BrushSize);
        _computeShader.SetFloat("IdleTime", _simulation.IdleTime);
        _computeShader.SetFloat("Time", Time.time);
        _computeShader.SetFloat("DeltaTime", Time.deltaTime);
        _computeShader.SetFloat("Gravity", _simulation.Gravity);
        _computeShader.SetInt("Step", _step);
        _computeShader.Dispatch(_kernel, _dispatchCount.x, _dispatchCount.y, 1);

        UpdateState();
    }

    bool _isUpdatingState = false;
    void UpdateState(){
        //if(Time.time - _lastUpdateStateTime < _updateStateTimeout) return;
        //_lastUpdateStateTime = Time.time;
        if(_isUpdatingState) return;
        _isUpdatingState = true;
        AsyncGPUReadback.Request(_statesBuffer, (request) =>
        {
            if (!request.hasError)
            {
                // Get the data from the request
                int[] data = request.GetData<int>().ToArray();
                _activeParticleCount = data[0];
                _solidParticleCount = data[5];
                _particleCount = data[6];
                if(_activeParticleCount == 0) SetActiveState(false);
                if(data[1] > 0) LeftChunk.SetActiveState();
                if(data[2] > 0) TopChunk.SetActiveState();
                if(data[3] > 0) RightChunk.SetActiveState();
                if(data[4] > 0) BottomChunk.SetActiveState();
            }
            else
            {
                Debug.LogError("AsyncGPUReadback failed.");
            }
            _isUpdatingState = false;
        });
    }

    public void SetActiveState(bool state=true){
        _isActiveState = state;
        // _testImage.enabled = state;
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

    public void UpdateNeighborBuffer()
    {
        if(LeftChunk != null) SetLeftChunk(LeftChunk);
        if(TopChunk != null) SetTopChunk(TopChunk);
        if(BottomChunk != null) SetBottomChunk(BottomChunk);
        if(RightChunk != null) SetRightChunk(RightChunk);
    }

    public void SetLeftChunk(Chunk chunk, bool mirror=true)
    {
        if(LeftChunk != null) return;
        if(_computeShader != null && chunk.GetParticlesBuffer() != null) _computeShader.SetBuffer(_kernel, "LeftChunkParticles", chunk.GetParticlesBuffer());
        LeftChunk = chunk;
        if(mirror) chunk.SetRightChunk(this, false);
    }
    public void SetTopChunk(Chunk chunk, bool mirror=true)
    {
        if(TopChunk != null) return;
        if(_computeShader != null && chunk.GetParticlesBuffer() != null) _computeShader.SetBuffer(_kernel, "TopChunkParticles", chunk.GetParticlesBuffer());
        TopChunk = chunk;
        if(mirror) chunk.SetBottomChunk(this, false);
    }
    public void SetBottomChunk(Chunk chunk, bool mirror=true)
    {
        if(BottomChunk != null) return;
        if(_computeShader != null && chunk.GetParticlesBuffer() != null) _computeShader.SetBuffer(_kernel, "BottomChunkParticles", chunk.GetParticlesBuffer());
        BottomChunk = chunk;
        if(mirror) chunk.SetTopChunk(this, false);
    }
    public void SetRightChunk(Chunk chunk, bool mirror=true)
    {
        if(RightChunk != null) return;
        if(_computeShader != null && chunk.GetParticlesBuffer() != null) _computeShader.SetBuffer(_kernel, "RightChunkParticles", chunk.GetParticlesBuffer());
        RightChunk = chunk;
        if(mirror) chunk.SetLeftChunk(this, false);
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
    float _lastUpdateColliderTime = 0;
    public float TimeSinceLastUpdateColliderTime => Time.time - _lastUpdateColliderTime;
    public float LastUpdateColliderTime => _lastUpdateColliderTime;
    public void UpdateCollider()
    {
        if(_texture == null) _texture = new Texture2D(_size, _size, TextureFormat.RGBA32, false);

        _colliderParticleCount = _solidParticleCount;
        _lastUpdateColliderTime = Time.time;

        AsyncGPUReadback.Request(_colliderTexture, 0, TextureFormat.RGBA32, (req) =>
        {
            if (!req.hasError)
            {
                AdvancedPolygonCollider tempCollider = Instantiate(_collider, transform);

                var rawData = req.GetData<Color>();
                _texture.LoadRawTextureData(rawData);
                _texture.Apply();

                _colliderSpriteRenderer.sprite = Sprite.Create(_texture, new Rect(0, 0, _size, _size), Vector2.zero, _scale);
                _collider.RecalculatePolygon();

                Destroy(tempCollider.gameObject, 0.5f);
            }
        });
    
    }

    void OnDestroy()
    {
        Release();
    }
    
    void OnDisable()
    {
        if(_simulation.UseStorage) Save();
        // Release();
    }

    void Release(){
        if (_particlesBuffer != null)
        {
            _particlesBuffer.Release();
            _particlesBuffer = null;
        }
        if(_renderTexture != null){
            _renderTexture.Release();
        }
        if(_statesBuffer != null) _statesBuffer.Release();
    }


    /**
    * Save
    */
    Texture2D ToTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(_size, _size);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    string DirPath => Application.dataPath + "/../" + "/Data/" + _simulation.Seed + "/Chunks";
    string FileName => Position.x + "_" + Position.y + ".png";
    Vector2 Position => new Vector2(transform.position.x, transform.position.y) / _simulation.WorldChunkSize;
    
    public void Save(){
        int dataKernel = _dataShader.FindKernel("CSMain");
        _dataShader.SetBuffer(dataKernel, "Particles", _particlesBuffer);
        _dataShader.SetBuffer(dataKernel, "Types", _simulation.ParticleTypesBuffer);
        _dataShader.SetTexture(dataKernel, "Result", _renderTexture);
        _dataShader.SetBool("IsSave", true);
        _dataShader.SetBool("IsLoad", false);
        _dataShader.SetInt("Size", _size);
        _dataShader.Dispatch(_kernel, _dispatchCount.x, _dispatchCount.y, 1);
        
        byte[] bytes = ToTexture2D(_renderTexture).EncodeToPNG();
        if(!Directory.Exists(DirPath)) {
            Directory.CreateDirectory(DirPath);
        }
        File.WriteAllBytes(DirPath + "/" + FileName, bytes);
    }

    public bool HasSaveFile(){
        if(!Directory.Exists(DirPath)) return false;
        if(!File.Exists(DirPath + "/" + FileName)) return false;
        return true;
    }

    public void Load()
    {

        if(!HasSaveFile()) return;
        SetActiveState(false);

        byte[] bytes = File.ReadAllBytes(DirPath + "/" + FileName);
        Texture2D tex = new Texture2D(_size, _size);
        tex.LoadImage(bytes);
        tex.Apply();
        Graphics.Blit(tex, _renderTexture);

        int dataKernel = _dataShader.FindKernel("CSMain");
        _dataShader.SetBuffer(dataKernel, "Particles", _particlesBuffer);
        _dataShader.SetBuffer(dataKernel, "Types", _simulation.ParticleTypesBuffer);
        _dataShader.SetTexture(dataKernel, "Result", _renderTexture);
        _dataShader.SetBool("IsSave", false);
        _dataShader.SetBool("IsLoad", true);
        _dataShader.SetInt("Size", _size);
        _dataShader.Dispatch(_kernel, _dispatchCount.x, _dispatchCount.y, 1);
        
        SetActiveState();
    }
}


