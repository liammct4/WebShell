using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebShell.Utilities
{
	public class MessageUtilities
	{
		public static void ShowError(string title, string message)
		{
			var error = MessageBoxManager.GetMessageBoxStandard(
				$"WebShell: {title}",
				message,
				ButtonEnum.Ok,
				Icon.Error
			);

			error.ShowAsync();
		}
	}
}
