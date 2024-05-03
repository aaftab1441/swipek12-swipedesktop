using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SwipeK12
{
    class Paginator : DocumentPaginator
    {
        Canvas pageCanvas;
        bool dualSided = false;

        public Paginator(bool dualSided)
        {
            this.dualSided = dualSided;

            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            pageCanvas = mainWindow.cnvCardFront;
        }

        public override DocumentPage GetPage(int pageNumber)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

            switch (pageNumber)
            {
                case 0:
                    pageCanvas = mainWindow.cnvCardFront;
                    break;

                case 1:
                    pageCanvas = mainWindow.cnvCardBack;
                    break;

                default:
                    pageCanvas = null;
                    break;
            }

            pageCanvas.Measure(PageSize);
            pageCanvas.UpdateLayout();

            return new DocumentPage(pageCanvas);
        }

        public override bool IsPageCountValid
        {
            get { return true; }
        }

        public override int PageCount
        {
            get
            {
                if (dualSided)
                {
                    return 2;
                }
                else
                {
                    return 1;
                }
            }
        }

        public override System.Windows.Size PageSize
        {
            get
            {
                return new Size(pageCanvas.Width, pageCanvas.Height);
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override IDocumentPaginatorSource Source
        {
            get { return null; }
        }
    }
}
