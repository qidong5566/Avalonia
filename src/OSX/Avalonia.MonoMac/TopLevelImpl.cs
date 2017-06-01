﻿using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Controls.Platform.Surfaces;
using MonoMac.AppKit;

using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;

namespace Avalonia.MonoMac
{
    abstract class TopLevelImpl : ITopLevelImpl, IFramebufferPlatformSurface
    {
        public TopLevelView View { get; }
        public TopLevelImpl()
        {
            View = new TopLevelView(this);
        }

        [Adopts("NSTextInputClient")]
        public class TopLevelView : NSView
        {
            TopLevelImpl _tl;
            bool _isLeftPressed, _isRightPressed, _isMiddlePressed;
            private readonly IMouseDevice _mouse;
            private readonly IKeyboardDevice _keyboard;
            private NSTrackingArea _area;
            private NSCursor _cursor;
            public TopLevelView(TopLevelImpl tl)
            {
                _tl = tl;
                _mouse = AvaloniaLocator.Current.GetService<IMouseDevice>();
                _keyboard = AvaloniaLocator.Current.GetService<IKeyboardDevice>();
			}

            public override bool ConformsToProtocol(IntPtr protocol)
            {
                var rv = base.ConformsToProtocol(protocol);
                return rv;
            }

            public override void DrawRect(CGRect dirtyRect)
            {
                _tl.Paint?.Invoke(dirtyRect.ToAvaloniaRect());
            }

            [Export("viewDidChangeBackingProperties:")]
            public void ViewDidChangeBackingProperties()
            {
                _tl?.ScalingChanged?.Invoke(_tl.Scaling);
            }

            void UpdateCursor()
            {
                ResetCursorRects();
                if (_cursor != null)
                    AddCursorRect(Frame, _cursor);
            }

            static NSCursor ArrowCursor = NSCursor.ArrowCursor;
            public void SetCursor(NSCursor cursor)
            {
                _cursor = cursor ?? ArrowCursor;
                UpdateCursor();
            }

            public override void SetFrameSize(CGSize newSize)
            {
                base.SetFrameSize(newSize);

                if (_area != null)
                {
                    RemoveTrackingArea(_area);
                    _area.Dispose(); ;
                }
                _area = new NSTrackingArea(new CGRect(default(CGPoint), newSize),
                                           NSTrackingAreaOptions.ActiveAlways |
                                           NSTrackingAreaOptions.MouseMoved |
                                           NSTrackingAreaOptions.EnabledDuringMouseDrag, this, null);
                AddTrackingArea(_area);
                UpdateCursor();
                _tl?.Resized?.Invoke(_tl.ClientSize);
            }

            InputModifiers GetModifiers(NSEventModifierMask mod)
            {
                var rv = new InputModifiers();
                if (mod.HasFlag(NSEventModifierMask.ControlKeyMask))
                    rv |= InputModifiers.Control;
                if (mod.HasFlag(NSEventModifierMask.ShiftKeyMask))
                    rv |= InputModifiers.Shift;
                if (mod.HasFlag(NSEventModifierMask.AlternateKeyMask))
                    rv |= InputModifiers.Alt;
                if (mod.HasFlag(NSEventModifierMask.CommandKeyMask))
                    rv |= InputModifiers.Windows;

                if (_isLeftPressed)
                    rv |= InputModifiers.LeftMouseButton;
                if (_isMiddlePressed)
                    rv |= InputModifiers.MiddleMouseButton;
                if (_isRightPressed)
                    rv |= InputModifiers.RightMouseButton;
                return rv;
            }

            public Point TranslateLocalPoint(Point pt) => pt.WithY(Bounds.Height - pt.Y);

            Vector GetDelta(NSEvent ev)
            {
                var rv = new Vector(ev.ScrollingDeltaX, ev.ScrollingDeltaY);
                //TODO: Verify if handling of HasPreciseScrollingDeltas
                // is required (touchpad or magic-mouse is needed)
                return rv;
            }

            uint GetTimeStamp(NSEvent ev) => (uint)(ev.Timestamp * 1000);

            void MouseEvent(NSEvent ev, RawMouseEventType type)
            {
                BecomeFirstResponder();
                var loc = TranslateLocalPoint(ConvertPointToView(ev.LocationInWindow, this).ToAvaloniaPoint());
                var ts = GetTimeStamp(ev);
                var mod = GetModifiers(ev.ModifierFlags);
                if (type == RawMouseEventType.Wheel)
                {
                    var delta = GetDelta(ev);
                    if (delta.X == 0 && delta.Y == 0)
                        return;
                    _tl.Input?.Invoke(new RawMouseWheelEventArgs(_mouse, ts, _tl.InputRoot, loc,
                                                                 delta, mod));
                }
                else
                    _tl.Input?.Invoke(new RawMouseEventArgs(_mouse, ts, _tl.InputRoot, type, loc, mod));
            }

            public override void MouseMoved(NSEvent theEvent)
            {
                MouseEvent(theEvent, RawMouseEventType.Move);
                base.MouseMoved(theEvent);
            }

            public override void MouseDragged(NSEvent theEvent)
            {
                MouseEvent(theEvent, RawMouseEventType.Move);
                base.MouseDragged(theEvent);
            }

            public override void OtherMouseDragged(NSEvent theEvent)
            {
                MouseEvent(theEvent, RawMouseEventType.Move);
                base.OtherMouseDragged(theEvent);
            }

            public override void RightMouseDragged(NSEvent theEvent)
            {
                MouseEvent(theEvent, RawMouseEventType.Move);
                base.RightMouseDragged(theEvent);
            }

