using System;
using System.Collections.Generic;
using System.Text;

namespace WebShell.Config
{
	public class ServerSettings
	{
		public ushort Port { get; set; } = 1234;
		public string Server { get; set; } = "localhost";
		public string Arguments { get; set; } = "";
	}
}
