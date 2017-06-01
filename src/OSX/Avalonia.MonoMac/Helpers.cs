﻿﻿using System;
using System.Runtime.InteropServices;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.CoreGraphics;
using MonoMac;
namespace Avalonia.MonoMac
{
    static class Helpers
    {
        public static Point ToAvaloniaPoint(this CGPoint point) => new Point(point.X, point.Y);
        public static CGPoint ToMonoMacPoint(this Point point) => new CGPoint(point.X, point.Y);
        public static Size ToAvaloniaSize(this CGSize size) => new Size(size.Width, size.Height);
        public static CGSize ToMonoMacSize(this Size size) => new CGSize(size.Width, size.Height);
        public static Rect ToAvaloniaRect(this CGRect rect) => new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
        public static CGRect ToMonoMacRect(this Rect rect) => new CGRect(rect.X, rect.Y, rect.Width, rect.Height);

		public static Point ConvertPointY(this Point pt)
		{
			var sw = NSScreen.Screens[0].Frame;
			var t = Math.Max(sw.Top, sw.Bottom);
			return pt.WithY(t - pt.Y);
		}

		public static CGPoint ConvertPointY(this CGPoint pt)
		{
			var sw = NSScreen.Screens[0].Frame;
			var t = Math.Max(sw.Top, sw.Bottom);
            return new CGPoint(pt.X, t - pt.Y);
		}

    }
}