            public NSEvent LastMouseDownEvent { get; private set; }

            public override void MouseDown(NSEvent theEvent)
            {
                _isLeftPressed = true;
                LastMouseDownEvent = theEvent;
                MouseEvent(theEvent, RawMouseEventType.LeftButtonDown);
                LastMouseDownEvent = null;
                base.MouseDown(theEvent);
            }

            public override void RightMouseDown(NSEvent theEvent)
            {
                _isRightPressed = true;
                MouseEvent(theEvent, RawMouseEventType.RightButtonDown);
                base.RightMouseDown(theEvent);
            }

            public override void OtherMouseDown(NSEvent theEvent)
            {
                _isMiddlePressed = true;
                MouseEvent(theEvent, RawMouseEventType.MiddleButtonDown);
                base.OtherMouseDown(theEvent);
            }

            public override void MouseUp(NSEvent theEvent)
            {
                _isLeftPressed = false;
                MouseEvent(theEvent, RawMouseEventType.LeftButtonUp);
                base.MouseUp(theEvent);
            }

            public override void RightMouseUp(NSEvent theEvent)
            {
                _isRightPressed = false;
                MouseEvent(theEvent, RawMouseEventType.RightButtonUp);
                base.RightMouseUp(theEvent);
            }

            public override void OtherMouseUp(NSEvent theEvent)
            {
                _isMiddlePressed = false;
                MouseEvent(theEvent, RawMouseEventType.MiddleButtonUp);
                base.OtherMouseUp(theEvent);
            }

            public override void ScrollWheel(NSEvent theEvent)
            {
                MouseEvent(theEvent, RawMouseEventType.Wheel);
                base.ScrollWheel(theEvent);
            }

            public override void MouseExited(NSEvent theEvent)
            {
                MouseEvent(theEvent, RawMouseEventType.LeaveWindow);
                base.MouseExited(theEvent);
            }

            void KeyboardEvent(RawKeyEventType type, NSEvent ev)
            {
                var code = KeyTransform.TransformKeyCode(ev.KeyCode);
                if (!code.HasValue)
                    return;
                _tl.Input?.Invoke(new RawKeyEventArgs(_keyboard, GetTimeStamp(ev),
                     type, code.Value, GetModifiers(ev.ModifierFlags)));
            }

            public override void KeyDown(NSEvent theEvent)
            {
                KeyboardEvent(RawKeyEventType.KeyDown, theEvent);
                InputContext.HandleEvent(theEvent);
                base.KeyDown(theEvent);
            }

            public override void KeyUp(NSEvent theEvent)
            {
                KeyboardEvent(RawKeyEventType.KeyUp, theEvent);
                base.KeyUp(theEvent);
            }



            #region NSTextInputClient

            public override bool AcceptsFirstResponder() => true;

            public bool HasMarkedText 
            {
                [Export("hasMarkedText")]
                get { return false; } 
            }

			public NSRange MarkedRange
			{
				[Export("markedRange")]
				get { return new NSRange(NSRange.NotFound, 0); }
			}

            public NSRange SelectedRange
			{
				[Export("selectedRange")]
                get { return new NSRange(NSRange.NotFound, 0); }
			}

            [Export("setMarkedText:selectedRange:replacementRange:")]
            public void SetMarkedText(NSString str, NSRange a1, NSRange a2)
            {
                
            }

            [Export("unmarkText")]
            public void UnmarkText()
            {
                
            }

            public NSArray ValidAttributesForMarkedText
            {
                [Export("validAttributesForMarkedText")]
                get
                {
                    return new NSArray();
                }
            }

            [Export("attributedSubstringForProposedRange:actualRange:")]
            public NSAttributedString AttributedSubstringForProposedRange(NSRange range, IntPtr wat)
            {
                return new NSAttributedString("");
            }

            [Export("insertText:replacementRange:")]
            public void InsertText(NSString str, NSRange range)
            {
                //TODO: timestamp
                _tl.Input?.Invoke(new RawTextInputEventArgs(_keyboard, 0, str.ToString()));
            }

            [Export("characterIndexForPoint:")]
            public uint CharacterIndexForPoint(CGPoint pt)
            {
                return 0;
            }

            [Export("firstRectForCharacterRange:actualRange:")]
            public CGRect FirstRectForCharacterRange(NSRange range, IntPtr wat)
            {
                return new CGRect();
            }

			#endregion
		}

        public IInputRoot InputRoot { get; private set; }

        public abstract Size ClientSize { get; }

        public double Scaling 
        {
            get
            {
                if (View.Window == null)
                    return 1;
                return View.Window.BackingScaleFactor;
            }
        }

        public IEnumerable<object> Surfaces => new[] { this };

		#region Events
		public Action<RawInputEventArgs> Input { get; set; }
		public Action<Rect> Paint { get; set; }
		public Action<Size> Resized { get; set; }
		public Action<double> ScalingChanged { get; set; }
		public Action Closed { get; set; }
		#endregion

		public virtual void Dispose()
        {
            Closed?.Invoke();
            Closed = null;
            View.Dispose();
        }

        public void Invalidate(Rect rect)
        {
            View.SetNeedsDisplayInRect(View.Frame);
        }

        public abstract Point PointToClient(Point point);

        public abstract Point PointToScreen(Point point);

        public void SetCursor(IPlatformHandle cursor)
        {
            View.SetCursor((cursor as Cursor)?.Native);
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public ILockedFramebuffer Lock() => new EmulatedFramebuffer(View);
    }
}
