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

			if (!Network.IsPortInUse(config.Server.Port))
			{
				LoadMissingPage(config.Server.Port);
				return;
			}

			webView21.Source = new Uri($"localhost:{config.Server.Port}");
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
	}
}
