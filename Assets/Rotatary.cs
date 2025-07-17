using UnityEngine;

public class Rotatary : MonoBehaviour
{

    public float rotationSpeed = 10f; // Speed of rotation in degrees per second

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}
