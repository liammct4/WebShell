using Avalonia;
using System;
using System.Collections.Generic;
using System.Text;
using WinPoint = System.Drawing.Point;
using WinSize = System.Drawing.Size;

namespace WebShell.Utilities
{
	public static class ConversionUtilities
	{
		extension(WinPoint point)
		{
			public PixelPoint AvaloniaPoint => new(point.X, point.Y);
		}

		extension(PixelPoint point)
		{
			public WinPoint WinPoint => new(point.X, point.Y);
		}

		extension(WinSize size)
		{
			public Size AvaloniaSize => new(size.Width, size.Height);
		}
	}
}
