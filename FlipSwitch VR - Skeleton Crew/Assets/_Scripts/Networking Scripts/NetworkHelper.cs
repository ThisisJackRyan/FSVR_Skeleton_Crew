using System.Net;
using System.Net.Sockets;
using System;

public static class NetworkHelper {

	public static string[] playerIpAddresses = new string[] { "192.168.1.184", "192.168.1.102", "192.168.1.103", "192.168.1.104" };
	public static string hostIpAddress = "192.168.1.172";

	public static string GetLocalIPAddress() {
		var host = Dns.GetHostEntry( Dns.GetHostName() );
		foreach ( var ip in host.AddressList ) {
			if ( ip.AddressFamily == AddressFamily.InterNetwork ) {
				return ip.ToString();
			}
		}
		throw new Exception( "Local IP Address not Found!" );
	}
}
