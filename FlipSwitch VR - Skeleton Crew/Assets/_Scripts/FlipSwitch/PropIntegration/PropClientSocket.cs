using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

#pragma warning disable 0219
public static class PropClientSocket
{
	private static ThreadStart delegate1002;
	private static ThreadStart delegate1003;
	private static ThreadStart delegate1004;
	private static ThreadStart delegate1005;
	private static WritePi pi1002;
	private static WritePi pi1003;
	private static WritePi pi1004;
	private static WritePi pi1005;

	public static void SetupSockets()
	{
		pi1002 = new WritePi("192.168.1.144");
		pi1003 = new WritePi("pi1003");
		pi1004 = new WritePi("192.168.1.140");
		pi1005 = new WritePi("pi1005");

		delegate1002 = new ThreadStart(pi1002.Run);
		delegate1003 = new ThreadStart(pi1003.Run);
		delegate1004 = new ThreadStart(pi1004.Run);
		delegate1005 = new ThreadStart(pi1005.Run);

		Thread thread1002 = new Thread(delegate1002);
		Thread thread1003 = new Thread(delegate1003);
		Thread thread1004 = new Thread(delegate1004);
		Thread thread1005 = new Thread(delegate1005);

		thread1002.Start();
		//thread1003.Start();
		thread1004.Start();
		//thread1005.Start();
	}


	private static ThreadStart delegateSpecific;
	private static WritePi piSpecific;

	public static void SetupSocketSpecfic(string ipOrID) {
		piSpecific = new WritePi( ipOrID );
		delegateSpecific = new ThreadStart( piSpecific.Run );
		Thread threadSpecific = new Thread( delegateSpecific );

		threadSpecific.Start();
	}

	public static void CloseSockets() {
		pi1002.CloseSocket();
		//pi1003.CloseSocket();
		pi1004.CloseSocket();
		//pi1005.CloseSocket();
	}
   
	public static void sendMessage(message msg)
	{
		switch (msg.piName)
		{
			case "pi1002":
				pi1002.eventCode = msg.msgCode;
				break;
			case "pi1003":
				pi1003.eventCode = msg.msgCode;
				break;
			case "pi1004":
				pi1004.eventCode = msg.msgCode;
				break;
			case "pi1005":
				pi1005.eventCode = msg.msgCode;
				break;
			case "Specific":
				piSpecific.eventCode = msg.msgCode;
				break;
		}
	}

} // end class ClientSocket

public class WritePi
{
	bool socketReady;                // global variables are setup here
	TcpClient mySocket;
	public NetworkStream theStream;
	StreamWriter theWriter;

	public int eventCode { get; set; }
	private string piName;
	private bool endThread = false;

	public WritePi (string piName)
	{
		this.piName = piName;
		eventCode = -1;
	}

	public void Run()
	{
		SetupSocket(piName, 50010);
		if (!socketReady) {
			return;
		}

		while (true)
		{
			if (endThread)
				break;
			if (eventCode != -1)
			{
				WriteSocket(eventCode);
				CloseSocket();
			}
		}
	}

	public void SetupSocket(string name, int port)
	{
		// Socket setup here
		try
		{
			mySocket = new TcpClient(name, port);
			theStream = mySocket.GetStream();
			theWriter = new StreamWriter(theStream);
			socketReady = true;
		}
		catch (Exception e)
		{
			Debug.Log( "error: " + e );

			Console.Write("Socket error:" + e); // catch any exceptions
			//CloseSocket();
		}
	}

	public void WriteSocket(int val)
	{
		// function to write data out
		if (!socketReady) return;
		theWriter.Write(val);
		theWriter.Flush();
	}

	public void CloseSocket()
	{                            // function to close the socket
		if (!socketReady)
			return;
		theWriter.Close();
		mySocket.Close();
		socketReady = false;
		endThread = true;
	}

	public void MaintainConnection()
	{
		if (!theStream.CanRead)
			SetupSocket(piName, 50010);
	}
}