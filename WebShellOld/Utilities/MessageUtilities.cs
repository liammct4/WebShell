using System;
using System.Collections.Generic;
using System.Text;

namespace WebShell.Utilities
{
	public class MessageUtilities
	{
		public static void ShowError(string title, string message)
		{
			MessageBox.Show(
				message,
				$"WebShell: {title}",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error,
				MessageBoxDefaultButton.Button1
			);
		}
	}
}
