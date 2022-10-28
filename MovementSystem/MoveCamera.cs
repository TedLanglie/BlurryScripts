using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MoveCamera : MonoBehaviour
{
    [SerializeField] Transform _CameraPosition;
    [SerializeField] GameObject _DeathCam;
    [SerializeField] GameObject _EmptyRagdollPlayer;
    [SerializeField] GameObject _HUD;
    bool dead = false;

    void Update()
    {
        if(!dead) transform.position = _CameraPosition.position;
        else
        {
            die();
        }
    }

    void die()
    {
        GameObject Player = GameObject.FindGameObjectWithTag("Player");
        Vector3 bodySpawnPos = new Vector3(Player.transform.position.x, Player.transform.position.y - 1f, Player.transform.position.z);
        Instantiate(_EmptyRagdollPlayer, bodySpawnPos, transform.rotation);
        Destroy(Player);
        Destroy(_HUD);
        _DeathCam.SetActive(true);
        
        StartCoroutine(changeScenesAfterDeath());
    }

    public void setPlayerDeath()
    {
        dead = true;
    }

    private IEnumerator changeScenesAfterDeath()
    {
        yield return new WaitForSeconds(2);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


}
