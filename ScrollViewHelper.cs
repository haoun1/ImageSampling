using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScrollViewHelper
{
    public static class Helper
    {
        public static bool GetAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollProperty, value);
        }

        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(Helper), new PropertyMetadata(false, AutoScrollPropertyChanged));

        private static void AutoScrollPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var datagrid = d as DataGrid;

            if (datagrid != null && (bool)e.NewValue && datagrid.Items.Count > 1)
            {
                datagrid.ScrollIntoView(datagrid.Items[datagrid.Items.Count - 1]);
            }
            //var scrollViewer = d as ScrollViewer;

            //if (scrollViewer != null && (bool)e.NewValue)
            //{
            //    scrollViewer.ScrollToBottom();
            //}
        }
    }
}


namespace nsSizeObserver
{
    public static class SizeObserver
    {
        public static readonly DependencyProperty ObserveProperty = DependencyProperty.RegisterAttached(
            "Observe",
            typeof(bool),
            typeof(SizeObserver),
            new FrameworkPropertyMetadata(OnObserveChanged));

        public static readonly DependencyProperty ObservedWidthProperty = DependencyProperty.RegisterAttached(
            "ObservedWidth",
            typeof(double),
            typeof(SizeObserver));

        public static readonly DependencyProperty ObservedHeightProperty = DependencyProperty.RegisterAttached(
            "ObservedHeight",
            typeof(double),
            typeof(SizeObserver));

        public static bool GetObserve(FrameworkElement frameworkElement)
        {
            //frameworkElement.AssertNotNull("frameworkElement");
            return (bool)frameworkElement.GetValue(ObserveProperty);
        }

        public static void SetObserve(FrameworkElement frameworkElement, bool observe)
        {
            //frameworkElement.AssertNotNull("frameworkElement");
            frameworkElement.SetValue(ObserveProperty, observe);
        }

        public static double GetObservedWidth(FrameworkElement frameworkElement)
        {
            //ameworkElement.AssertNotNull("frameworkElement");
            return (double)frameworkElement.GetValue(ObservedWidthProperty);
        }

        public static void SetObservedWidth(FrameworkElement frameworkElement, double observedWidth)
        {
            //frameworkElement.AssertNotNull("frameworkElement");
            frameworkElement.SetValue(ObservedWidthProperty, observedWidth);
        }

        public static double GetObservedHeight(FrameworkElement frameworkElement)
        {
            //frameworkElement.AssertNotNull("frameworkElement");
            return (double)frameworkElement.GetValue(ObservedHeightProperty);
        }

        public static void SetObservedHeight(FrameworkElement frameworkElement, double observedHeight)
        {
            //frameworkElement.AssertNotNull("frameworkElement");
            frameworkElement.SetValue(ObservedHeightProperty, observedHeight);
        }

        private static void OnObserveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var frameworkElement = (FrameworkElement)dependencyObject;

            if ((bool)e.NewValue)
            {
                frameworkElement.SizeChanged += OnFrameworkElementSizeChanged;
                UpdateObservedSizesForFrameworkElement(frameworkElement);
            }
            else
            {
                frameworkElement.SizeChanged -= OnFrameworkElementSizeChanged;
            }
        }

        private static void OnFrameworkElementSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateObservedSizesForFrameworkElement((FrameworkElement)sender);
        }

        private static void UpdateObservedSizesForFrameworkElement(FrameworkElement frameworkElement)
        {
            frameworkElement.SetCurrentValue(ObservedWidthProperty, frameworkElement.ActualWidth);
            frameworkElement.SetCurrentValue(ObservedHeightProperty, frameworkElement.ActualHeight);


        }
    }
}


namespace ControlHelper
{
    public class LedControl : CheckBox
    {
        static LedControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LedControl), new FrameworkPropertyMetadata(typeof(LedControl)));
        }

        public static readonly DependencyProperty OnColorProperty =
            DependencyProperty.Register("OnColor", typeof(Brush), typeof(LedControl), new PropertyMetadata(Brushes.Green));

        public Brush OnColor
        {
            get
            {
                return (Brush)GetValue(OnColorProperty);
            }
            set
            {
                SetValue(OnColorProperty, value);
            }
        }

        public static readonly DependencyProperty OffColorProperty =
            DependencyProperty.Register("OffColor", typeof(Brush), typeof(LedControl), new PropertyMetadata(Brushes.Red));

        public Brush OffColor
        {
            get
            {
                return (Brush)GetValue(OffColorProperty);
            }
            set
            {
                SetValue(OffColorProperty, value);
            }
        }
    }

}

