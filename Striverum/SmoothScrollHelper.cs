using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Striverum
{
    public static class SmoothScrollHelper
    {
        private static readonly Dictionary<ScrollViewer, SmoothScrollState> _activeStates = new();

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(SmoothScrollHelper),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.PreviewMouseWheel += Element_PreviewMouseWheel;
                }
                else
                {
                    element.PreviewMouseWheel -= Element_PreviewMouseWheel;
                }
            }
        }

        private static void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sv = sender as ScrollViewer ?? FindVisualChild<ScrollViewer>(sender as DependencyObject);
            if (sv == null) return;
            if (sv.ScrollableHeight <= 0 && sv.ScrollableWidth <= 0) return;

            e.Handled = true;

            if (!_activeStates.TryGetValue(sv, out var state))
            {
                state = new SmoothScrollState(sv);
                _activeStates[sv] = state;
            }

            state.ScrollBy(-e.Delta);
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var sub = FindVisualChild<T>(child);
                if (sub != null) return sub;
            }
            return null;
        }

        private class SmoothScrollState
        {
            private readonly ScrollViewer _sv;
            private double _targetOffset;
            private bool _isAnimating;

            public SmoothScrollState(ScrollViewer sv)
            {
                _sv = sv;
                _targetOffset = sv.ScrollableHeight > 0 ? sv.VerticalOffset : sv.HorizontalOffset;
            }

            public void ScrollBy(double delta)
            {
                bool isVertical = _sv.ScrollableHeight > 0;
                double currentOffset = isVertical ? _sv.VerticalOffset : _sv.HorizontalOffset;
                double maxScroll = isVertical ? _sv.ScrollableHeight : _sv.ScrollableWidth;

                if (maxScroll <= 0) return;

                // If offset jumped externally (e.g. user dragged thumb), sync with current position
                if (!_isAnimating || Math.Abs(currentOffset - _targetOffset) > 200)
                {
                    _targetOffset = currentOffset;
                }

                // 1 mouse wheel notch is delta 120 -> scroll ~90px smoothly
                _targetOffset += delta * 0.75;
                _targetOffset = Math.Max(0, Math.Min(maxScroll, _targetOffset));

                if (!_isAnimating)
                {
                    _isAnimating = true;
                    CompositionTarget.Rendering += OnRendering;
                }
            }

            private void OnRendering(object sender, EventArgs e)
            {
                bool isVertical = _sv.ScrollableHeight > 0;
                double maxScroll = isVertical ? _sv.ScrollableHeight : _sv.ScrollableWidth;

                if (maxScroll <= 0)
                {
                    StopAnimation();
                    return;
                }

                double current = isVertical ? _sv.VerticalOffset : _sv.HorizontalOffset;
                double diff = _targetOffset - current;

                if (Math.Abs(diff) < 0.6)
                {
                    if (isVertical)
                        _sv.ScrollToVerticalOffset(_targetOffset);
                    else
                        _sv.ScrollToHorizontalOffset(_targetOffset);

                    StopAnimation();
                    return;
                }

                // Smooth exponential easing (lerp) per frame
                double next = current + diff * 0.22;
                if (isVertical)
                    _sv.ScrollToVerticalOffset(next);
                else
                    _sv.ScrollToHorizontalOffset(next);
            }

            private void StopAnimation()
            {
                if (_isAnimating)
                {
                    _isAnimating = false;
                    CompositionTarget.Rendering -= OnRendering;
                }
            }
        }
    }
}
