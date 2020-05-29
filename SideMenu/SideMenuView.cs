using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using static System.Math;
using System.ComponentModel;

namespace SideMenu
{
    public class SideMenuView : AbsoluteLayout
    {
        private static readonly double SwipeThresholdDistance = 17;

        private static readonly TimeSpan SwipeThresholdTime = TimeSpan.FromMilliseconds(Device.RuntimePlatform == Device.Android ? 100 : 60);

        public static readonly BindableProperty DiffProperty = BindableProperty.Create(nameof(Diff), typeof(double), typeof(SideMenuView), 0.0, BindingMode.OneWayToSource);

        public static readonly BindableProperty CurrentGestureDiffProperty = BindableProperty.Create(nameof(CurrentGestureDiff), typeof(double), typeof(SideMenuView), 0.0, BindingMode.OneWayToSource);

        public static readonly BindableProperty IsLeftMenuGestureEnabledProperty = BindableProperty.Create(nameof(IsLeftMenuGestureEnabled), typeof(bool), typeof(SideMenuView), true);

        public static readonly BindableProperty IsRightMenuGestureEnabledProperty = BindableProperty.Create(nameof(IsRightMenuGestureEnabled), typeof(bool), typeof(SideMenuView), true);

        public static readonly BindableProperty GestureThresholdProperty = BindableProperty.Create(nameof(GestureThreshold), typeof(double), typeof(SideMenuView), 7.0);

        public static readonly BindableProperty VerticalCancelingGestureThresholdProperty = BindableProperty.Create(nameof(VerticalCancelingGestureThreshold), typeof(double), typeof(SideMenuView), double.PositiveInfinity);

        public static readonly BindableProperty ShouldThrottleGestureProperty = BindableProperty.Create(nameof(ShouldThrottleGesture), typeof(bool), typeof(SideMenuView), false);

        public static readonly BindableProperty PlaceProperty = BindableProperty.CreateAttached(nameof(GetPlace), typeof(SideMenuViewPlace), typeof(SideMenuView), default(SideMenuViewPlace));

        private readonly PanGestureRecognizer _panGesture = new PanGestureRecognizer();

        private readonly List<TimeDiffItem> _timeDiffItems = new List<TimeDiffItem>();

        private View _mainView;

        private View _leftMenu;

        private View _rightMenu;

        private bool _isPanStarted;

        private bool _isGestureDirectionResolved;

        public SideMenuView()
        {
            _panGesture.PanUpdated += OnPanUpdated;
            GestureRecognizers.Add(_panGesture);
        }

        public double Diff
        {
            get => (double)GetValue(DiffProperty);
            set => SetValue(DiffProperty, value);
        }

        public double CurrentGestureDiff
        {
            get => (double)GetValue(CurrentGestureDiffProperty);
            set => SetValue(CurrentGestureDiffProperty, value);
        }

        public bool IsLeftMenuGestureEnabled
        {
            get => (bool)GetValue(IsLeftMenuGestureEnabledProperty);
            set => SetValue(IsLeftMenuGestureEnabledProperty, value);
        }

        public bool IsRightMenuGestureEnabled
        {
            get => (bool)GetValue(IsRightMenuGestureEnabledProperty);
            set => SetValue(IsRightMenuGestureEnabledProperty, value);
        }

        public double GestureThreshold
        {
            get => (double)GetValue(GestureThresholdProperty);
            set => SetValue(GestureThresholdProperty, value);
        }

        public bool ShouldThrottleGesture
        {
            get => (bool)GetValue(ShouldThrottleGestureProperty);
            set => SetValue(ShouldThrottleGestureProperty, value);
        }

        public double VerticalCancelingGestureThreshold
        {
            get => (double)GetValue(VerticalCancelingGestureThresholdProperty);
            set => SetValue(VerticalCancelingGestureThresholdProperty, value);
        }

        public static SideMenuViewPlace GetPlace(BindableObject bindable)
            => (SideMenuViewPlace)bindable.GetValue(PlaceProperty);

        public static void SetPlace(BindableObject bindable, SideMenuViewPlace value)
            => bindable.SetValue(PlaceProperty, value);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (!IsLeftMenuGestureEnabled && !IsRightMenuGestureEnabled)
            {
                return;
            }

            var diff = e.TotalX;
            var verticalDiff = e.TotalY;
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    OnTouchStarted();
                    return;
                case GestureStatus.Running:
                    OnTouchChanged(diff, verticalDiff);
                    return;
                case GestureStatus.Canceled:
                case GestureStatus.Completed:
                    if (Device.RuntimePlatform == Device.Android)
                    {
                        OnTouchChanged(diff, verticalDiff);
                    }
                    OnTouchEnded();
                    return;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async void OnSwiped(SwipeDirection swipeDirection)
        {
            await Task.Delay(1);
            if (_isPanStarted)
            {
                return;
            }

            //TODO: handle swipe
        }

        protected override void OnAdded(View view)
        {
            base.OnAdded(view);
            StoreView(view);
        }

        protected override void OnRemoved(View view)
        {
            base.OnRemoved(view);
            ClearView(view);
        }

        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            base.LayoutChildren(x, y, width, height);
            if (_mainView != null)
            {
                RaiseChild(_mainView);
            }
        }

