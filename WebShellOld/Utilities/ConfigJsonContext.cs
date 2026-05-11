using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebShell.Config;

namespace WebShell.Utilities
{
	[JsonSerializable(typeof(LoadWebAppConfig))]
	public partial class ConfigJsonContext : JsonSerializerContext
	{
	}
}
