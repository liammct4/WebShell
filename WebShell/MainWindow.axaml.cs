using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebShell.Config;
using WebShell.Utilities;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace WebShell
{
	public partial class MainWindow : Window
	{
		private static readonly JsonSerializerOptions options = new()
		{
			IndentSize = 4,
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.Never,
			TypeInfoResolver = ConfigJsonContext.Default
		};

		public MainWindow()
		{
			InitializeComponent();

			LoadWebAppConfig? webApp = null;

			if (!File.Exists("config.json"))
			{
				webApp = new LoadWebAppConfig();

				string json = JsonSerializer.Serialize(webApp, options);

				File.WriteAllText("config.json", json);
			}
			else
			{
				try
				{
					using Stream stream = File.OpenRead("config.json");
					webApp = JsonSerializer.Deserialize<LoadWebAppConfig>(stream, options);
				}
				catch (Exception e) when (e is
					FileNotFoundException or
					UnauthorizedAccessException or
					IOException
				)
				{
					MessageUtilities.ShowError(
						$"""
						Couldn't access the config.json file.
					
						Full Error:
						{e}
						""",
						"Could not open file."
					);
				}
				catch (JsonException)
				{
					MessageUtilities.ShowError(
						"Could not parse config.json. The file is invalid.",
						"Could not parse file."
					);
				}
			}

			if (webApp is null)
			{
				return;
			}

			return;
			// Start server.
			if (!File.Exists(webApp.Server.Path))
			{
				MessageUtilities.ShowError(
					$"Couldn't find server executable at: {webApp.Server.Path}",
					"Couldn't start server."
				);
				return;
			}

			Process server = new()
			{
				StartInfo =
				{
					FileName = webApp.Server.Path,
					Arguments = webApp.Server.Arguments,
					CreateNoWindow = true
				}
			};

#if DEBUG
			server.StartInfo.CreateNoWindow = false;
#endif

			if (!server.Start())
			{
				MessageUtilities.ShowError(
					"Unable to start the server due to unknown reasons.",
					"Couldn't start server."
				);
				return;
			}

			// Initial pre waiting, wait for a second for the server
			// to be ready before launching, then start with an empty
			// view.

			using var client = new TcpClient();

			try
			{
				client.Connect("127.0.0.1", webApp.Server.Port);
				client.Close();
			}
			catch (SocketException) // Timed out.
			{

			}

			server.Kill();
		}
	}
}