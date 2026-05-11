using System.Text.Json;
using WebShell.Config;

namespace WebShell
{
	internal static class Program
	{
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

				string json = JsonSerializer.Serialize(webApp);

				File.WriteAllText("config.json", json);
			}
			else
			{
				try
				{
					using Stream stream = File.OpenRead("config.json");
					webApp = JsonSerializer.Deserialize<LoadWebAppConfig>(stream);
				}
				catch (Exception e) when (e is
					FileNotFoundException or
					UnauthorizedAccessException or
					IOException
				)
				{
					MessageBox.Show(
						$"""
						Couldn't access the config.json file.
					
						Full Error:
						{e}
						""",
						"WebShell Error: Could not open file.",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error,
						MessageBoxDefaultButton.Button1
					);
				}
				catch (JsonException)
				{
					MessageBox.Show(
						"Could not parse config.json. The file is invalid.",
						"WebShell Error: Could not parse file.",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error,
						MessageBoxDefaultButton.Button1
					);
				}
			}
			
			if (webApp is null)
			{
				return;
			}

			Application.Run(new MainView(webApp));
		}
	}
}