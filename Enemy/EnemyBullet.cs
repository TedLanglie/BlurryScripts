using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] float _BulletSpeed = 2;
    Collider _bulletCollider;
    GameObject _player;
    Vector3 _bulletTarget;
    Rigidbody _rb;
    // Start is called before the first frame update
    void Start()
    {
        _bulletCollider = GetComponent<Collider>();
        _rb = GetComponent<Rigidbody>();
        _player = GameObject.FindGameObjectWithTag("Player");
        _bulletTarget = (_player.transform.position - transform.position).normalized;
    }

    // Update is called once per frame
    void Update()
    {
            _rb.AddForce(_bulletTarget * _BulletSpeed);
    }

    public void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            _player.gameObject.GetComponent<PlayerHealth>().PlayerDeath();
            Destroy(gameObject);
        } else if(other.gameObject.tag != "Enemy") {
            //instantiate bullet impact effect + play sound fx
            Destroy(gameObject);
        }
    }

    private IEnumerator RangeDestroy()
    {
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }
}
