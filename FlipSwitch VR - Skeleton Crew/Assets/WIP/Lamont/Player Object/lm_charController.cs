using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lm_charController : MonoBehaviour {
	
	public float speed = 10.0F;
	
	// Use this for initialization
	void Start () {
			Cursor.lockState = CursorLockMode.Locked;
	}
	
	// Update is called once per frame
	void Update () {
			if (Input.GetKeyDown("escape"))
				Cursor.lockState = CursorLockMode.None;
			if (Input.GetKeyDown(KeyCode.Mouse0))
				Cursor.lockState = CursorLockMode.Locked;
				
				
			if (Cursor.lockState == CursorLockMode.Locked){
				float translation = Input.GetAxis("Vertical") * speed;
				float straffe = Input.GetAxis("Horizontal") * speed;
				translation *= Time.deltaTime;
				straffe *= Time.deltaTime;
				
				transform.Translate(straffe, 0, translation);
			}
		
		}
}
