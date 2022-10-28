using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBody : MonoBehaviour
{
    Rigidbody[] _ragdollRigidBodies;
    GameObject _player;
    bool _dead = false;
    Vector3 _lookTarget;
    // Start is called before the first frame update
    void Awake()
    {
        _ragdollRigidBodies = GetComponentsInChildren<Rigidbody>();
        DisableRagdoll();
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        _lookTarget = new Vector3(_player.transform.position.x, transform.position.y, _player.transform.position.z);
        if(!_dead) transform.LookAt(_lookTarget);
    }

    public void Kill()
    {
        if(!_dead)
        {
            Destroy(GetComponentInChildren<Enemy>());
            _dead = true;
            EnableRagdoll();
        }
    }

    void DisableRagdoll()
    {
        foreach(var rigidbody in _ragdollRigidBodies)
        {
            rigidbody.isKinematic = true;
        }
    }

    void EnableRagdoll()
    {
        foreach(var rigidbody in _ragdollRigidBodies)
        {
            rigidbody.isKinematic = false;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;    
            Physics.IgnoreCollision(_player.GetComponent<Collider>(), rigidbody.GetComponent<Collider>()); // ragdoll ignores player
        }
    }
}
