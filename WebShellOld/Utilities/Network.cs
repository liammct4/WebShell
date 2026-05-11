using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace WebShell.Utilities
{
	public static class Network
	{
		public static bool IsPortInUse(int port)
		{
			IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
			IPEndPoint[] endpoints = ipProperties.GetActiveTcpListeners();
			return endpoints.Any(e => e.Port == port);
		}
	}
}
