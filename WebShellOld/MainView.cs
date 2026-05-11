using Microsoft.Web.WebView2.Core;
using System.Drawing.Drawing2D;
using System.Reflection;
using WebShell.Config;
using WebShell.Utilities;

namespace WebShell
{
	public partial class MainView : Form
	{
		public MainView(LoadWebAppConfig config)
		{
			InitializeComponent();

			Load += MainView_Load;
			FormClosed += MainView_FormClosed;

			InitializePage(config);
		}

		private async void InitializePage(LoadWebAppConfig config)
		{
			await webView21.EnsureCoreWebView2Async();

			webView21.CoreWebView2.FaviconChanged += FaviconChanged_Event;

			Text = string.IsNullOrWhiteSpace(config.CustomTitle) ?
				"WebShell" :
				config.CustomTitle;

			if (config.UseDocumentTitle)
			{
				webView21.CoreWebView2.DocumentTitleChanged += DocumentTitleChanged_Event;
			}

			if (!Network.IsPortInUse(config.Server.Port))
			{
				LoadMissingPage(config.Server.Port);

				while (!Network.IsPortInUse(config.Server.Port))
				{
					await Task.Delay(250);
				}
			}

			webView21.Source = new Uri($"localhost:{config.Server.Port}");
		}

		private async void FaviconChanged_Event(object? sender, object e)
		{			
			using Bitmap bitmap = new(await webView21.CoreWebView2.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png));

			Size scaledSize = bitmap.Size * 4;
			using Bitmap scaledBitmap = new(scaledSize.Width, scaledSize.Height);
			using var graphics = Graphics.FromImage(scaledBitmap);

			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;
			graphics.DrawImage(bitmap, new Rectangle(0, 0, scaledSize.Width, scaledSize.Height));

			Icon = Icon.FromHandle(scaledBitmap.GetHicon());
		}

		private async void LoadMissingPage(ushort port)
		{
			Assembly current = Assembly.GetExecutingAssembly();

			using Stream page = current.GetManifestResourceStream("WebShell.Resources.MissingPortPage.html");
			using StreamReader reader = new(page);

			string pageText = reader.ReadToEnd();
			string formatted = pageText.Replace("{PORT_NUMBER}", port.ToString());
			
			await webView21.EnsureCoreWebView2Async();

			webView21.NavigateToString(formatted);
		}

		private void DocumentTitleChanged_Event(object? sender, object e)
		{
			Text = webView21.CoreWebView2.DocumentTitle;
		}

		private void MainView_Load(object? sender, EventArgs e)
		{
			Location = Settings.Default.Location;
			Size = Settings.Default.Size;

			WindowState = Enum.Parse<FormWindowState>(Settings.Default.WindowState);
		}

		private void MainView_FormClosed(object? sender, FormClosedEventArgs e)
		{
			FormWindowState state = WindowState;

			if (state != FormWindowState.Maximized)
			{
				Settings.Default.Location = Location;
				Settings.Default.Size = Size;
			}

			if (state == FormWindowState.Minimized)
			{
				state = FormWindowState.Normal;
			}

			Settings.Default.WindowState = state.ToString();

			Settings.Default.Save();
		}
	}
}
