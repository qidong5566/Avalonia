﻿using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

namespace Avalonia.MonoMac
{
    class WindowBaseImpl : TopLevelImpl, IWindowBaseImpl
    {
        public CustomWindow Window { get; private set; }

		public WindowBaseImpl()
		{
            Window = new CustomWindow(this);
            Window.StyleMask = NSWindowStyle.Titled;
            Window.BackingType = NSBackingStore.Buffered;
            Window.ContentView = View;
            Window.Delegate = CreateWindowDelegate();
		}

        public class CustomWindow : NSWindow
        {
            readonly WindowBaseImpl _impl;

            public CustomWindow(WindowBaseImpl impl)
            {
                _impl = impl;
            }

            public override void BecomeKeyWindow()
            {
                _impl.Activated?.Invoke();
                base.BecomeKeyWindow();
            }

            public override void ResignKeyWindow()
            {
                _impl.Deactivated?.Invoke();
                base.ResignKeyWindow();
            }
        }

        protected virtual NSWindowDelegate CreateWindowDelegate() => new WindowBaseDelegate(this);

        public class WindowBaseDelegate : NSWindowDelegate
        {
            readonly WindowBaseImpl _impl;

            public WindowBaseDelegate(WindowBaseImpl impl)
            {
                _impl = impl;
            }

            public override void DidMoved(global::MonoMac.Foundation.NSNotification notification)
            {
                _impl.PositionChanged?.Invoke(_impl.Position);
            }

            public override void WillClose(global::MonoMac.Foundation.NSNotification notification)
            {
                _impl.Window.Dispose();
                _impl.Window = null;
                _impl.Dispose();
            }
        }


        public Point Position
        {
            get
            {
                var pos = Window.Frame.ToAvaloniaRect().BottomLeft.ConvertPointY();
                //Console.WriteLine($"GET pos {pos}");
                return pos;
            }
            set
            {
                //Console.WriteLine($"SET pos {value}");
                Window.CascadeTopLeftFromPoint(value.ToMonoMacPoint().ConvertPointY());
            }
        }


        protected virtual NSWindowStyle GetStyle()
        {
            return NSWindowStyle.Borderless;
        }

        protected void UpdateStyle()
        {
            Window.StyleMask = GetStyle();
        }


        IPlatformHandle IWindowBaseImpl.Handle => new PlatformHandle(Window.Handle, "NSWindow");
        public Size MaxClientSize => NSScreen.Screens[0].Frame.ToAvaloniaRect().Size;
		public Action<Point> PositionChanged { get; set; }
		public Action Deactivated { get; set; }
		public Action Activated { get; set; }

		public override Size ClientSize => Window.ContentRectFor(Window.Frame).Size.ToAvaloniaSize();


		public void Show()
        {
            Window.MakeKeyAndOrderFront(Window);
        }

        public void Hide()
        {
            Window?.OrderOut(Window);
        }


        public void BeginMoveDrag()
        {
            var ev = View.LastMouseDownEvent;
            if (ev == null)
                return;
            var handle = Selector.GetHandle("performWindowDragWithEvent:");
            Messaging.void_objc_msgSend_IntPtr(Window.Handle, handle, ev.Handle);
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
            //TODO: Intercept mouse events and implement resize drag manually
        }

        public void Activate()
        {
            Window.MakeKeyWindow();
        }

        public void Resize(Size clientSize)
        {
            var pos = Position;
            Window.SetContentSize(clientSize.ToMonoMacSize());
            Position = pos;
        }

        public override Point PointToClient(Point point)
        {
            var cocoaScreenPoint = point.ToMonoMacPoint().ConvertPointY();
            var cocoaViewPoint = Window.ConvertScreenToBase(cocoaScreenPoint).ToAvaloniaPoint();
            return View.TranslateLocalPoint(cocoaViewPoint);
        }

        public override Point PointToScreen(Point point)
        {
            var cocoaViewPoint = View.TranslateLocalPoint(point).ToMonoMacPoint();
            var cocoaScreenPoint = Window.ConvertBaseToScreen(cocoaViewPoint);
            return cocoaScreenPoint.ConvertPointY().ToAvaloniaPoint();
        }



        public override void Dispose()
        {
            Window?.Close();
            Window?.Dispose();
			base.Dispose();
        }
    }
}
