using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebShell.Config;
using WebShell.Utilities;

namespace WebShell
{
	internal static class Program
	{
		private static readonly JsonSerializerOptions options = new()
		{
			IndentSize = 4,
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.Never
		};

		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// To customize application configuration such as set high DPI settings or default font,
			// see https://aka.ms/applicationconfiguration.
			ApplicationConfiguration.Initialize();

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

			// TODO: Add timeout.
			using var client = new TcpClient();
			client.Connect("127.0.0.1", webApp.Server.Port);
			client.Close();

			Application.Run(new MainView(webApp));

			server.Kill();
		}
	}
}