using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Simulation : MonoBehaviour
{
    [SerializeField] private ComputeShader _computeShader;
    [SerializeField] RawImage _renderImage;
    RenderTexture _renderTexture;

    [SerializeField] int _size = 200;
    int _kernel;
    Vector2Int _dispatchCount;
    
    ComputeBuffer _particlesBuffer;
    ComputeBuffer _particleTypesBuffer;

    [SerializeField] int _createdType = 1;
    [SerializeField] Color _createdColor = Color.white;

    [SerializeField] List<ParticleResource> _particleTypes = new List<ParticleResource>();

    void Start()
    {
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

        _computeShader.SetInt("Size", _size);

        InitParticles();
    }

    void InitParticles()
    {
        List<Particle> _particles = new List<Particle>();
        for(int x = 0; x < _size; x++){
            for(int y = 0; y < _size; y++){
                Particle p = new Particle();
                /*if(UnityEngine.Random.value < 0.1f) p = sandParticle;
                else if(UnityEngine.Random.value < 0.1f) p = waterParticle;*/
                p.position = new Vector2(x, y);
                _particles.Add(p);
            }
        }

        int particleSize = sizeof(float) * 2
            + sizeof(int)
        ;
        _particlesBuffer = new ComputeBuffer(Mathf.FloorToInt(Mathf.Pow(_size, 2f)), particleSize);
        _particlesBuffer.SetData(_particles);
        _computeShader.SetBuffer(_kernel, "Particles", _particlesBuffer);

        int particleTypeSize = sizeof(float) * 4
            + sizeof(int)
        ;
        List<ParticleType> particleTypes = new List<ParticleType>();
        foreach(ParticleResource particleType in _particleTypes)
        {
            ParticleType p = new ParticleType();
            p.color = particleType.color;
            p.movementType = (int)particleType.movementType;
            particleTypes.Add(p);
        }
        _particleTypesBuffer = new ComputeBuffer(_particleTypes.Count, particleTypeSize);
        _particleTypesBuffer.SetData(particleTypes);
        _computeShader.SetBuffer(_kernel, "Types", _particleTypesBuffer);
    }

    void Update()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition = (mousePosition / transform.localScale.x / (_size / 200f) + Vector2.one) * _size / 2f;
        _computeShader.SetVector("MousePosition", mousePosition);
        _computeShader.SetBool("MouseDown", Input.GetMouseButton(0));
        _computeShader.SetVector("MouseColor", _createdColor);
        _computeShader.SetInt("MouseType", _createdType);
        _computeShader.SetFloat("Time", Time.time);
        _computeShader.Dispatch(_kernel, _dispatchCount.x, _dispatchCount.y, 1);
    }
}

public struct Particle {
    public Vector2 position;
    public int particleType;
}

public struct ParticleType 
{
    public Color color;
    public int movementType;
}
