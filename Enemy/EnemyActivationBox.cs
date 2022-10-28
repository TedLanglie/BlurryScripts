using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyActivationBox : MonoBehaviour
{
    GameObject player;
    [SerializeField] GameObject _Enemy;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }
    public void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            _Enemy.SetActive(true);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            _Enemy.SetActive(false);
        }
    }
}
