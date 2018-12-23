using System.Windows.Media;

namespace System.Windows
{
    public static class DependencyObjectExtension
    {
        public static bool TryCast<TElement>(this DependencyObject dObj, out TElement element) where TElement : UIElement
        {
            element = dObj as TElement;
            return element != null;
        }

        public static DependencyObject FindAncestorOfType(this DependencyObject o, Type ancestorType)
        {
            var parent = VisualTreeHelper.GetParent(o);
            if (parent != null)
            {
                if (parent.GetType().IsSubclassOf(ancestorType) || parent.GetType() == ancestorType)
                {
                    return parent;
                }
                return FindAncestorOfType(parent, ancestorType);
            }
            return null;
        }
    }
}