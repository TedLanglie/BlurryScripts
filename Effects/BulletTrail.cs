using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletTrail : MonoBehaviour
{
    [SerializeField] private float _BulletTrailSpeed;
    private Vector3 _endPoint;
    private Vector3 _startPos;

    void Start()
    {
        _startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
            var step =  _BulletTrailSpeed * Time.deltaTime; // calculate distance to move
            transform.position = Vector3.MoveTowards(transform.position, _endPoint, step);

            // Check if the position of the cube and sphere are approximately equal.
            if (Vector3.Distance(transform.position, _endPoint) < 0.001f)
            {
                Destroy(gameObject);
            }
    }

    public void SetEndpoint(Vector3 Raycast_endPoint)
    {
        _endPoint = Raycast_endPoint;
    }
}
