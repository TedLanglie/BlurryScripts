using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBody : MonoBehaviour
{
    Rigidbody[] _ragdollRigidBodies;
    // Start is called before the first frame update
    void Awake()
    {
        _ragdollRigidBodies = GetComponentsInChildren<Rigidbody>();
    }

    void Start()
    {
        EnableRagdoll();
    }

    void EnableRagdoll()
    {
        foreach(var rigidbody in _ragdollRigidBodies)
        {
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;    
            rigidbody.velocity += transform.forward * Random.Range(-8.0f, 8.0f);
        }
    }
}
