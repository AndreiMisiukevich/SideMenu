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
        #region Private Settings

        private const string AnimationName = nameof(SideMenuView);

        private const uint AnimationRate = 16;

        private const uint AnimationLength = 350;

        private const double SwipeThresholdDistance = 17;

        private const double AcceptMoveThresholdPercentage = 0.3;

        private static readonly Easing AnimationEasing = Easing.SinIn;

        private static readonly TimeSpan SwipeThresholdTime = TimeSpan.FromMilliseconds(Device.RuntimePlatform == Device.Android ? 100 : 60);

        #endregion

        #region Public Bindable Properties

        public static readonly BindableProperty DiffProperty = BindableProperty.Create(nameof(Diff), typeof(double), typeof(SideMenuView), 0.0, BindingMode.OneWayToSource);

        public static readonly BindableProperty GestureThresholdProperty = BindableProperty.Create(nameof(GestureThreshold), typeof(double), typeof(SideMenuView), 7.0);

        public static readonly BindableProperty CancelVerticalGestureThresholdProperty = BindableProperty.Create(nameof(CancelVerticalGestureThreshold), typeof(double), typeof(SideMenuView), 1.0);

        public static readonly BindableProperty ShouldThrottleGestureProperty = BindableProperty.Create(nameof(ShouldThrottleGesture), typeof(bool), typeof(SideMenuView), false);

        public static readonly BindableProperty StateProperty = BindableProperty.Create(nameof(State), typeof(SideMenuViewState), typeof(SideMenuView), SideMenuViewState.Default);

        public static readonly BindableProperty PlaceProperty = BindableProperty.CreateAttached(nameof(GetPlace), typeof(SideMenuViewPlace), typeof(SideMenuView), SideMenuViewPlace.MainView);

        #endregion

        #region Private Fields

        private readonly PanGestureRecognizer _panGesture = new PanGestureRecognizer();

        private readonly List<TimeDiffItem> _timeDiffItems = new List<TimeDiffItem>();

        private View _mainView;

        private View _leftMenu;

        private View _rightMenu;

        private View _activeMenu;

        private View _inactiveMenu;

        private double _zeroDiff;

        private bool _isGestureStarted;

        private bool _isGestureDirectionResolved;

        #endregion

        #region Public Constructors

        public SideMenuView()
        {
            if (Device.RuntimePlatform == Device.Android)
            {
                return;
            }
            _panGesture.PanUpdated += OnPanUpdated;
            GestureRecognizers.Add(_panGesture);
        }

        #endregion

        #region Hidden API

        [EditorBrowsable(EditorBrowsableState.Never)]
        public double PreviousDiff { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public double CurrentGestureDiff { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public SideMenuViewState CurrentGestureState { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
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
            if (_isGestureStarted)
            {
                return;
            }

            State = ResolveSwipeState(swipeDirection == SwipeDirection.Right);
            PerformAnimation();
        }

        #endregion

        #region Public API

        public double Diff
        {
            get => (double)GetValue(DiffProperty);
            set => SetValue(DiffProperty, value);
        }

        public double GestureThreshold
        {
            get => (double)GetValue(GestureThresholdProperty);
            set => SetValue(GestureThresholdProperty, value);
        }

        public double CancelVerticalGestureThreshold
        {
            get => (double)GetValue(CancelVerticalGestureThresholdProperty);
            set => SetValue(CancelVerticalGestureThresholdProperty, value);
        }

        public bool ShouldThrottleGesture
        {
            get => (bool)GetValue(ShouldThrottleGestureProperty);
            set => SetValue(ShouldThrottleGestureProperty, value);
        }

        public SideMenuViewState State
        {
            get => (SideMenuViewState)GetValue(StateProperty);
            set => SetValue(StateProperty, value);
        }

        public static SideMenuViewPlace GetPlace(BindableObject bindable)
            => (SideMenuViewPlace)bindable.GetValue(PlaceProperty);

        public static void SetPlace(BindableObject bindable, SideMenuViewPlace value)
            => bindable.SetValue(PlaceProperty, value);

        #endregion

        #region Protected Overriden Methods

        protected override void OnAdded(View view)
        {
            base.OnAdded(view);
            HandleViewAdded(view);
        }

        protected override void OnRemoved(View view)
        {
            base.OnRemoved(view);
            HandleViewRemoved(view);
        }

        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            base.LayoutChildren(x, y, width, height);
            if (_mainView == null)
            {
                return;
            }
            RaiseChild(_mainView);
        }

        #endregion

        #region Private Methods

        private void OnTouchStarted()
        {
            if (_isGestureStarted)
            {
                return;
            }
            _isGestureDirectionResolved = false;
            _isGestureStarted = true;
            _zeroDiff = 0;
            PopulateDiffItems(0);
        }

        private void OnTouchChanged(double diff, double verticalDiff)
        {
            if (!_isGestureStarted || Abs(CurrentGestureDiff - diff) <= double.Epsilon)
            {
                return;
            }

            PopulateDiffItems(diff);
            var absDiff = Abs(diff);
            var absVerticalDiff = Abs(verticalDiff);
            if (!_isGestureDirectionResolved && Max(absDiff, absVerticalDiff) > CancelVerticalGestureThreshold)
            {
                absVerticalDiff *= 2.5;
                if (absVerticalDiff >= absDiff)
                {
                    _isGestureStarted = false;
                    OnTouchEnded();
                    return;
                }
                _isGestureDirectionResolved = true;
            }

            _mainView.AbortAnimation(AnimationName);
            var totalDiff = PreviousDiff + diff;
            if (!UpdateDiff(totalDiff - _zeroDiff, false))
            {
                _zeroDiff = totalDiff - Diff;
            }
        }

        private void OnTouchEnded()
        {
            if (!_isGestureStarted)
            {
                return;
            }
            _isGestureStarted = false;
            CleanDiffItems();
            if (TryResolveFlingGesture(out SideMenuViewState state))
            {
                State = state;
            }
            PreviousDiff = Diff;
            PerformAnimation();
        }

        private void PerformAnimation()
        {
            var menuWidth = _activeMenu?.Width ?? double.PositiveInfinity;
            var start = Diff;
            var end = Sign((int)State) * menuWidth;

            var length = (uint)(AnimationLength * Abs(start - end) / menuWidth);
            if(length == 0)
            {
                return;
            }
            _mainView.Animate(AnimationName,
                new Animation(v => UpdateDiff(v, true), Diff, end), AnimationRate, length, AnimationEasing);
        }

        private SideMenuViewState ResolveSwipeState(bool isRightSwipe)
        {
            var left = SideMenuViewState.LeftMenuShown;
            var right = SideMenuViewState.RightMenuShown;
            switch (State)
            {
                case SideMenuViewState.LeftMenuShown:
                    right = SideMenuViewState.Default;
                    SetActiveView(true);
                    break;
                case SideMenuViewState.RightMenuShown:
                    left = SideMenuViewState.Default;
                    SetActiveView(false);
                    break;
            }
            return isRightSwipe ? left : right;
        }

        private bool UpdateDiff(double diff, bool shouldUpdatePreviousDiff) {
            diff = Sign(diff) * Min(Abs(diff), _activeMenu?.Width ?? 0);
            SetActiveView(diff >= 0);
            if (Abs(Diff - diff) <= double.Epsilon)
            {
                return false;
            }
            Diff = diff;
            SetState(diff);
            if (shouldUpdatePreviousDiff)
            {
                PreviousDiff = diff;
            }
            _mainView.TranslationX = diff;
            return true;
        }

        private void SetState(double diff)
        {
            var menuWidth = _activeMenu?.Width ?? double.PositiveInfinity;
            var moveThreshold = (_activeMenu?.Width ?? double.PositiveInfinity) * AcceptMoveThresholdPercentage;
            var absDiff = Abs(diff);
            var state = State;
            if (Sign(diff) != (int)state)
            {
                state = SideMenuViewState.Default;
            }
            if (state == SideMenuViewState.Default && absDiff <= moveThreshold ||
                state != SideMenuViewState.Default && absDiff < menuWidth - moveThreshold)
            {
                CurrentGestureState = SideMenuViewState.Default;
                return;
            }
            if (diff >= 0)
            {
                CurrentGestureState = SideMenuViewState.LeftMenuShown;
                return;
            }
            CurrentGestureState = SideMenuViewState.RightMenuShown;
        }

        private void SetActiveView(bool isLeft)
        {
            _activeMenu = _leftMenu;
            _inactiveMenu = _rightMenu;
            if (!isLeft)
            {
                _activeMenu = _rightMenu;
                _inactiveMenu = _leftMenu;
            }
            if (_inactiveMenu != null && _activeMenu != null)
            {
                LowerChild(_inactiveMenu);
            }
        }

        private bool TryResolveFlingGesture(out SideMenuViewState state)
        {
            state = CurrentGestureState;
            if (State != state)
            {
                return true;
            }

            var lastItem = _timeDiffItems.LastOrDefault();
            var firstItem = _timeDiffItems.FirstOrDefault();
            var count = _timeDiffItems.Count;
            _timeDiffItems.Clear();

            if (count < 2)
            {
                return false;
            }

            var distDiff = lastItem.Diff - firstItem.Diff;

            if (Sign(distDiff) != Sign(lastItem.Diff))
            {
                return false;
            }

            var absDistDiff = Abs(distDiff);
            var timeDiff = lastItem.Time - firstItem.Time;

            var acceptValue = SwipeThresholdDistance * timeDiff.TotalMilliseconds / SwipeThresholdTime.TotalMilliseconds;

            if (absDistDiff < acceptValue)
            {
                return false;
            }

            state = ResolveSwipeState(distDiff > 0);
            return true;
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

        private void HandleViewAdded(View view)
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

        private void HandleViewRemoved(View view)
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
            if(_activeMenu == view)
            {
                _activeMenu = null;
            }
            if(_inactiveMenu == view)
            {
                _inactiveMenu = view;
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

        #endregion
    }
}
