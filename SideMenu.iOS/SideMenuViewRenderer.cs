using Foundation;
using SideMenu;
using SideMenu.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using System.ComponentModel;
using static System.Math;

[assembly: ExportRenderer(typeof(SideMenuView), typeof(SideMenuViewRenderer))]
namespace SideMenu.iOS
{
    [Preserve(AllMembers = true)]
    public class SideMenuViewRenderer : VisualElementRenderer<SideMenuView>
    {
        private UISwipeGestureRecognizer _leftSwipeGesture;

        private UISwipeGestureRecognizer _rightSwipeGesture;

        public static void Preserve() { }

        public SideMenuViewRenderer()
        {
            _leftSwipeGesture = new UISwipeGestureRecognizer(OnSwiped)
            {
                Direction = UISwipeGestureRecognizerDirection.Left
            };
            AddGestureRecognizer(_leftSwipeGesture);
            _rightSwipeGesture = new UISwipeGestureRecognizer(OnSwiped)
            {
                Direction = UISwipeGestureRecognizerDirection.Right
            };
            AddGestureRecognizer(_rightSwipeGesture);
        }

        public override void AddGestureRecognizer(UIGestureRecognizer gestureRecognizer)
        {
            base.AddGestureRecognizer(gestureRecognizer);

            if (gestureRecognizer is UIPanGestureRecognizer panGestureRecognizer)
            {
                gestureRecognizer.ShouldBeRequiredToFailBy = ShouldBeRequiredToFailBy;
                gestureRecognizer.ShouldRecognizeSimultaneously = ShouldRecognizeSimultaneously;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Dispose(ref _leftSwipeGesture);
                Dispose(ref _rightSwipeGesture);
            }
            base.Dispose(disposing);
        }

        private void Dispose(ref UISwipeGestureRecognizer gestureRecognizer)
        {
            if (gestureRecognizer != null)
            {
                RemoveGestureRecognizer(gestureRecognizer);
                gestureRecognizer.Dispose();
                gestureRecognizer = null;
            }
        }

        private void OnSwiped(UISwipeGestureRecognizer gesture)
        {
            var swipeDirection = gesture.Direction == UISwipeGestureRecognizerDirection.Left
                ? SwipeDirection.Left
                : SwipeDirection.Right;

            Element?.OnSwiped(swipeDirection);
        }

        private bool ShouldBeRequiredToFailBy(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
            => IsPanGestureHandled() && otherGestureRecognizer.View != this;

        private bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
        {
            if (!(gestureRecognizer is UIPanGestureRecognizer panGesture))
            {
                return true;
            }

            var parent = Element?.Parent;
            while (parent != null)
            {
                if (parent is MasterDetailPage)
                {
                    var velocity = panGesture.VelocityInView(this);
                    return Abs(velocity.Y) > Abs(velocity.X);
                }
                parent = parent.Parent;
            }
            return !IsPanGestureHandled();
        }

        private bool IsPanGestureHandled()
            => Abs(Element?.CurrentGestureDiff ?? 0) >= Element?.GestureThreshold;
    }
}