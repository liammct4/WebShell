using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebShell.Utilities
{
	public static class WebViewExtension
	{
		extension(NativeWebView webView)
		{
			public async Task<string> GetDocumentTitle()
			{
				string result = await webView.InvokeScript("document.title");
				return result[1..^1];
			}

			public async Task<Uri?> GetFaviconUri()
			{
				string? rawUri = await webView.InvokeScript("document.querySelector(\"link[rel='icon']\").href;");

				if (rawUri is null)
				{
					return null;
				}

				string replaced = rawUri.Replace("\"", "");

				return new Uri(replaced);
			}
		}
	}
}
