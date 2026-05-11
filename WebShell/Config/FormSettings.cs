using System;
using System.Collections.Generic;
using System.Text;

namespace WebShell.Config
{
	public class FormSettings
	{
		public string CustomTitle { get; set; } = "WebShell";
		public bool UseDocumentTitle { get; set; } = false;
		public bool StartMaximized { get; set; } = false;
	}
}
