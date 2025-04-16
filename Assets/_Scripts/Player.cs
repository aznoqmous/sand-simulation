using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] float _scrollSpeed = 1f;
    [SerializeField] float _moveSpeed = 10f;
    [SerializeField] float _jumpSpeed = 20f;
    [SerializeField] Rigidbody2D _rigidbody2D;

    void Start()
    {
        
    }

    void Update()
    {
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z), Time.deltaTime * 2f);
        Camera.main.orthographicSize -= Input.GetAxis("Mouse ScrollWheel") * _scrollSpeed;
        
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
    }
    void FixedUpdate()
    {
        if(_rigidbody2D.bodyType == RigidbodyType2D.Static) return;
        
        Vector2 speed = _rigidbody2D.linearVelocity;
        speed.x = Input.GetAxis("Horizontal");
        _rigidbody2D.linearVelocity = new Vector2(speed.x * _moveSpeed / 10f, speed.y);
    }
}
