using TMPro;
using UnityEngine;

public class Interface : MonoBehaviour
{
    [SerializeField] Simulation _simulation;
    [SerializeField] TextMeshProUGUI _fpsText;
    [SerializeField] TextMeshProUGUI _particlesCountText;

    [SerializeField] float _fpsTimeout = 0.5f;
    float _lastFpsUpdateTime = 0f;

    void Update()
    {
        if(Time.time - _lastFpsUpdateTime < _fpsTimeout) return;
        _lastFpsUpdateTime = Time.time;
        _fpsText.text = Mathf.RoundToInt(1f / Time.deltaTime).ToString();
        _particlesCountText.text = $"{_simulation.ActiveParticleCount}/{_simulation.ParticleCount}";
    }
}