        private void OnTouchStarted()
        {
            if (_isPanStarted)
            {
                return;
            }
            _isGestureDirectionResolved = false;
            _isPanStarted = true;
            PopulateDiffItems(0);
        }

        private void OnTouchChanged(double diff, double verticalDiff)
        {
            if (!_isPanStarted || Abs(CurrentGestureDiff - diff) <= double.Epsilon)
            {
                return;
            }

            var activeMenu = _leftMenu;
            var inactiveMenu = _rightMenu;
            if (Diff + diff < 0)
            {
                activeMenu = _rightMenu;
                inactiveMenu = _leftMenu;
            }
            if (inactiveMenu != null)
            {
                LowerChild(inactiveMenu);
            }

            var absDiff = Abs(diff);
            var absVerticalDiff = Abs(verticalDiff);
            if (!_isGestureDirectionResolved && Max(absDiff, absVerticalDiff) > VerticalCancelingGestureThreshold)
            {
                absVerticalDiff *= 2.5;
                if (absVerticalDiff >= absDiff)
                {
                    diff = 0;
                    _isPanStarted = false;
                }
                _isGestureDirectionResolved = true;
            }
            else if (activeMenu != null)
            {
                var value = Diff + diff;
                diff = Sign(value) * Min(Abs(value), activeMenu.Width) - Diff;
            }
            else
            {
                diff = -Diff;
            }
            PopulateDiffItems(diff);
            _mainView.TranslationX = Diff + diff;
        }

        private async void OnTouchEnded()
        {
            if (!_isPanStarted)
            {
                return;
            }

            _isPanStarted = false;
            var diff = CurrentGestureDiff;
            var absDiff = Abs(diff);
            Diff += diff;
            CleanDiffItems();
            var isNextSelected = default(bool?);

            //if (absDiff > RealMoveDistance || CheckPanSwipe())
            //{
            //    isNextSelected = diff < 0;
            //}

            _timeDiffItems.Clear();

            if (isNextSelected.HasValue)
            {
               //TODO: Handle move
            }
            else
            {
                //TODO: Handle reset
            }

            //TEST
            await _mainView.TranslateTo(0, _mainView.Y);
            Diff = 0;
        }

        private bool CheckPanSwipe()
        {
            if (_timeDiffItems.Count < 2)
            {
                return false;
            }

            var lastItem = _timeDiffItems.LastOrDefault();
            var firstItem = _timeDiffItems.FirstOrDefault();

            var distDiff = lastItem.Diff - firstItem.Diff;

            if (Sign(distDiff) != Sign(lastItem.Diff))
            {
                return false;
            }

            var absDistDiff = Abs(distDiff);
            var timeDiff = lastItem.Time - firstItem.Time;

            var acceptValue = SwipeThresholdDistance * timeDiff.TotalMilliseconds / SwipeThresholdTime.TotalMilliseconds;

            return absDistDiff >= acceptValue;
        }

        private void PopulateDiffItems(double diff)
        {
            CurrentGestureDiff = diff;
            var timeNow = DateTime.UtcNow;

            if (_timeDiffItems.Count >= 25)
            {
                CleanDiffItems();
            }

            _timeDiffItems.Add(new TimeDiffItem
            {
                Time = timeNow,
                Diff = diff
            });
        }

        private void CleanDiffItems()
        {
            var time = _timeDiffItems.LastOrDefault().Time;

            for (var i = _timeDiffItems.Count - 1; i >= 0; --i)
            {
                if (time - _timeDiffItems[i].Time > SwipeThresholdTime)
                {
                    _timeDiffItems.RemoveAt(i);
                }
            }
        }

        private void StoreView(View view)
        {
            switch (GetPlace(view))
            {
                case SideMenuViewPlace.MainView:
                    SetupMainViewLayout(_mainView = view);
                    break;
                case SideMenuViewPlace.LeftMenu:
                    SetupMenuLayout(_leftMenu = view, new Rectangle(0, 0, -1, 1));
                    break;
                case SideMenuViewPlace.RightMenu:
                    SetupMenuLayout(_rightMenu = view, new Rectangle(1, 0, -1, 1));
                    break;
                default:
                    return;
            }
        }

        private void ClearView(View view)
        {
            switch (GetPlace(view))
            {
                case SideMenuViewPlace.MainView:
                    _mainView = null;
                    return;
                case SideMenuViewPlace.LeftMenu:
                    _leftMenu = null;
                    return;
                case SideMenuViewPlace.RightMenu:
                    _rightMenu = null;
                    return;
            }
        }

        private void SetupMainViewLayout(View view)
        {
            SetLayoutFlags(view, AbsoluteLayoutFlags.All);
            SetLayoutBounds(view, new Rectangle(0, 0, 1, 1));
        }

        private void SetupMenuLayout(View view, Rectangle bounds)
        {
            SetLayoutFlags(view, AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.HeightProportional);
            SetLayoutBounds(view, bounds);
        }

        private void InvokeOnMainThreadIfNeeded(Action action)
        {
            if (!Device.IsInvokeRequired)
            {
                action.Invoke();
                return;
            }
            Device.BeginInvokeOnMainThread(action);
        }
    }
}
