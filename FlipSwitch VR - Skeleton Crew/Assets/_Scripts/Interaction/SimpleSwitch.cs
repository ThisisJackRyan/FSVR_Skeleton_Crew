using UnityEngine;

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.net
/// 
/// Description: SimpleSwitch is a class meant to be a buiding block for any kind of on/off switch type object.
/// </summary>
public abstract class SimpleSwitch : MonoBehaviour {
	#region Fields

	[Header("Interactivity")]
	[Tooltip("Whether or not the switch is active.")]
	public bool isActive;
	[Tooltip("The message to be displayed when switch can be activated")]
	public string activateMessage;
	[Tooltip("The message to be displayed when switch can be de-activated")]
	public string deactivateMessage;
	
	//todo these are just here in case we decide to use them later on
	//public UnityEvent activateEvent;
	//public UnityEvent deactivateEvent;

	//this is set by toggle by default.
	protected GameObject activator;

	internal string Tooltip {
		get { return isActive ? deactivateMessage : activateMessage; }
	}

	#endregion

	void OnIsActiveChange(bool n) {
		isActive = n;
	}

	/// <summary>
	/// Toggles this Switch and calls the appropriate method. 
	/// </summary>
	/// <param name="activatingObject">The object activating this switch. if overriding toggle, and the activator please set this within your method.</param>
	public virtual void Toggle(GameObject activatingObject) {
		this.activator = activatingObject;
		isActive = !isActive;
		if (isActive) {
			OnActivate();
		} else {
			OnDeactivate();
		}
	}

	/// <summary>
	/// Called when [isActive].
	/// </summary>
	public abstract void OnActivate();

	/// <summary>
	/// Called when ![isActive].
	/// </summary>
	public abstract void OnDeactivate();
}