using UnityEngine;

public class RotatingCube : MonoBehaviour
{
    private readonly Vector3 _rotation = new(15, 30, 45);

    private void Update()
    {
        transform.Rotate(_rotation * Time.deltaTime);
    }
}
