

namespace SwipeDesktop.Assets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;
    
    internal class ModalAdorner : Adorner
    {
        public ModalAdorner(UIElement adornerElement)
            : base(adornerElement)
        { }
        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(new SolidColorBrush(), null, WindowRect());
            base.OnRender(drawingContext);
        }

        protected override Geometry GetLayoutClip(Size layoutSlotSize)
        {
            // Add a group that includes the whole window except the adorned control
            GeometryGroup grp = new GeometryGroup();
            grp.Children.Add(new RectangleGeometry(WindowRect()));
            grp.Children.Add(new RectangleGeometry(new Rect(layoutSlotSize)));

            return grp;
        }

        private Rect WindowRect()
        {
            if (AdornedElement == null)
            {
                throw new ArgumentException("cannot adorn a null control");
            }
            else
            {
                // Get a point of the offset of the window
                Window window = Application.Current.MainWindow;
                Point windowOffset;

                if (window == null)
                {
                    throw new ArgumentException("can't get main window");

                }
                else
                {
                    GeneralTransform transformToAncestor = AdornedElement.TransformToAncestor(window);
                    if (transformToAncestor == null || transformToAncestor.Inverse == null)
                    {
                        throw new ArgumentException("no transform to window");
                    }
                    else
                    {
                        windowOffset = transformToAncestor.Inverse.Transform(new Point(0, 0));
                    }
                }

                // Get a point of the lower-right corner of the window
                Point windowLowerRight = windowOffset;
                windowLowerRight.Offset(window.ActualWidth, window.ActualHeight);
                return new Rect(windowOffset, windowLowerRight);
            }
        }

    }

}
