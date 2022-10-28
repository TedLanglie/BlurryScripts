using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowDeath : MonoBehaviour
{
    [SerializeField] Transform _DeathPos;
    // Update is called once per frame
    void Update()
    {
        if(_DeathPos!=null)
        {
            transform.position = _DeathPos.position;
            transform.rotation = _DeathPos.rotation;
        }
    }
}
