using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CSharpControls.Wpf
{
    public class ToggleSwitch : ToggleButton
    {
        #region Constructor

        static ToggleSwitch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(ctrlType, new FrameworkPropertyMetadata(ctrlType));
        }

        #endregion

        #region Properties

        private const string CheckLabeLAnimationName = "PART_CheckLabeLAnimation";
        private const string CheckLabeLAnimationName2 = "PART_CheckLabeLAnimation2";
        private const string SharedGroupStateName = "PART_SharedGroupSize";

        private const string HeaderPlacementVisualState = "HeaderContentPlacementAt";
        private const string HeaderStretchVisualState = "HeaderStretchAt";
        private const string SwitchPlacementVisualState = "SwitchContentPlacementAt";

        private const double DefaultSwitchWidthValue = 44.0D;
        private const string DefaultCheckedTextValue = "On";
        private const string DefaultIndeterminateTextValue = "???";
        private const string DefaultUncheckedTextValue = "Off";
        private const Dock DefaultHeaderContentPlacementValue = Dock.Left;
        private const Dock DefaultSwitchContentPlacementValue = Dock.Left;
        private const HorizontalAlignment DefaultHeaderHorizontalValue = HorizontalAlignment.Left;
        private const HorizontalAlignment DefaultCheckHorizontalValue = HorizontalAlignment.Left;
        private const HorizontalAlignment DefaultSwitchHorizontalValue = HorizontalAlignment.Left;
        private static readonly string DefaultSharedSizeGroupName = string.Empty;
        private static readonly Thickness DefaultHeaderPaddingValue = new Thickness(0D);
        private static readonly Thickness DefaultCheckPaddingValue = new Thickness(0D);
        private static readonly Thickness DefaultSwitchPaddingValue = new Thickness(8D, 0D, 8D, 0D);

        private static readonly Brush CheckedBackgroundBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x63, 0xB1));
        private static readonly Brush CheckedForegroundBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
        private static readonly Brush CheckedBorderBrushBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x63, 0xB1));

        private static readonly Brush CheckedBackground2Brush = new SolidColorBrush(Color.FromRgb(0x00, 0xB1, 0xB1));
        private static readonly Brush CheckedForeground2Brush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
        private static readonly Brush CheckedBorder2BrushBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xB1, 0xB1));

        private static readonly Brush UncheckedBackgroundBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
        private static readonly Brush UncheckedForegroundBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
        private static readonly Brush UncheckedBorderBrushBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));

        private static readonly Type ctrlType = typeof(ToggleSwitch);
        private const string ctrlName = nameof(ToggleSwitch);

        public static readonly DependencyProperty HeaderHorizontalAlignmentProperty =
            DependencyProperty.Register(nameof(HeaderHorizontalAlignment), typeof(HorizontalAlignment), ctrlType, new PropertyMetadata(DefaultHeaderHorizontalValue, OnHeaderHorizontalAlignmentChanged));

        public static readonly DependencyProperty HeaderPaddingProperty =
            DependencyProperty.Register(nameof(HeaderPadding), typeof(Thickness), ctrlType, new PropertyMetadata(DefaultHeaderPaddingValue));

        public static readonly DependencyProperty HeaderContentPlacementProperty =
            DependencyProperty.Register(nameof(HeaderContentPlacement), typeof(Dock), ctrlType, new PropertyMetadata(DefaultHeaderContentPlacementValue, OnHeaderContentPlacementPropertyChanged));

        public static readonly DependencyProperty CheckedTextProperty =
           DependencyProperty.Register(nameof(CheckedText), typeof(string), ctrlType, new PropertyMetadata(DefaultCheckedTextValue, new PropertyChangedCallback(OnCheckTextChanged)));

        public static readonly DependencyProperty IndeterminateTextProperty =
           DependencyProperty.Register(nameof(IndeterminateText), typeof(string), ctrlType, new PropertyMetadata(DefaultIndeterminateTextValue, new PropertyChangedCallback(OnCheckTextChanged)));

        public static readonly DependencyProperty UncheckedTextProperty =
            DependencyProperty.Register(nameof(UncheckedText), typeof(string), ctrlType, new PropertyMetadata(DefaultUncheckedTextValue));

        public static readonly DependencyProperty CheckHorizontalAlignmentProperty =
            DependencyProperty.Register(nameof(CheckHorizontalAlignment), typeof(HorizontalAlignment), ctrlType, new PropertyMetadata(DefaultCheckHorizontalValue));

        public static readonly DependencyProperty CheckPaddingProperty =
            DependencyProperty.Register(nameof(CheckPadding), typeof(Thickness), ctrlType, new PropertyMetadata(DefaultCheckPaddingValue));

        public static readonly DependencyProperty CheckedBackgroundProperty =
            DependencyProperty.Register(nameof(CheckedBackground), typeof(Brush), ctrlType, new PropertyMetadata(CheckedBackgroundBrush));

        public static readonly DependencyProperty CheckedForegroundProperty =
            DependencyProperty.Register(nameof(CheckedForeground), typeof(Brush), ctrlType, new PropertyMetadata(CheckedForegroundBrush));

        public static readonly DependencyProperty CheckedBorderBrushProperty =
            DependencyProperty.Register(nameof(CheckedBorderBrush), typeof(Brush), ctrlType, new PropertyMetadata(CheckedBorderBrushBrush));

        public static readonly DependencyProperty CheckedBackground2Property =
        DependencyProperty.Register(nameof(CheckedBackground2), typeof(Brush), ctrlType, new PropertyMetadata(CheckedBackground2Brush));

        public static readonly DependencyProperty CheckedForeground2Property =
            DependencyProperty.Register(nameof(CheckedForeground2), typeof(Brush), ctrlType, new PropertyMetadata(CheckedForeground2Brush));

        public static readonly DependencyProperty CheckedBorder2BrushProperty =
            DependencyProperty.Register(nameof(CheckedBorder2Brush), typeof(Brush), ctrlType, new PropertyMetadata(CheckedBorder2BrushBrush));

        public static readonly DependencyProperty UncheckedBackgroundProperty =
            DependencyProperty.Register(nameof(UncheckedBackground), typeof(Brush), ctrlType, new PropertyMetadata(UncheckedBackgroundBrush));

        public static readonly DependencyProperty UncheckedForegroundProperty =
            DependencyProperty.Register(nameof(UncheckedForeground), typeof(Brush), ctrlType, new PropertyMetadata(UncheckedForegroundBrush));

        public static readonly DependencyProperty UncheckedBorderBrushProperty =
            DependencyProperty.Register(nameof(UncheckedBorderBrush), typeof(Brush), ctrlType, new PropertyMetadata(UncheckedBorderBrushBrush));

        public static readonly DependencyProperty SwitchWidthProperty =
            DependencyProperty.Register(nameof(SwitchWidth), typeof(Double), ctrlType, new PropertyMetadata(DefaultSwitchWidthValue));

        public static readonly DependencyProperty SwitchPaddingProperty =
            DependencyProperty.Register(nameof(SwitchPadding), typeof(Thickness), ctrlType, new PropertyMetadata(DefaultSwitchPaddingValue));

        public static readonly DependencyProperty SwitchContentPlacementProperty =
            DependencyProperty.Register(nameof(SwitchContentPlacement), typeof(Dock), ctrlType, new PropertyMetadata(DefaultSwitchContentPlacementValue, OnSwitchContentPlacementPropertyChanged));

        public static readonly DependencyProperty SwitchHorizontalAlignmentProperty =
            DependencyProperty.Register(nameof(SwitchHorizontalAlignment), typeof(HorizontalAlignment), ctrlType, new PropertyMetadata(DefaultSwitchHorizontalValue));

        public static readonly DependencyProperty SharedSizeGroupNameProperty =
            DependencyProperty.Register(nameof(SharedSizeGroupName), typeof(string), ctrlType, null);

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly DependencyProperty IsHeaderStretchProperty = DependencyProperty.Register(nameof(IsHeaderStretch), typeof(bool), ctrlType, null);

        [Bindable(true)]
        [Description("Gets or sets the horizontal alignment of the Header's content."), Category(ctrlName)]
        public HorizontalAlignment HeaderHorizontalAlignment
        {
            get { return (HorizontalAlignment)GetValue(HeaderHorizontalAlignmentProperty); }
            set { SetValue(HeaderHorizontalAlignmentProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the spacing around the header text."), Category(ctrlName)]
        public Thickness HeaderPadding
        {
            get { return (Thickness)GetValue(HeaderPaddingProperty); }
            set { SetValue(HeaderPaddingProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the placement of the Header Content to the Switch."), Category(ctrlName)]
        public Dock HeaderContentPlacement
        {
            get { return (Dock)GetValue(HeaderContentPlacementProperty); }
            set { SetValue(HeaderContentPlacementProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the Switch text for checked state."), Category(ctrlName)]
        public string CheckedText
        {
            get { return (string)GetValue(CheckedTextProperty); }
            set { SetValue(CheckedTextProperty, value); }
        }


        [Bindable(true)]
        [Description("Gets or sets the Switch text for indeterminate state."), Category(ctrlName)]
        public string IndeterminateText
        {
            get { return (string)GetValue(IndeterminateTextProperty); }
            set { SetValue(IndeterminateTextProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the Switch text for Unchecked state."), Category(ctrlName)]
        public string UncheckedText
        {
            get { return (string)GetValue(UncheckedTextProperty); }
            set { SetValue(UncheckedTextProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the horizontal alignment of the Check's content."), Category(ctrlName)]
        public HorizontalAlignment CheckHorizontalAlignment
        {
            get { return (HorizontalAlignment)GetValue(CheckHorizontalAlignmentProperty); }
            set { SetValue(CheckHorizontalAlignmentProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the spacing around the switch text."), Category(ctrlName)]
        public Thickness CheckPadding
        {
            get { return (Thickness)GetValue(CheckPaddingProperty); }
            set { SetValue(CheckPaddingProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the graphical switch checked background brush."), Category(ctrlName)]
        public Brush CheckedBackground
        {
            get { return (Brush)GetValue(CheckedBackgroundProperty); }
            set { SetValue(CheckedBackgroundProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the graphical switch checked foreground brush."), Category(ctrlName)]
        public Brush CheckedForeground
        {
            get { return (Brush)GetValue(CheckedForegroundProperty); }
            set { SetValue(CheckedForegroundProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the graphical switch checked border brush."), Category(ctrlName)]
        public Brush CheckedBorderBrush
        {
            get { return (Brush)GetValue(CheckedBorderBrushProperty); }
            set { SetValue(CheckedBorderBrushProperty, value); }
        }
       

        [Bindable(true)]
        [Description("Gets or sets the graphical switch checked background brush."), Category(ctrlName)]
        public Brush CheckedBackground2
        {
            get { return (Brush)GetValue(CheckedBackground2Property); }
            set { SetValue(CheckedBackground2Property, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the graphical switch checked foreground brush."), Category(ctrlName)]
        public Brush CheckedForeground2
        {
            get { return (Brush)GetValue(CheckedForeground2Property); }
            set { SetValue(CheckedForeground2Property, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the graphical switch checked border brush."), Category(ctrlName)]
        public Brush CheckedBorder2Brush
        {
            get { return (Brush)GetValue(CheckedBorder2BrushProperty); }
            set { SetValue(CheckedBorder2BrushProperty, value); }
        }


        [Bindable(true)]
        [Description("Gets or sets the graphical switch Unchecked background brush."), Category(ctrlName)]
        public Brush UncheckedBackground
        {
            get { return (Brush)GetValue(UncheckedBackgroundProperty); }
            set { SetValue(UncheckedBackgroundProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the graphical switch Unchecked foreground brush."), Category(ctrlName)]
        public Brush UncheckedForeground
        {
            get { return (Brush)GetValue(UncheckedForegroundProperty); }
            set { SetValue(UncheckedForegroundProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the graphical switch Unchecked border brush."), Category(ctrlName)]
        public Brush UncheckedBorderBrush
        {
            get { return (Brush)GetValue(UncheckedBorderBrushProperty); }
            set { SetValue(UncheckedBorderBrushProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the width of the graphical switch. (Default: 44)"), Category(ctrlName)]
        public Double SwitchWidth
        {
            get { return (Double)GetValue(SwitchWidthProperty); }
            set { SetValue(SwitchWidthProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the spacing around the graphical switch."), Category(ctrlName)]
        public Thickness SwitchPadding
        {
            get { return (Thickness)GetValue(SwitchPaddingProperty); }
            set { SetValue(SwitchPaddingProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the placement of the graphical switch to the state text."), Category(ctrlName)]
        public Dock SwitchContentPlacement
        {
            get { return (Dock)GetValue(SwitchContentPlacementProperty); }
            set { SetValue(SwitchContentPlacementProperty, value); }
        }

        [Bindable(true)]
        [Description("Gets or sets the horizontal alignment of the Switch's content."), Category(ctrlName)]
        public HorizontalAlignment SwitchHorizontalAlignment
        {
            get { return (HorizontalAlignment)GetValue(SwitchHorizontalAlignmentProperty); }
            set { SetValue(SwitchHorizontalAlignmentProperty, value); }
        }

        [Bindable(true)]
        [Description("Name of Shared Size Group for the Left column (header or switch). Depends on HeaderContentPlacement property."), Category(ctrlName)]
        public string SharedSizeGroupName
        {
            get { return (string)GetValue(SharedSizeGroupNameProperty); }
            set { SetValue(SharedSizeGroupNameProperty, value); }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsHeaderStretch
        {
            get { return (bool)GetValue(IsHeaderStretchProperty); }
            set { SetValue(IsHeaderStretchProperty, value); }
        }

        #endregion

        #region Events

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            CheckLabeLAnimationValue(this);
            SharedGroupStateValue(this, HeaderContentPlacement);
            CoerceHeaderSizing();
            UpdatePlacementVisualState();
        }

        private static void OnHeaderHorizontalAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ToggleSwitch ctrl;
            if (d.TryCast(out ctrl))
                ctrl.CoerceHeaderSizing();
        }

        private void CoerceHeaderSizing()
        {
            SetValue(IsHeaderStretchProperty, HeaderContentPlacement == Dock.Left || HeaderContentPlacement == Dock.Right);
        }

        private static void OnCheckTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ToggleSwitch ctrl;
            if (d.TryCast(out ctrl))
                CheckLabeLAnimationValue(ctrl);
        }

        private static void OnHeaderContentPlacementPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ToggleSwitch ctrl;
            if (d.TryCast(out ctrl))
            {
                var oldValue = (Dock)e.OldValue;
                var newValue = (Dock)e.NewValue;

                ChangeSharedGroupStateValue(ctrl, newValue, oldValue);
                ctrl.OnHeaderContentPlacementChanged(newValue, oldValue);
            }
        }

        private static void OnSwitchContentPlacementPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ToggleSwitch ctrl;
            if (d.TryCast(out ctrl))
                ctrl.OnSwitchContentPlacementChanged((Dock)e.NewValue, (Dock)e.OldValue);
        }

        #endregion

        #region Methods

        private static void ChangeSharedGroupStateValue(ToggleSwitch ctrl, Dock newValue, Dock oldValue)
        {
            SharedGroupStateValue(ctrl, oldValue, false);
            SharedGroupStateValue(ctrl, newValue);
        }

        private static void SharedGroupStateValue(ToggleSwitch ts, Dock placement, bool IsBound = true)
        {
            var field = (DiscreteObjectKeyFrame)ts.Template?.FindName(SharedGroupStateName + placement.ToString(), ts);
            if (field != null)
            {
                var binding = new Binding(nameof(SharedSizeGroupName)) { Source = ts };
                BindingOperations.SetBinding(field, ObjectKeyFrame.ValueProperty, IsBound ? binding : new Binding());
            }
        }

        private static void CheckLabeLAnimationValue(ToggleSwitch ts)
        {
            var checkLabelAnimation = (DiscreteObjectKeyFrame)ts.Template?.FindName(CheckLabeLAnimationName, ts);
            if (checkLabelAnimation != null)
            {
                var binding = new Binding(nameof(CheckedText)) { Source = ts };
                BindingOperations.SetBinding(checkLabelAnimation, ObjectKeyFrame.ValueProperty, binding);
            }

            var checkLabelAnimation2 = (DiscreteObjectKeyFrame)ts.Template?.FindName(CheckLabeLAnimationName2, ts);
            if (checkLabelAnimation2 != null)
            {
                var binding = new Binding(nameof(IndeterminateText)) { Source = ts };
                BindingOperations.SetBinding(checkLabelAnimation2, ObjectKeyFrame.ValueProperty, binding);
            }
        }

        protected virtual void OnHeaderContentPlacementChanged(Dock newValue, Dock oldValue)
        {
            CoerceHeaderSizing();
            UpdateHeaderVisualState(newValue);
        }

        protected virtual void OnSwitchContentPlacementChanged(Dock newValue, Dock oldValue)
        {
            UpdateSwitchPlacementVisualState(newValue);
        }

        private void UpdatePlacementVisualState()
        {
            UpdateHeaderVisualState(HeaderContentPlacement);
            UpdateSwitchPlacementVisualState(SwitchContentPlacement);
        }

        private void UpdateHeaderVisualState(Dock newPlacement)
        {
            GoToState($"{HeaderPlacementVisualState}{newPlacement.ToString()}", false);

            if (IsHeaderStretch)
            {
                switch (newPlacement)
                {
                    case Dock.Right:
                    case Dock.Left:
                        GoToState($"{HeaderStretchVisualState}{newPlacement.ToString()}", false);
                        break;
                    case Dock.Top:
                    case Dock.Bottom:
                        GoToState($"{HeaderStretchVisualState}Middle", false);
                        break;
                }
            }
            else
            {
                GoToState($"{HeaderStretchVisualState}Middle", false);
            }
        }

        private void UpdateSwitchPlacementVisualState(Dock newPlacement)
        {
            GoToState($"{SwitchPlacementVisualState}{newPlacement.ToString()}", false);
        }

        internal bool GoToState(string stateName, bool useTransitions)
        {
            return VisualStateManager.GoToState(this, stateName, useTransitions);
        }

        #endregion
    }
}