using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
class CustomPolygonCollider : MonoBehaviour
{
    [SerializeField] PolygonCollider2D _polygonCollider;
    [SerializeField] Texture2D _texture;
    [SerializeField] SpriteRenderer _spriteRenderer;

    public void Start()
    {
        if(_spriteRenderer != null){
            _texture = _spriteRenderer.sprite.texture;
        }
        UpdatePolygonCollider();
    }

    public void UpdatePolygonCollider()
    {   
        Color[] pixels = _spriteRenderer.sprite.texture.GetPixels(
            Mathf.CeilToInt(_spriteRenderer.sprite.textureRect.x), 
            Mathf.CeilToInt(_spriteRenderer.sprite.textureRect.y), 
            _texture.width, 
            _texture.height, 
            0
        );
        Debug.Log(pixels);
    }
}