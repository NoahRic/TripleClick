/* TripleClickMouseProcessor.cs
 * Copyright Noah Richards, licensed under the Ms-PL.
 * 
 * Check out blogs.msdn.com/noahric for more information about the Visual Studio 2010 editor.
 */
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Formatting;

namespace TripleClick
{
    [Export(typeof(IMouseProcessorProvider))]
    [Name("TripleClick")]
    [Order(Before = "DragDrop")]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class TripleClickMouseProcessorProvider : IMouseProcessorProvider
    {
        public IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
        {
            return new TripleClickMouseProcessor(wpfTextView);
        }
    }

    internal sealed class TripleClickMouseProcessor : MouseProcessorBase
    {
        private IWpfTextView _view;
        private bool _dragging;
        private SnapshotSpan? _originalLine;

        public TripleClickMouseProcessor(IWpfTextView view)
        {
            _view = view;

            _view.LayoutChanged += (sender, args) =>
                {
                    if (_dragging && args.OldSnapshot != args.NewSnapshot)
                        StopDrag();
                };
        }

        #region Overrides

        public override void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount != 3)
                return;

            var line = GetTextViewLineUnderPoint(e);
            if (line == null)
                return;

            SnapshotSpan extent = line.ExtentIncludingLineBreak;
            
            SelectSpan(extent);
            StartDrag(extent);

            e.Handled = true;
        }

        public override void PreprocessMouseMove(MouseEventArgs e)
        {
            if (!_dragging || e.LeftButton != MouseButtonState.Pressed)
                return;

            // If somehow the drag wasn't set up correctly or the view has changed, stop the drag immediately
            if (_originalLine == null || _originalLine.Value.Snapshot != _view.TextSnapshot)
            {
                StopDrag();
                return;
            }

            var line = GetTextViewLineUnderPoint(e);
            if (line == null)
                return;

            SnapshotSpan newExtent = line.ExtentIncludingLineBreak;

            // Calculate the new extent, using the original span
            int start = Math.Min(newExtent.Start, _originalLine.Value.Start);
            int end = Math.Max(newExtent.End, _originalLine.Value.End);

            SelectSpan(new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(start, end)));

            e.Handled = true;
        }

        public override void PostprocessMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            StopDrag();
        }

        #endregion

        #region Helpers

        ITextViewLine GetTextViewLineUnderPoint(MouseEventArgs e)
        {
            Point viewPoint = RelativeToView(e.GetPosition(_view.VisualElement));

            return _view.TextViewLines.GetTextViewLineContainingYCoordinate(viewPoint.Y);
        }

        void SelectSpan(SnapshotSpan extent)
        {
            if (!extent.IsEmpty)
            {
                _view.Selection.Select(extent, true);
                _view.Caret.MoveTo(_view.Selection.ActivePoint);
            }
            else
            {
                _view.Selection.Clear();
                _view.Caret.MoveTo(extent.Start.TranslateTo(_view.TextSnapshot, PointTrackingMode.Negative));
            }
        }

        void StartDrag(SnapshotSpan extent)
        {
            _dragging = _view.VisualElement.CaptureMouse();

            if (_dragging)
            {
                _originalLine = extent;
            }
        }

        void StopDrag()
        {
            if (_dragging)
            {
                _view.VisualElement.ReleaseMouseCapture();
                _originalLine = null;
                _dragging = false;
            }
        }

        Point RelativeToView(Point position)
        {
            return new Point(position.X - _view.ViewportLeft, position.Y - _view.ViewportTop);
        }

        #endregion
    }
}
