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

			Text = string.IsNullOrWhiteSpace(config.CustomTitle) ?
				"WebShell" :
				config.CustomTitle;

			if (config.UseDocumentTitle)
			{
				webView21.NavigationCompleted += WebViewInitalized_Event;
			}

			if (!Network.IsPortInUse(config.Server.Port))
			{
				LoadMissingPage(config.Server.Port);
				return;
			}

			webView21.Source = new Uri($"localhost:{config.Server.Port}");
		}

		private void WebViewInitalized_Event(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
		{
			webView21.CoreWebView2.DocumentTitleChanged += DocumentTitleChanged_Event;
			Text = webView21.CoreWebView2.DocumentTitle;
		}

		private async void LoadMissingPage(ushort port)
		{
			Assembly current = Assembly.GetExecutingAssembly();

			using Stream page = current.GetManifestResourceStream("WebShell.MissingPortPage.html");
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
