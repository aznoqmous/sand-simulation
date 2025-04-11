using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] float _scrollSpeed = 1f;
    
    void Start()
    {
        
    }

    void Update()
    {
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z), Time.deltaTime * 2f);
        Camera.main.orthographicSize += Input.GetAxis("Mouse ScrollWheel") * _scrollSpeed;
        transform.position += new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0) * Time.deltaTime * 5f;
    }
}
