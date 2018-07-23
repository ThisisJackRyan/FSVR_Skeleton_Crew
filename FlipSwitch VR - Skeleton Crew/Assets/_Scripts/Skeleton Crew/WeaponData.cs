using UnityEngine;
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

	//gun and bow only
	public int ammo;
	public GameObject projectile;
	public GameObject particles;
	public float power = 10;

	public Vector3 heldPosition, holsteredPosition;
	public Quaternion heldRotation, holsteredRotation;

	public AudioClip firesound, outOfAmmoSound;

	#endregion

	public enum WeaponType {
		Melee, Gun, Bow, Punt
	}
	
	void Start() {
		
	}

	void Update() {
	}

	

}