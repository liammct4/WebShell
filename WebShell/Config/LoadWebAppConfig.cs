using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace WebShell.Config
{
	public class LoadWebAppConfig
	{
		public string CustomTitle { get; set; } = "WebShell";
		public bool UseDocumentTitle { get; set; } = false;
		public ServerSettings Server { get; set; } = new();
	}
}
