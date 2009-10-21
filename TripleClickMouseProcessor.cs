/* TripleClickMouseProcessor.cs
 * Copyright Noah Richards, licensed under the Ms-PL.
 * Check out blogs.msdn.com/noahric for more information about the Visual Studio 2010 editor!
 */
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

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

        public TripleClickMouseProcessor(IWpfTextView view)
        {
            _view = view;
        }

        public override void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount != 3)
                return;

            Point viewPoint = RelativeToView(e.GetPosition(_view.VisualElement));

            var line = _view.TextViewLines.GetTextViewLineContainingYCoordinate(viewPoint.Y);
            if (line == null)
                return;

            var extent = line.Extent;
            if (!extent.IsEmpty)
            {
                _view.Selection.Select(extent, false);
                _view.Caret.MoveTo(_view.Selection.ActivePoint);
            }
            else
            {
                _view.Selection.Clear();
                _view.Caret.MoveTo(extent.Start.TranslateTo(_view.TextSnapshot, PointTrackingMode.Negative));
            }

            e.Handled = true;
        }

        Point RelativeToView(Point position)
        {
            return new Point(position.X - _view.ViewportLeft, position.Y - _view.ViewportTop);
        }
    }
}