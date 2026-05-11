using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebShell.Config;
using WebShell.Utilities;

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

		private readonly HttpClient httpClient = new();
		private readonly Process server;
		private readonly LoadWebAppConfig config;

		public MainWindow()
		{
			InitializeComponent();

			Closing += MainWindow_Closing;

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

			server = new()
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

			config = webApp;

			LoadPage();
		}

		private async void LoadPage()
		{
			Title = string.IsNullOrWhiteSpace(config.CustomTitle) ?
				"WebShell" :
				config.CustomTitle;

			webView.NavigationCompleted += WebView_NavigationCompleted;

			if (!Network.IsPortInUse(config.Server.Port))
			{
				LoadMissingPage(config.Server.Port);

				while (!Network.IsPortInUse(config.Server.Port))
				{
					await Task.Delay(250);
				}
			}

			webView.Source = new Uri($"localhost:{config.Server.Port}");
		}

		private async void LoadMissingPage(ushort port)
		{
			Assembly current = Assembly.GetExecutingAssembly();

			using Stream page = current.GetManifestResourceStream("WebShell.Resources.MissingPortPage.html");
			using StreamReader reader = new(page);

			string pageText = reader.ReadToEnd();
			string formatted = pageText.Replace("{PORT_NUMBER}", port.ToString());

			webView.NavigateToString(formatted);
		}

		private async void WebView_NavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
		{
			if (config.UseDocumentTitle)
			{
				Title = await webView.GetDocumentTitle();
			}

			Uri iconUrl = await webView.GetFaviconUri();

			using var client = new HttpClient();
			using Stream stream = await client.GetStreamAsync(iconUrl);

			Icon = new WindowIcon(stream);
		}

		private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
		{
			server.Kill();
		}
	}
}