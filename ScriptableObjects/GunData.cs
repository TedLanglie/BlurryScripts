using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Gun", menuName ="Weapon/Gun")]
public class GunData : ScriptableObject
{
    [Header("Info")]
    public new string name; // test

    [Header("Shooting")]
    public float damage;
    public int maxAmmo;
    public int totalAmmo;
    public int shotsPerSecond;
    public float reloadSpeed;
    public float hitForce;
    public float range;
    public bool tapable;
    public float kickbackForce;
    public float resetSmooth;
    [Header("Throwing")]
    public float throwForce;
    public float throwExtraForce;
    public float rotationForce;
    [Header("Pickup")]
    public float animTime;
}
