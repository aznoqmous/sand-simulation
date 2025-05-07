using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
class SimulationPolygonCollider : MonoBehaviour
{
    [SerializeField] PolygonCollider2D _polygonCollider;
    [SerializeField] Texture2D _texture;
    [SerializeField] SpriteRenderer _spriteRenderer;
    [SerializeField] float _alphaThreshold = 0;

    int _width;
    int _height;

    public void Start()
    {
        if(_spriteRenderer != null){
            _texture = _spriteRenderer.sprite.texture;
            UpdatePolygon();
        }
    }

    public void UpdatePolygon(){
        _width = Mathf.FloorToInt(_spriteRenderer.sprite.textureRect.width);
        _height = Mathf.FloorToInt(_spriteRenderer.sprite.textureRect.height);
        Color[] pixels = _spriteRenderer.sprite.texture.GetPixels(
            0, 
            0,
            _width, 
            _height, 
            0
        );
        
        UpdatePolygonCollider(pixels, _width, _height);
    }

    public void UpdatePolygonCollider(Color[] pixels, int width, int height)
    {   
        _width = width;
        _height = height;
        float scale = 1f/100f*2f;
        Vector2 offset = - new Vector2(_width, _height) / 2f;
        
        /* Marching square */
        Debug.Log("Pixels " + pixels.Length);
        Debug.Log("Width " + _width);
        Debug.Log("Height " + _height);
        List<Vector2> path = new List<Vector2>();
        for(int y = -1; y < _height; y++){
            for(int x = -1; x < _width; x++){
                int value = 0;
                if(GetValue(x, y, pixels)) value += 0x1000;
                if(GetValue(x+1, y, pixels)) value += 0x0100;
                if(GetValue(x+1, y+1, pixels)) value += 0x0010;
                if(GetValue(x, y+1, pixels)) value += 0x0001;
                switch(value){
                    case 0x0000: break;
                    case 0x1000:
                        path.Add((new Vector2(x, y) + offset) * scale);
                        path.Add((new Vector2(x, y + 0.5f) + offset) * scale);
                        path.Add((new Vector2(x + 0.5f, y) + offset) * scale);
                        break;
                    case 0x0100:
                        path.Add((new Vector2(x + 1.0f, y) + offset) * scale);
                        path.Add((new Vector2(x + 0.5f, y) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 0.5f) + offset) * scale);
                        break;
                    case 0x0010: 
                        path.Add((new Vector2(x + 1.0f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 0.5f) + offset) * scale);
                        path.Add((new Vector2(x + 0.5f, y + 1.0f) + offset) * scale);
                        break;
                    case 0x0001: 
                        path.Add((new Vector2(x, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x, y + 0.5f) + offset) * scale);
                        break;
                    case 0x0011:
                        path.Add((new Vector2(x + 1.0f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 0.5f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 0.5f) + offset) * scale);
                        break;
                    case 0x1100:
                        path.Add((new Vector2(x + 1.0f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 0.5f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 0.5f) + offset) * scale);
                        break;
                    case 0x0110:
                        path.Add((new Vector2(x + 1.0f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.5f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.5f, y + 1.0f) + offset) * scale);
                        break;
                    case 0x1001:
                        path.Add((new Vector2(x + 0.0f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.5f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.5f, y + 1.0f) + offset) * scale);
                        break;
                    case 0x1010:
                        path.Add((new Vector2(x + 0.5f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 0.5f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 1.0f) + offset) * scale);

                        path.Add((new Vector2(x + 0.0f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 0.5f) + offset) * scale);
                        path.Add((new Vector2(x + 0.5f, y + 0.0f) + offset) * scale);
                        break;
                    case 0x0101:
                        path.Add((new Vector2(x + 1.0f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.5f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 0.5f) + offset) * scale);

                        path.Add((new Vector2(x + 0.0f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 0.5f) + offset) * scale);
                        path.Add((new Vector2(x + 0.5f, y + 1.0f) + offset) * scale);
                        break;
                    case 0x0111:
                        path.Add((new Vector2(x + 0.5f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 0.5f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 0.0f) + offset) * scale);
                        break;
                    case 0x1011:
                        path.Add((new Vector2(x + 0.5f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 0.5f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 0.0f) + offset) * scale);
                        break;
                    case 0x1101:
                        path.Add((new Vector2(x + 0.5f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 0.5f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 1.0f) + offset) * scale);
                        break;
                    case 0x1110:
                        path.Add((new Vector2(x + 0.5f, y + 1.0f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 0.5f) + offset) * scale);
                        path.Add((new Vector2(x + 0.0f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 0.0f) + offset) * scale);
                        path.Add((new Vector2(x + 1.0f, y + 1.0f) + offset) * scale);
                        break;
                    default: break;
                }
            }
        }

        Vector2 prev = Vector2.zero;
        foreach(Vector2 p in path){
            Debug.DrawLine((Vector3) prev + transform.position, (Vector3) p + transform.position, Color.red, 5f);
            prev = p;
        }
        _polygonCollider.SetPath(0, path);
    }

    bool GetValue(int x, int y, Color[] pixels){
        if(x < 0 || y < 0 || x > _width - 1 || y > _height - 1) return false;
        return pixels[x + y * Mathf.FloorToInt(_width)].a > _alphaThreshold;
    }
}