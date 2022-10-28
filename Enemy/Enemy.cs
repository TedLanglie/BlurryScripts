using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    GameObject player;
    [SerializeField] BoxCollider _EnemyRange;
    bool _isPlayerInRange = false;
    [SerializeField] float _TimeInBetweenShots;
    [SerializeField] GameObject _EnemyBullet;
    [SerializeField] GameObject _GunShotEffect;
    AudioSource _gunshotSource;
    bool _currentlyShooting = false;
    // Start is called before the first frame update
    void Start()
    {
        _gunshotSource = GetComponent<AudioSource>();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if(_currentlyShooting == false && _isPlayerInRange) StartCoroutine(shoot());
    }

    private IEnumerator shoot()
    {
        _currentlyShooting = true;
        yield return new WaitForSeconds(_TimeInBetweenShots);
        _gunshotSource.Play();
        Instantiate(_EnemyBullet, transform.position, Quaternion.identity);
        Instantiate(_GunShotEffect, transform.position, Quaternion.identity);

        _currentlyShooting = false;
    }

    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("Something entered");
        if(other.gameObject.tag == "Player")
        {
            Debug.Log("Its a player");
            _isPlayerInRange = true;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            _isPlayerInRange = false;
        }
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
