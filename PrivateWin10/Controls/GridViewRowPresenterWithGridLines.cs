using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PrivateWin10.Controls
{
    public class GridViewRowPresenterWithGridLines : GridViewRowPresenter
    {
        private static readonly Style DefaultSeparatorStyle;
        public static readonly DependencyProperty SeparatorStyleProperty;
        private readonly List<FrameworkElement> _lines = new List<FrameworkElement>();

        static GridViewRowPresenterWithGridLines()
        {
            DefaultSeparatorStyle = new Style(typeof (Rectangle));
            DefaultSeparatorStyle.Setters.Add(new Setter(Shape.FillProperty, SystemColors.ControlLightBrush));
            SeparatorStyleProperty = DependencyProperty.Register("SeparatorStyle", typeof (Style), typeof (GridViewRowPresenterWithGridLines),
                                                                    new UIPropertyMetadata(DefaultSeparatorStyle, SeparatorStyleChanged));
        }

        public Style SeparatorStyle
        {
            get { return (Style) GetValue(SeparatorStyleProperty); }
            set { SetValue(SeparatorStyleProperty, value); }
        }

        private IEnumerable<FrameworkElement> Children
        {
            get { return LogicalTreeHelper.GetChildren(this).OfType<FrameworkElement>(); }
        }

        private static void SeparatorStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (GridViewRowPresenterWithGridLines) d;
            var style = (Style) e.NewValue;
            foreach (FrameworkElement line in presenter._lines)
            {
                line.Style = style;
            }
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var size = base.ArrangeOverride(arrangeSize);
            var children = Children.ToList();
            EnsureLines(children.Count);
            for (var i = 0; i < _lines.Count; i++)
            {
                var child = children[i];
                var x = child.TransformToAncestor(this).Transform(new Point(child.ActualWidth, 0)).X + child.Margin.Right;
                var rect = new Rect(x, -Margin.Top, 1, size.Height + Margin.Top + Margin.Bottom);
                var line = _lines[i];
                line.Measure(rect.Size);
                line.Arrange(rect);
            }
            return size;
        }

        private void EnsureLines(int count)
        {
            count = count - _lines.Count;
            for (var i = 0; i < count; i++)
            {
                var line = (FrameworkElement) Activator.CreateInstance(SeparatorStyle.TargetType);
                line = new Rectangle{Fill=Brushes.LightGray};
                line.Style = SeparatorStyle;
                AddVisualChild(line);
                _lines.Add(line);
            }
        }

        protected override int VisualChildrenCount
        {
            get { return base.VisualChildrenCount + _lines.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            var count = base.VisualChildrenCount;
            return index < count ? base.GetVisualChild(index) : _lines[index - count];
        }
    }
}