using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeTrailRender : MonoBehaviour
{
    TrailRenderer _smokeTrailLr;
    [SerializeField] float _TimeTillDecay;
    // Start is called before the first frame update
    void Start()
    {
        _smokeTrailLr = gameObject.GetComponent<TrailRenderer>();
        StartCoroutine(Activate());
    }

    private IEnumerator Activate()
    {
        Color c = _smokeTrailLr.startColor;
		c.a = 0.7f;
        _smokeTrailLr.startColor = c;
        yield return new WaitForSeconds(_TimeTillDecay);
        for(float i = 1f; i > 0f; i-=.01f)
        {
            yield return new WaitForSeconds(.01f);
            c.a = i;
            _smokeTrailLr.startColor = c;
        }
        Destroy(gameObject);
    }
}
