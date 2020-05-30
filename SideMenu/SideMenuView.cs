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
        private const string AnimationName = nameof(SideMenuView);

        private const uint AnimationRate = 16;

        private const uint AnimationLength = 250;

        private const double SwipeThresholdDistance = 17;

        private const double AcceptMoveThresholdPercentage = 0.3;

        private static readonly Easing AnimationEasing = Easing.CubicInOut;

        public static readonly TimeSpan SwipeThresholdTime = TimeSpan.FromMilliseconds(Device.RuntimePlatform == Device.Android ? 100 : 60);

        public static readonly BindableProperty DiffProperty = BindableProperty.Create(nameof(Diff), typeof(double), typeof(SideMenuView), 0.0, BindingMode.OneWayToSource);

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

        private View _activeMenu;

        private View _inactiveMenu;

        private bool _isPanStarted;

        private bool _isGestureDirectionResolved;

        public SideMenuView()
        {
            if (Device.RuntimePlatform != Device.Android)
            {
                _panGesture.PanUpdated += OnPanUpdated;
                GestureRecognizers.Add(_panGesture);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public double PreviousDiff { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public double CurrentGestureDiff { get; set; }

        public double Diff
        {
            get => (double)GetValue(DiffProperty);
            set => SetValue(DiffProperty, value);
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
            _isPanStarted = true;
            OnTouchEnded();
            //TODO: Force swipe
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

            PopulateDiffItems(diff);
            var absDiff = Abs(diff);
            var absVerticalDiff = Abs(verticalDiff);
            if (!_isGestureDirectionResolved && Max(absDiff, absVerticalDiff) > VerticalCancelingGestureThreshold)
            {
                absVerticalDiff *= 2.5;
                if (absVerticalDiff >= absDiff)
                {
                    _isPanStarted = false;
                    OnTouchEnded();
                    return;
                }
                _isGestureDirectionResolved = true;
            }
            else if (_activeMenu == null)
            {
                //TODO: Store zero diff
            }

            _mainView.AbortAnimation(AnimationName);
            UpdateDiff(PreviousDiff + diff, false);
        }

        private void OnTouchEnded()
        {
            if (!_isPanStarted)
            {
                return;
            }
            _isPanStarted = false;

            var diff = Diff;
            PreviousDiff = diff;
            CleanDiffItems();
            var isMenuOpening = default(bool?);

            var menuWidth = _activeMenu?.Width ?? 0;
            if (Abs(diff) > menuWidth * AcceptMoveThresholdPercentage || CheckIsSwipe())
            {
                isMenuOpening = diff > 0;
                if(_activeMenu == _rightMenu)
                {
                    isMenuOpening = !isMenuOpening;
                }
            }

            var end = isMenuOpening.HasValue
                ? Sign(diff) * menuWidth
                : 0;

            _mainView.Animate(AnimationName,
                new Animation(v => UpdateDiff(v, true), Diff, end), AnimationRate, AnimationLength, AnimationEasing);
        }

        private void UpdateDiff(double diff, bool shouldUpdatePreviousDiff) {
            _activeMenu = _leftMenu;
            _inactiveMenu = _rightMenu;
            if (diff < 0)
            {
                _activeMenu = _rightMenu;
                _inactiveMenu = _leftMenu;
            }
            if (_inactiveMenu != null && _activeMenu != null)
            {
                LowerChild(_inactiveMenu);
            }

            diff = Sign(diff) * Min(Abs(diff), _activeMenu?.Width ?? 0);
            Diff = diff;
            if (shouldUpdatePreviousDiff)
            {
                PreviousDiff = diff;
            }
            _mainView.TranslationX = diff;
        }

        private bool CheckIsSwipe()
        {
            if (_timeDiffItems.Count < 2)
            {
                return false;
            }

            var lastItem = _timeDiffItems.LastOrDefault();
            var firstItem = _timeDiffItems.FirstOrDefault();
            _timeDiffItems.Clear();

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
