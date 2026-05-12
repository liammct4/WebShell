using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Policy;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebShell.Config;
using WebShell.Utilities;
using WinBitmap = System.Drawing.Bitmap;
using WinIcon = System.Drawing.Icon;

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

		private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

		private readonly HttpClientHandler http = new()
		{
			UseProxy = false
		};
		private readonly HttpClient httpClient;
		private readonly Process server;
		private readonly LoadWebAppConfig config;

		public MainWindow()
		{
			httpClient = new(http);

			InitializeComponent();

			Closing += MainWindow_Closing;

			if (!Settings.Default.FirstBoot)
			{
				Position = Settings.Default.Location.AvaloniaPoint;
				Width = Settings.Default.Size.Width;
				Height = Settings.Default.Size.Height;

				WindowState = Enum.Parse<WindowState>(Settings.Default.WindowState);
			}

			if (File.Exists("webapp.ico"))
			{
				Icon = new WindowIcon("webapp.ico");
			}

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
					DisplayError(
						$"""
						Couldn't access the config.json file due to the
						file not existing, or being unauthorized.
					
						Full Error:
						{e}
						"""
					);
				}
				catch (JsonException)
				{
					DisplayError(
						"Could not parse config.json. The file is invalid."
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
				DisplayError(
					$"Could not find server executable at: \"{webApp.Server.Path}\""
				);
				return;
			}

			server = new()
			{
				StartInfo =
				{
					FileName = webApp.Server.Path,
					Arguments = webApp.Server.Arguments,
#if !DEBUG
					CreateNoWindow = true
#endif
				}
			};

			if (!server.Start())
			{
				DisplayError("Unable to start the server due to unknown reasons.");
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

		private void DisplayError(string message)
		{
			ViewModel.ErrorMessage = message;
			ViewModel.ShowError = true;
			TransparencyLevelHint = [WindowTransparencyLevel.Mica];
			Background = new SolidColorBrush(Colors.Transparent);
		}

		private void RemoveError()
		{
			ViewModel.ErrorMessage = "";
			ViewModel.ShowError = false;
			TransparencyLevelHint = [WindowTransparencyLevel.None];
			Background = new SolidColorBrush(Colors.Black);
		}

		private async void LoadPage()
		{
			Title = string.IsNullOrWhiteSpace(config.CustomTitle) ?
				"WebShell" :
				config.CustomTitle;

			webView.NavigationCompleted += WebView_NavigationCompleted;

			if (!Network.IsPortInUse(config.Server.Port))
			{
				DisplayError(
					$"""
					No server could be found on port {config.Server.Port}.
					Check that you have specified the correct port in the server options in config.json.
					Also check that you have given the correct arguments to the server.
					"""
				);
				
				while (!Network.IsPortInUse(config.Server.Port))
				{
					await Task.Delay(250);
				}
			}

			RemoveError();

			webView.Source = new Uri($"localhost:{config.Server.Port}");
		}

		private async void WebView_NavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
		{
			if (config.UseDocumentTitle)
			{
				Title = await webView.GetDocumentTitle();
			}

			Uri iconUrl = await webView.GetFaviconUri();
			iconUrl = new Uri(iconUrl.ToString().Replace("localhost", "127.0.0.1"));

			using Stream downloadStream = await httpClient.GetStreamAsync(iconUrl);
			using MemoryStream sourceStream = new();

			downloadStream.CopyTo(sourceStream);
			sourceStream.Seek(0, SeekOrigin.Begin);

			WinBitmap bitmap = new(sourceStream);
			WinIcon convertedIcon = WinIcon.FromHandle(bitmap.GetHicon());

			using Stream fs = File.OpenWrite("webapp.ico");
			convertedIcon.Save(fs);

			sourceStream.Seek(0, SeekOrigin.Begin);
			Icon = new WindowIcon(sourceStream);
		}

		private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
		{
			WindowState state = WindowState;

			Settings.Default.FirstBoot = false;

			if (state != WindowState.Maximized)
			{
				Settings.Default.Location = Position.WinPoint;
				Settings.Default.Size = new((int)Width, (int)Height);
			}

			if (state == WindowState.Minimized)
			{
				state = WindowState.Normal;
			}

			Settings.Default.WindowState = state.ToString();

			Settings.Default.Save();

			if (server is null)
			{
				return;
			}

			server.Kill();

#if !DEBUG
			Process changeIconProcess = new()
			{
				StartInfo =
				{
					FileName = "IconChanger.exe",
					CreateNoWindow = true
				}
			};

			changeIconProcess.Start();
#endif
		}
	}
}