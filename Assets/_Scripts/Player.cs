using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] float _cameraSpeed = 10f;
    [SerializeField] float _moveSpeed = 10f;
    [SerializeField] float _jumpSpeed = 20f;
    [SerializeField] Rigidbody2D _rigidbody2D;
    [SerializeField] SpriteRenderer _sprite;
    
    void Update()
    {
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z), Time.deltaTime * _cameraSpeed);

        
        if(Input.GetKeyDown(KeyCode.T))
        {
            _rigidbody2D.bodyType = _rigidbody2D.bodyType == RigidbodyType2D.Dynamic ? RigidbodyType2D.Static : RigidbodyType2D.Dynamic;
        }

        if(_rigidbody2D.bodyType == RigidbodyType2D.Static)
        {
            transform.position += new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0) * Time.deltaTime  * _moveSpeed / 10f;

        }
        if(Input.GetKeyDown(KeyCode.W)){
            _rigidbody2D.AddForce(Vector2.up * _jumpSpeed, ForceMode2D.Impulse);
        }

        _sprite.transform.localScale = new Vector3(Input.GetAxis("Horizontal") < 0 ? -1 : 1, 1, 1);
    }
    
    void FixedUpdate()
    {
        if(_rigidbody2D.bodyType == RigidbodyType2D.Static) return;
        
        Vector2 speed = _rigidbody2D.linearVelocity;
        speed.x = Input.GetAxis("Horizontal");
        if(Input.GetKey(KeyCode.W)) {
            speed.y += _moveSpeed / 100f;
        }
        _rigidbody2D.linearVelocity = new Vector2(speed.x * _moveSpeed / 10f, speed.y);
    }
}
