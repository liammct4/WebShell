using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace WebShell.Config
{
	public class LoadWebAppConfig
	{
		public FormSettings Window { get; set; } = new();
		public ServerSettings Server { get; set; } = new();
	}
}
