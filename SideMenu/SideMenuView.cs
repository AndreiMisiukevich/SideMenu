using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using static System.Math;
using static Xamarin.Forms.Device;
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

        private static readonly Easing AnimationEasing = Easing.SinOut;

        private static readonly TimeSpan SwipeThresholdTime = TimeSpan.FromMilliseconds(RuntimePlatform == Android ? 100 : 60);

        #endregion

        #region Public Bindable Properties

        public static readonly BindableProperty DiffProperty = BindableProperty.Create(nameof(Diff), typeof(double), typeof(SideMenuView), 0.0, BindingMode.OneWayToSource);

        public static readonly BindableProperty GestureThresholdProperty = BindableProperty.Create(nameof(GestureThreshold), typeof(double), typeof(SideMenuView), 7.0);

        public static readonly BindableProperty CancelVerticalGestureThresholdProperty = BindableProperty.Create(nameof(CancelVerticalGestureThreshold), typeof(double), typeof(SideMenuView), 1.0);

        public static readonly BindableProperty ShouldThrottleGestureProperty = BindableProperty.Create(nameof(ShouldThrottleGesture), typeof(bool), typeof(SideMenuView), false);

        public static readonly BindableProperty StateProperty = BindableProperty.Create(nameof(State), typeof(SideMenuViewState), typeof(SideMenuView), SideMenuViewState.Default, BindingMode.TwoWay,
            propertyChanged: (bindable, oldValue, newValue) => ((SideMenuView)bindable).PerformAnimation());

        #endregion

        #region Public Attached Properties

        public static readonly BindableProperty PlaceProperty = BindableProperty.CreateAttached(nameof(GetPlace), typeof(SideMenuViewPlace), typeof(SideMenuView), SideMenuViewPlace.MainView);

        public static readonly BindableProperty MenuWidthPercentageProperty = BindableProperty.CreateAttached(nameof(GetMenuWidthPercentage), typeof(double), typeof(SideMenuView), -1.0);

        public static readonly BindableProperty MenuGestureEnabledProperty = BindableProperty.CreateAttached(nameof(GetMenuGestureEnabled), typeof(bool), typeof(SideMenuView), true);

        #endregion

        #region Private Fields

        private readonly PanGestureRecognizer _panGesture = new PanGestureRecognizer();

        private readonly List<TimeDiffItem> _timeDiffItems = new List<TimeDiffItem>();

        private readonly View _overlayView;

        private View _mainView;

        private View _leftMenu;

        private View _rightMenu;

        private View _activeMenu;

        private View _inactiveMenu;

        private double _zeroDiff;

        private bool _isGestureStarted;

        private bool _isGestureDirectionResolved;

        private bool _isSwipe;

        #endregion

        #region Public Constructors

        public SideMenuView()
        {
            _overlayView = SetupMainViewLayout(new BoxView
            {
                InputTransparent = true,
                GestureRecognizers =
                {
                    new TapGestureRecognizer
                    {
                        Command = new Command(() => State = SideMenuViewState.Default)
                    }
                }
            });
            Children.Add(_overlayView);

            if (RuntimePlatform == Android)
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
                    if (RuntimePlatform == Android)
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
            if(RuntimePlatform == Android)
            {
                return;
            }

            await Task.Delay(1);
            if (_isGestureStarted)
            {
                return;
            }

            UpdateState(ResolveSwipeState(swipeDirection == SwipeDirection.Right), true);
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

        public static double GetMenuWidthPercentage(BindableObject bindable)
            => (double)bindable.GetValue(MenuWidthPercentageProperty);

        public static void SetMenuWidthPercentage(BindableObject bindable, double value)
            => bindable.SetValue(MenuWidthPercentageProperty, value);

        public static bool GetMenuGestureEnabled(BindableObject bindable)
            => (bool)bindable.GetValue(MenuGestureEnabledProperty);

        public static void SetMenuGestureEnabled(BindableObject bindable, bool value)
            => bindable.SetValue(MenuGestureEnabledProperty, value);

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
            RaiseChild(_overlayView);
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
            if (!TryUpdateDiff(totalDiff - _zeroDiff, false))
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

            PreviousDiff = Diff;
            var state = State;
            var isSwipe = TryResolveFlingGesture(ref state);
            PopulateDiffItems(0);
            _timeDiffItems.Clear();
            UpdateState(state, isSwipe);
        }

        private void PerformAnimation()
        {
            var state = State;
            var start = Diff;
            var menuWidth = (state == SideMenuViewState.LeftMenuShown ? _leftMenu : _rightMenu)?.Width ?? 0;
            var end = Sign((int)state) * menuWidth;

            var animationLength = (uint)(AnimationLength * Abs(start - end) / Width);
            if (_isSwipe)
            {
                _isSwipe = false;
                animationLength /= 2;
            }
            if (animationLength == 0)
            {
                return;
            }
            var animation = new Animation(v => TryUpdateDiff(v, true), Diff, end);
            _mainView.Animate(AnimationName, animation, AnimationRate, animationLength, AnimationEasing, (v, isCanceled) =>
            {
                if (isCanceled)
                {
                    return;
                }
                _overlayView.InputTransparent = state == SideMenuViewState.Default;
            });
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

        private bool TryUpdateDiff(double diff, bool shouldUpdatePreviousDiff) {
            SetActiveView(diff >= 0);
            if(_activeMenu == null || !GetMenuGestureEnabled(_activeMenu))
            {
                return false;
            }
            diff = Sign(diff) * Min(Abs(diff), _activeMenu.Width);
            if (Abs(Diff - diff) <= double.Epsilon)
            {
                return false;
            }
            Diff = diff;
            SetCurrentGestureState(diff);
            if (shouldUpdatePreviousDiff)
            {
                PreviousDiff = diff;
            }
            _mainView.TranslationX = diff;
            _overlayView.TranslationX = diff;
            return true;
        }

        private void SetCurrentGestureState(double diff)
        {
            var menuWidth = _activeMenu?.Width ?? Width;
            var moveThreshold = menuWidth * AcceptMoveThresholdPercentage;
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

        private void UpdateState(SideMenuViewState state, bool isSwipe)
        {
            _isSwipe = isSwipe;
            if (State == state)
            {
                PerformAnimation();
                return;
            }
            State = state;
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
            if (_inactiveMenu == null ||
                _activeMenu == null ||
                _leftMenu.X + _leftMenu.Width <= _rightMenu.X ||
                Children.IndexOf(_inactiveMenu) < Children.IndexOf(_activeMenu))
            {
                return;
            }
            LowerChild(_inactiveMenu);
        }

        private bool TryResolveFlingGesture(ref SideMenuViewState state)
        {
            if (state != CurrentGestureState)
            {
                state = CurrentGestureState;
                return false;
            }

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

            if (_timeDiffItems.Count >= 25)
            {
                CleanDiffItems();
            }

            _timeDiffItems.Add(new TimeDiffItem
            {
                Time = DateTime.UtcNow,
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
                    _mainView = SetupMainViewLayout(view);
                    break;
                case SideMenuViewPlace.LeftMenu:
                    _leftMenu = SetupMenuLayout(view, true);
                    break;
                case SideMenuViewPlace.RightMenu:
                    _rightMenu = SetupMenuLayout(view, false);
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
                _inactiveMenu = null;
            }
        }

        private View SetupMainViewLayout(View view)
        {
            SetLayoutFlags(view, AbsoluteLayoutFlags.All);
            SetLayoutBounds(view, new Rectangle(0, 0, 1, 1));
            return view;
        }

        private View SetupMenuLayout(View view, bool isLeft)
        {
            var width = GetMenuWidthPercentage(view);
            var flags = width > 0
                ? AbsoluteLayoutFlags.All
                : AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.HeightProportional;
            SetLayoutFlags(view, flags);
            SetLayoutBounds(view, new Rectangle(isLeft ? 0: 1, 0, width, 1));
            return view;
        }

        #endregion
    }
}
