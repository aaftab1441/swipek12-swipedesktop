using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using log4net;

namespace SwipeK12
{
    class CanvasUtils
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public static void BringToFront(Canvas pParent, UIElement pToMove)
        {
            try
            {
                int currentIndex = Canvas.GetZIndex(pToMove);
                int zIndex = 0;
                int maxZ = 0;
                UIElement child;
                for (int i = 0; i < pParent.Children.Count; i++)
                {
                    if (pParent.Children[i] is UIElement &&
                        pParent.Children[i] != pToMove)
                    {
                        child = pParent.Children[i] as UIElement;
                        zIndex = Canvas.GetZIndex(child);
                        maxZ = Math.Max(maxZ, zIndex);
                        if (zIndex > currentIndex)
                        {
                            Canvas.SetZIndex(child, zIndex - 1);
                        }
                    }
                }
                Canvas.SetZIndex(pToMove, maxZ);
                pParent.UpdateLayout();
            }
            catch (Exception ex)
            {
                log.Error("Error bringing UI element to front:", ex);
            }
        }

        public static int GetMaxZindex(Canvas canvas)
        {
            int maxIndex = 0;

            if (canvas != null)
            {
                foreach (UIElement element in canvas.Children)
                {
                    maxIndex = Math.Max(maxIndex, Canvas.GetZIndex(element));
                }
            }

            return maxIndex;
        }
    }
}
