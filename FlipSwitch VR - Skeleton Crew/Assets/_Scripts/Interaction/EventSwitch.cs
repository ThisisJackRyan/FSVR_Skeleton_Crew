using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.net
/// 
/// Description: EventSwitch
/// </summary>
public class EventSwitch : SimpleSwitch {
	#region Fields

	public UnityEvent activateEvent, deactivateEvent;

	#endregion
	public override void OnActivate() {
		print("called");
		activateEvent.Invoke();
	}
	public override void OnDeactivate() {
		deactivateEvent.Invoke();
	}
}