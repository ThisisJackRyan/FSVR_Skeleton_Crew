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

	public static message turnOn = new message( 1001, "pi1005" );
	public static message turnOff = new message( 1002, "pi1005" );

}

public class PropPiTrigger : NetworkBehaviour
{
	private static PropPiTrigger instance = null;

	public static PropPiTrigger Get()
	{
		if(instance == null)
		{
			GameObject piInterface = new GameObject();
			instance = piInterface.AddComponent<PropPiTrigger>();
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
					PropClientSocket.sendMessage(MessageCollection.Lift);
					break;
			case PhysicalEffect.Decon:
					PropClientSocket.sendMessage(MessageCollection.Decon);
					break;
			case PhysicalEffect.Flash:
					PropClientSocket.sendMessage(MessageCollection.Flash);
					break;
			case PhysicalEffect.MakeNoise:
					PropClientSocket.sendMessage(MessageCollection.MakeNoise);
					break;
		}
	}
}
