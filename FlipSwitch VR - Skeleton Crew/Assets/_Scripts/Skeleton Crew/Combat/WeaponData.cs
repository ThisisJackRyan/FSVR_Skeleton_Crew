﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.net
/// 
/// Description: WeaponData
/// </summary>
[CreateAssetMenu]
public class WeaponData : ScriptableObject {
	#region Fields

	public int damage;
	public WeaponType type;
	public GripType gripType;

	//gun and bow only
	public int ammo;
	public GameObject projectile;
	public GameObject particles;
	public float power = 10;
    public float spread = 0.01f;
	public float timeBetweenShots = 1.5f;

	public Vector3 heldPositionRight, heldPositionLeft, holsteredPosition;
	public Quaternion heldRotationRight, heldRotationLeft, holsteredRotation;

	public AudioClip firesound, outOfAmmoSound, reloadClip;

	public HapticEvent hapticsFiring,  hapticsOutOfAmmo;

	#endregion

	public enum WeaponType {
		Melee, Gun, Bow, Punt
	}

	public enum GripType {
		Pistol, Sword, Musket, AxeKnife
	}
	
}