using System.Windows;
using System.Windows.Input;

namespace PrivateWin10
{
  public static class MouseDownHelper
  {
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled", 
        typeof (bool), typeof (MouseDownHelper), (PropertyMetadata) new FrameworkPropertyMetadata((object) false, new PropertyChangedCallback(MouseDownHelper.OnNotifyPropertyChanged)));
    internal static readonly DependencyPropertyKey IsMouseDownPropertyKey = DependencyProperty.RegisterAttachedReadOnly("IsMouseDown", 
        typeof (bool), typeof (MouseDownHelper), (PropertyMetadata) new FrameworkPropertyMetadata((object) false));
    public static readonly DependencyProperty IsMouseDownProperty = MouseDownHelper.IsMouseDownPropertyKey.DependencyProperty;
    internal static readonly DependencyPropertyKey IsMouseLeftButtonDownPropertyKey = DependencyProperty.RegisterAttachedReadOnly("IsMouseLeftButtonDown", 
        typeof (bool), typeof (MouseDownHelper), (PropertyMetadata) new FrameworkPropertyMetadata((object) false));
    public static readonly DependencyProperty IsMouseLeftButtonDownProperty = MouseDownHelper.IsMouseLeftButtonDownPropertyKey.DependencyProperty;

    public static void SetIsEnabled(UIElement element, bool value)
    {
      element.SetValue(IsEnabledProperty, (object) value);
    }

    public static bool GetIsEnabled(UIElement element)
    {
      return (bool) element.GetValue(IsEnabledProperty);
    }

    private static void OnNotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      UIElement element;
      if ((element = d as UIElement) == null || e.NewValue == null)
        return;
      if ((bool) e.NewValue)
        Register(element);
      else
        UnRegister(element);
    }

    private static void Register(UIElement element)
    {
      element.PreviewMouseDown += Element_MouseDown;
      element.PreviewMouseLeftButtonDown += Element_MouseLeftButtonDown;
      element.MouseLeave += Element_MouseLeave;
      element.PreviewMouseUp += Element_MouseUp;
    }

    private static void UnRegister(UIElement element)
    {
      element.PreviewMouseDown -= Element_MouseDown;
      element.PreviewMouseLeftButtonDown -= Element_MouseLeftButtonDown;
      element.MouseLeave -= Element_MouseLeave;
      element.PreviewMouseUp -= Element_MouseUp;
    }

    private static void Element_MouseDown(object sender, MouseButtonEventArgs e)
    {
      UIElement source;
      if ((source = e.Source as UIElement) == null)
        return;
      SetIsMouseDown(source, true);
    }

    private static void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      UIElement source;
      if ((source = e.Source as UIElement) == null)
        return;
      SetIsMouseLeftButtonDown(source, true);
    }

    private static void Element_MouseLeave(object sender, MouseEventArgs e)
    {
      UIElement source;
      if ((source = e.Source as UIElement) == null)
        return;
      SetIsMouseDown(source, false);
      SetIsMouseLeftButtonDown(source, false);
    }

    private static void Element_MouseUp(object sender, MouseButtonEventArgs e)
    {
      UIElement source;
      if ((source = e.Source as UIElement) == null)
        return;
      SetIsMouseDown(source, false);
      SetIsMouseLeftButtonDown(source, false);
    }

    internal static void SetIsMouseDown(UIElement element, bool value)
    {
      element.SetValue(IsMouseDownPropertyKey, (object) value);
    }

    public static bool GetIsMouseDown(UIElement element)
    {
      return (bool) element.GetValue(IsMouseDownProperty);
    }

    internal static void SetIsMouseLeftButtonDown(UIElement element, bool value)
    {
      element.SetValue(IsMouseLeftButtonDownPropertyKey, (object) value);
    }

    public static bool GetIsMouseLeftButtonDown(UIElement element)
    {
      return (bool) element.GetValue(IsMouseLeftButtonDownProperty);
    }
  }
}
