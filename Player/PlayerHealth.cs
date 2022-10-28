using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] GameObject _CameraWithDeathScript;
    public void PlayerDeath()
    {
        _CameraWithDeathScript.gameObject.GetComponent<MoveCamera>().setPlayerDeath();
    }
}
