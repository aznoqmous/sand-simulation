using TMPro;
using UnityEngine;

public class Interface : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _fpsText;

    [SerializeField] float _fpsTimeout = 0.5f;
    float _lastFpsUpdateTime = 0f;

    void Update()
    {
        if(Time.time - _lastFpsUpdateTime < _fpsTimeout) return;
        _lastFpsUpdateTime = Time.time;
        _fpsText.text = Mathf.RoundToInt(1f / Time.deltaTime).ToString();
    }
}