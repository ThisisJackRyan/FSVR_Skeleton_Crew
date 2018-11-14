using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

#pragma warning disable 0219
public static class PropClientSocket {
	private static ThreadStart delegateWindController;
	private static ThreadStart delegateCannonControllerLeft;
	private static ThreadStart delegateCannonControllerRight;

	private static WritePi piWindController;
	private static WritePi piCannonControllerLeft;
	private static WritePi piCannonControllerRight;

	internal static void CloseSocket(PhysicalEffect effect) {
		switch (effect) {
			case PhysicalEffect.Wind:
				piWindController.CloseSocket();
				break;
			case PhysicalEffect.CannonLeft:
				piCannonControllerLeft.CloseSocket();
				break;
			case PhysicalEffect.CannonRight:
				piCannonControllerRight.CloseSocket();
				break;
		}
	}

	public static readonly string windController = "192.168.1.105";
	public static readonly string cannonControllerRight = "192.168.1.102";
	public static readonly string cannonControllerLeft = "192.168.1.104";

	public static void OpenSocket(PhysicalEffect effect) {
		switch (effect) {
			case PhysicalEffect.Wind:
				piWindController = new WritePi(windController);
				delegateWindController = new ThreadStart(piWindController.Run);
				Thread threadWindController = new Thread(delegateWindController);
				threadWindController.Start();
				break;
			case PhysicalEffect.CannonLeft:
				piCannonControllerLeft = new WritePi(cannonControllerLeft);
				delegateCannonControllerLeft = new ThreadStart(piCannonControllerLeft.Run);
				Thread threadCannonLeft = new Thread(delegateCannonControllerLeft);
				threadCannonLeft.Start();
				break;
			case PhysicalEffect.CannonRight:
				piCannonControllerRight = new WritePi(cannonControllerRight);
				delegateCannonControllerRight = new ThreadStart(piCannonControllerRight.Run);
				Thread threadCannonRight = new Thread(delegateCannonControllerRight);
				threadCannonRight.Start();
				break;
		}
	}


	private static ThreadStart delegateSpecific;
	private static WritePi piSpecific;

	public static void SetupSocketSpecfic(string ipOrID) {
		piSpecific = new WritePi(ipOrID);
		delegateSpecific = new ThreadStart(piSpecific.Run);
		Thread threadSpecific = new Thread(delegateSpecific);

		threadSpecific.Start();
	}

	public static void CloseSockets() {
		piWindController.CloseSocket();
		piCannonControllerLeft.CloseSocket();
		piCannonControllerRight.CloseSocket();
	}

	public static void SendMessage(Message msg) {
		switch (msg.effect) {
			case PhysicalEffect.Wind:
				piWindController.eventCode = msg.msgCode;
				break;
			case PhysicalEffect.CannonLeft:
				piCannonControllerLeft.eventCode = msg.msgCode;
				break;
			case PhysicalEffect.CannonRight:
				piCannonControllerRight.eventCode = msg.msgCode;
				break;
			case PhysicalEffect.SpecificController:
				piSpecific.eventCode = msg.msgCode;
				break;
		}
	}


} // end class ClientSocket

public enum Prop {
	WindOff,
	WindLow,
	WindMed,
	WindHigh,
	CannonLeftOne,
	CannonLeftTwo,
	CannonLeftThree,
	CannonRightOne,
	CannonRightTwo,
	CannonRightThree
}

public enum PhysicalEffect {
	Wind,
	CannonLeft,
	CannonRight,
	SpecificController
}

public struct Message {
	public int msgCode;
	public PhysicalEffect effect;

	public Message( PhysicalEffect effect, int code ) {
		msgCode = code;
		this.effect = effect;
	}
}

public class WritePi {
	bool socketReady;                // global variables are setup here
	TcpClient mySocket;
	public NetworkStream theStream;
	StreamWriter theWriter;

	public int eventCode { get; set; }
	private string piName;
	private bool endThread = false;

	public WritePi(string piName) {
		this.piName = piName;
		eventCode = -1;
	}

	public void Run() {
		SetupSocket(piName, 50010);
		if (!socketReady) {
			return;
		}

		while (true) {
			if (endThread)
				break;
			if (eventCode != -1) {
				WriteSocket(eventCode);
				CloseSocket();
			}
		}
	}

	public void SetupSocket(string name, int port) {
		// Socket setup here
		try {
			mySocket = new TcpClient(name, port);
			theStream = mySocket.GetStream();
			theWriter = new StreamWriter(theStream);
			socketReady = true;
		} catch (Exception e) {
			Debug.Log("error: " + e);

			Console.Write("Socket error:" + e); // catch any exceptions
												//CloseSocket();
		}
	}

	public void WriteSocket(int val) {
		// function to write data out
		if (!socketReady) return;
		theWriter.Write(val);
		theWriter.Flush();
	}

	public void CloseSocket() {                            // function to close the socket
		if (!socketReady)
			return;
		theWriter.Close();
		mySocket.Close();
		socketReady = false;
		endThread = true;
	}

	public void MaintainConnection() {
		if (!theStream.CanRead)
			SetupSocket(piName, 50010);
	}
}