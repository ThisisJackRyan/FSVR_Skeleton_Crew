﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AISeekTarget : MonoBehaviour {
	NavMeshAgent agent;
	public GameObject target;


// Use this for initialization
	void Start () {
		agent = GetComponent<NavMeshAgent>();
	}

	// Update is called once per frame
	void Update () {
		agent.destination = target.transform.position;

	}
}