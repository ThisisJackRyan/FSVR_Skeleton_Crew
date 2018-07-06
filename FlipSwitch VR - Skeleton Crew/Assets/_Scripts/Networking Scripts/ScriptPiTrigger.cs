using UnityEngine;
using UnityEngine.Networking;

/*
 * Author: Nathan Boehning
 */

public enum PhysicalEffect
{
	Lift,
	MakeNoise,
	Decon,
	Flash
}

public struct message
{
	public int msgCode;
	public string piName;

	public message(int code, string name)
	{
		msgCode = code;
		piName = name;
	}

}

public class MessageCollection
{
	public static message Lift = new message(1001, "pi1002");
	public static message MakeNoise = new message(1001, "pi1003");
	public static message Decon = new message(1001, "pi1004");
	public static message Flash = new message(1001, "pi1005");
}

public class ScriptPiTrigger : NetworkBehaviour
{
    private static ScriptPiTrigger instance = null;

    public static ScriptPiTrigger Get()
    {
        if(instance == null)
        {
            GameObject piInterface = new GameObject();
            instance = piInterface.AddComponent<ScriptPiTrigger>();
            piInterface.name = "Pi Interface";
        }

        return instance;
    }

    [Command]
	public void CmdTriggerPi(PhysicalEffect effect)
	{
		switch (effect)
		{
			case PhysicalEffect.Lift:
                    ScriptClientSocket.sendMessage(MessageCollection.Lift);
				    break;
			case PhysicalEffect.Decon:
                    ScriptClientSocket.sendMessage(MessageCollection.Decon);
				    break;
			case PhysicalEffect.Flash:
				    ScriptClientSocket.sendMessage(MessageCollection.Flash);
				    break;
			case PhysicalEffect.MakeNoise:
				    ScriptClientSocket.sendMessage(MessageCollection.MakeNoise);
				    break;
		}
	}
}
