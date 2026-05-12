using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebShell
{
	public partial class MainWindowViewModel : ObservableObject
	{
		[ObservableProperty]
		private string errorMessage;

		[ObservableProperty]
		private bool showError;
	}
}
