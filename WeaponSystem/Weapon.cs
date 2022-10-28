using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Weapon : MonoBehaviour
{
    [Header("Assignables")]
    [SerializeField] GunData _GunData;
    [SerializeField] AudioClip _PickUpSound;
    [SerializeField] AudioClip _ShotSound;
    [SerializeField] AudioClip _ReloadSound;
    [SerializeField] GameObject _EnemyImpactEffect;
    public Transform MuzzlePosition;
    public GameObject GunshotEffect;
    public GameObject SmokeTrailEffect;
    public GameObject BulletTrailEffect;
    public Vector3 ScopePos;
    public GameObject[] WeaponGfxs;
    public Collider[] GfxColliders;
    public Animator GunAnim;
    private int _weaponGfxLayer = 8; // must change if weapons on new layer
    private LayerMask _playerMask;

    private float _rotationTime;
    private float _time;
    private float timeSinceLastSmokeTrail = 0; // use this to prevent smoke spam on auto guns
    private float fullTimer = 0; // use this to prevent smoke spam on auto guns
    private bool _held;
    private bool _scoping;
    private bool _reloading;
    private bool _shooting;
    private int _ammo;
    private int _totalAmmo;
    private Rigidbody _rb;
    private Transform _playerCamera;
    private TMP_Text _ammoText;
    private Vector3 _startPosition;
    private Quaternion _startRotation;

    private void Start() {
        _rb = gameObject.AddComponent<Rigidbody>();
        _rb.mass = 0.1f;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _ammo = _GunData.maxAmmo;
        _totalAmmo = _GunData.totalAmmo;

        _playerMask = LayerMask.GetMask("Player");
    }

    private void Update() {
        if (!_held) return;

        if (_time < _GunData.animTime) {
            _time += Time.deltaTime;
            _time = Mathf.Clamp(_time, 0f, _GunData.animTime);
            var delta = -(Mathf.Cos(Mathf.PI * (_time / _GunData.animTime)) - 1f) / 2f;
            transform.localPosition = Vector3.Lerp(_startPosition, Vector3.zero, delta);
            transform.localRotation = Quaternion.Lerp(_startRotation, Quaternion.identity, delta);
        }
        else {
            _scoping = Input.GetMouseButton(1) && !_reloading;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.Lerp(transform.localPosition, _scoping ? ScopePos : Vector3.zero, _GunData.resetSmooth * Time.deltaTime);
        }

        if (_reloading) {
            _rotationTime += Time.deltaTime;
            var spinDelta = -(Mathf.Cos(Mathf.PI * (_rotationTime / _GunData.reloadSpeed)) - 1f) / 2f;
            transform.localRotation = Quaternion.Euler(new Vector3(spinDelta * 360f, 0, 0));
        }
        
        if (Input.GetKeyDown(KeyCode.R) && !_reloading && _ammo < _GunData.maxAmmo) {
            StartCoroutine(ReloadCooldown());
        }

        if ((_GunData.tapable ? Input.GetMouseButtonDown(0) : Input.GetMouseButton(0)) && !_shooting && !_reloading && _totalAmmo > 0) {
            _ammo--;
            _totalAmmo--;
            _ammoText.text = _ammo + " / " + _GunData.maxAmmo + "  TOTAL: " + _totalAmmo;
            Shoot();
            StartCoroutine(_ammo <= 0 && _totalAmmo > 0 ? ReloadCooldown() : ShootingCooldown());
        }

        fullTimer+=Time.deltaTime;
    }

    private void Shoot() {
        GameObject shotEffect = Instantiate(GunshotEffect, MuzzlePosition.position, MuzzlePosition.rotation);
        shotEffect.transform.parent = gameObject.transform;

        if(fullTimer - timeSinceLastSmokeTrail > 1f)
        {
            GameObject smokeEffect = Instantiate(SmokeTrailEffect, MuzzlePosition.position, MuzzlePosition.rotation);
            smokeEffect.transform.parent = gameObject.transform;
            timeSinceLastSmokeTrail = fullTimer;
        }

        GunAnim.SetTrigger("Shoot"); // play shoot animation clip
        
        SoundManager.instance.PlaySound(_ShotSound);

        transform.localPosition -= new Vector3(0, 0, _GunData.kickbackForce);
        if (!Physics.Raycast(_playerCamera.position, _playerCamera.forward, out var hitInfo, _GunData.range, ~_playerMask)) return;

        // bullet trail effect now that we've raycast, does not show when there is nothing hit
        if(hitInfo.point != null)
        {
            GameObject bulletTrail = Instantiate(BulletTrailEffect, MuzzlePosition.position, MuzzlePosition.rotation);
            bulletTrail.transform.parent = gameObject.transform;
            bulletTrail.GetComponent<BulletTrail>().SetEndpoint(hitInfo.point);
        }

        // if what we hit is damageable, take damage (could add clause that says also != player to prevent team fire)
        if(hitInfo.transform.GetComponent<IDamageable>() != null)
        {
            hitInfo.transform.GetComponent<IDamageable>().TakeDamage(_GunData.damage);
        }

        if(hitInfo.transform.tag == "Enemy")
        {
            hitInfo.transform.root.GetComponent<EnemyBody>().Kill();
            Instantiate(_EnemyImpactEffect, hitInfo.point, Quaternion.identity);
        }

        var rb = hitInfo.transform.GetComponent<Rigidbody>();
        if (rb == null) return;
        rb.velocity += _playerCamera.forward * _GunData.hitForce;
    }

    private IEnumerator ShootingCooldown() {
        _shooting = true;
        yield return new WaitForSeconds(1f / _GunData.shotsPerSecond);
        _shooting = false;
    }
    
    private IEnumerator ReloadCooldown() {
        _reloading = true;
        _ammoText.text = "RELOADING";
        SoundManager.instance.PlaySound(_ReloadSound);
        _rotationTime = 0f;
        yield return new WaitForSeconds(_GunData.reloadSpeed);
        if(_totalAmmo < _GunData.maxAmmo) _ammo = _totalAmmo;
        else _ammo = _GunData.maxAmmo;

        _ammoText.text = _ammo + " / " + _GunData.maxAmmo + "  TOTAL: " + _totalAmmo;
        _reloading = false;
    }

    public void Pickup(Transform weaponHolder, Transform playerCamera, TMP_Text ammoText) {
        if (_held) return;
        Destroy(_rb);
        SoundManager.instance.PlaySound(_PickUpSound);
        _time = 0f;
        transform.parent = weaponHolder;
        _startPosition = transform.localPosition;
        _startRotation = transform.localRotation;
        foreach (var col in GfxColliders) {
            col.enabled = false;
        }
        foreach (var gfx in WeaponGfxs) {
            gfx.layer = _weaponGfxLayer;
        }
        _held = true;
        _playerCamera = playerCamera;
        _ammoText = ammoText;
        _ammoText.text = _ammo + " / " + _GunData.maxAmmo + "  TOTAL: " + _totalAmmo;
        _scoping = false;
    }

    public void Drop(Transform playerCamera) {
        if (!_held) return;
        _rb = gameObject.AddComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.mass = 0.1f;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        var forward = playerCamera.forward;
        forward.y = 0f;
        _rb.velocity = forward * _GunData.throwForce;
        _rb.velocity += Vector3.up * _GunData.throwExtraForce;
        _rb.angularVelocity = Random.onUnitSphere * _GunData.rotationForce;
        foreach (var col in GfxColliders) {
            col.enabled = true;
        }
        foreach (var gfx in WeaponGfxs) {
            gfx.layer = 0;
        }
        _ammoText.text = "";
        transform.parent = null;
        _held = false;
    }

    public bool Scoping => _scoping;

}
