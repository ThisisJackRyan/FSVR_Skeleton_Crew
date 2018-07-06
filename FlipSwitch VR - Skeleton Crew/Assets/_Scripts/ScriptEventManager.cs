using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.Networking;

public class ScriptEventManager : MonoBehaviour {

    private Dictionary<string, UnityEvent> eventDictionary;

    private static ScriptEventManager eventManager;

    public static ScriptEventManager instance
    {
        get
        {
            if(!eventManager)
            {
                eventManager = FindObjectOfType(typeof(ScriptEventManager)) as ScriptEventManager;

                if (!eventManager)
                    Debug.LogError("There needs to be one active ScriptEventManager on an object in your scene.");
                else
                {
                    // Initialize the event manager
                    eventManager.Init();
                }
            }

            return eventManager;
        }
    }

    private void Init()
    {
        if(eventDictionary == null)
        {
            eventDictionary = new Dictionary<string, UnityEvent>();
        }
    }

    public static void StartListening(string eventName, UnityAction listener)
    {
        UnityEvent thisEvent = null;

        if(instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.AddListener(listener);
        }
        else
        {
            thisEvent = new UnityEvent();
            thisEvent.AddListener(listener);
            instance.eventDictionary.Add(eventName, thisEvent);
        }
    }

    public static void StopListening(string eventName, UnityAction listener)
    {
        if (eventManager == null) return;

        UnityEvent thisEvent = null;
        if(instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.RemoveListener(listener);
        }
    }

    //[Command]
    public static void CmdTriggerEvent(string eventName)
    {
        UnityEvent thisEvent = null;
        if(instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.Invoke();
        }
    }
}
