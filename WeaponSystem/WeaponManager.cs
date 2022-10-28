using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponManager : MonoBehaviour
{
    public float PickupRange;
    public float PickupRadius;

    public int WeaponLayer;
    public float SwaySize;
    public float SwaySmooth;

    public float DefaultFov;
    public float ScopedFov;
    public float FovSmooth;

    public Transform WeaponHolder;
    public Transform PlayerCamera;
    public Transform SwayHolder;
    public TextMeshProUGUI AmmoText;
    public Camera[] PlayerCams;
    public Image CrosshairImage;

    private bool _isWeaponHeld;
    private Weapon _heldWeapon;

    private void Update() {
        CrosshairImage.gameObject.SetActive(!_isWeaponHeld || !_heldWeapon.Scoping);
        foreach (var cam in PlayerCams) {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, _isWeaponHeld && _heldWeapon.Scoping ? ScopedFov : DefaultFov, FovSmooth * Time.deltaTime);
        }

        if (_isWeaponHeld) {
            var mouseDelta = -new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            SwayHolder.localPosition = Vector3.Lerp(SwayHolder.localPosition, Vector3.zero, SwaySmooth * Time.deltaTime);
            SwayHolder.localPosition += (Vector3) mouseDelta * SwaySize;
            
            if (Input.GetKeyDown(KeyCode.Q)) {
                _heldWeapon.Drop(PlayerCamera);
                _heldWeapon = null;
                _isWeaponHeld = false;
            }
        }
        else if (Input.GetKeyDown(KeyCode.E)) {
            var hitList = new RaycastHit[256];
            var hitNumber = Physics.CapsuleCastNonAlloc(PlayerCamera.position,
                PlayerCamera.position + PlayerCamera.forward * PickupRange, PickupRadius, PlayerCamera.forward,
                hitList);
            
            var realList = new List<RaycastHit>();
            for (var i = 0; i < hitNumber; i++) {
                var hit = hitList[i];
                if (hit.transform.gameObject.layer != WeaponLayer) continue;
                if (hit.point == Vector3.zero) {
                    realList.Add(hit);
                }
                else if (Physics.Raycast(PlayerCamera.position, hit.point - PlayerCamera.position, out var hitInfo,
                    hit.distance + 0.1f) && hitInfo.transform == hit.transform) {
                    realList.Add(hit);
                }
            }

            if (realList.Count == 0) return;
            
            realList.Sort((hit1, hit2) => {
                var dist1 = GetDistanceTo(hit1);
                var dist2 = GetDistanceTo(hit2);
                return Mathf.Abs(dist1 - dist2) < 0.001f ? 0 : dist1 < dist2 ? -1 : 1;
            });

            _isWeaponHeld = true;
            _heldWeapon = realList[0].transform.GetComponent<Weapon>();
            _heldWeapon.Pickup(WeaponHolder, PlayerCamera, AmmoText);
        }
    }

    private float GetDistanceTo(RaycastHit hit) {
        return Vector3.Distance(PlayerCamera.position, hit.point == Vector3.zero ? hit.transform.position : hit.point);
    }

}
