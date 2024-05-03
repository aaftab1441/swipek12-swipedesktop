using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace SwipeDesktop.Common
{
    public static class CanvasHelper
    {
        private const double defaultDpi = 96.0;

        internal static Canvas CloneToNewCanvas(System.Windows.Controls.Canvas visual)
        {

            //visual.UseLayoutRounding = true;
            //visual.BitmapScalingMode = BitmapScalingMode.NearestNeighbor;
           
            if (visual.Children.Count == 0)
            {
                return null;
            }

            Canvas print = new Canvas();
           

            if (visual != null && visual.Background is ImageBrush)
            {
                ImageBrush ib = new ImageBrush();
                BitmapImage img = (BitmapImage)((ImageBrush)visual.Background).ImageSource;
                ib.ImageSource = img;

                print.Background = ib;
                print.Background.Opacity = visual.Background.Opacity;
                
            }

            foreach (UIElement child in visual.Children)
            {
              
                if (child.GetType().Name == "Image")
                {
                    var image = (Image)child;
                    if (image.Name == "BarcodeImage")
                    {
                        var tmp = Path.GetTempPath();
                        var bmp = ((image.Source) as BitmapImage);
                        bmp.UriSource = new Uri($"{tmp}\\{image.DataContext}.png");
                    }
                }
                var xaml = System.Windows.Markup.XamlWriter.Save(child);
                try
                {
                    var deepCopy = System.Windows.Markup.XamlReader.Parse(xaml) as UIElement;
                    print.Children.Add(deepCopy);
                }
                catch (Exception ex)
                {
                    //noop skip this element for now
                }
            }

            print.UpdateLayout();

            return print;
        }

        internal static FixedDocument GetFixedDocument(Canvas front, Canvas back, PrintDialog printDialog)
        {
            System.Printing.PrintCapabilities capabilities = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket);
            Size pageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);
            Size visibleSize = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);
            FixedDocument fixedDoc = new FixedDocument();

            // If the toPrint visual is not displayed on screen we neeed to measure and arrange it.
            front.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            front.Arrange(new Rect(new Point(0, 0), front.DesiredSize));

            Size size = front.DesiredSize;

            VisualBrush vb = new VisualBrush(front);
            vb.Stretch = Stretch.None;
            vb.AlignmentX = AlignmentX.Left;
            vb.AlignmentY = AlignmentY.Top;
            vb.ViewboxUnits = BrushMappingMode.Absolute;
            vb.TileMode = TileMode.None;
            vb.Viewbox = new Rect(0, 0, visibleSize.Width, visibleSize.Height);
           
            PageContent pageContent = new PageContent();
            FixedPage page = new FixedPage();
            ((IAddChild)pageContent).AddChild(page);
            fixedDoc.Pages.Add(pageContent);
            page.Width = pageSize.Width;
            page.Height = pageSize.Height;

            Canvas canvas = new Canvas();
            //FixedPage.SetLeft(canvas, capabilities.PageImageableArea.OriginWidth);
            //FixedPage.SetTop(canvas, capabilities.PageImageableArea.OriginHeight);
            canvas.Width = visibleSize.Width;
            canvas.Height = visibleSize.Height;
            canvas.Background = vb;
            canvas.UpdateLayout();
            page.Children.Add(canvas);

            if (back != null)
            {
                VisualBrush vb2 = new VisualBrush(back);
                vb2.Stretch = Stretch.None;
                vb2.AlignmentX = AlignmentX.Left;
                vb2.AlignmentY = AlignmentY.Top;
                vb2.ViewboxUnits = BrushMappingMode.Absolute;
                vb2.TileMode = TileMode.None;
                vb2.Viewbox = new Rect(0, 0, visibleSize.Width, visibleSize.Height);

                PageContent pageContent2 = new PageContent();
                FixedPage page2 = new FixedPage();
                ((IAddChild)pageContent2).AddChild(page2);
                fixedDoc.Pages.Add(pageContent2);
                page2.Width = pageSize.Width;
                page2.Height = pageSize.Height;

                Canvas canvas2 = new Canvas();
                //FixedPage.SetLeft(canvas2, capabilities.PageImageableArea.OriginWidth);
                //FixedPage.SetTop(canvas2, capabilities.PageImageableArea.OriginHeight);
                canvas.Width = visibleSize.Width;
                canvas.Height = visibleSize.Height;
                canvas.Background = vb2;
                page2.Children.Add(canvas2);
            }

            return fixedDoc;
        }
        internal static FixedPage CopyToFixedPage(System.Windows.Controls.Canvas visual, FixedPage page)
        {

            //visual.UseLayoutRounding = true;
            //visual.BitmapScalingMode = BitmapScalingMode.NearestNeighbor;

            if (visual.Children.Count == 0)
            {
                return null;
            }

            foreach (UIElement child in visual.Children)
            {
                var xaml = System.Windows.Markup.XamlWriter.Save(child);
                try
                {
                    var deepCopy = System.Windows.Markup.XamlReader.Parse(xaml) as UIElement;
                    page.Children.Add(deepCopy);
                }
                catch (Exception ex)
                {
                    //noop skip this element for now
                }
            }
            return page;
        }
        internal static BitmapSource SnapShotPNG(UIElement source)
        {
            BitmapFrame bf;
            double actualWidth = source.RenderSize.Width;
            double actualHeight = source.RenderSize.Height;

            RenderTargetBitmap renderTarget = new RenderTargetBitmap((int)actualWidth, (int)actualHeight, 96, 96, PixelFormats.Pbgra32);
            DrawingVisual visual = new DrawingVisual();

            //RenderOptions.SetBitmapScalingMode
            RenderOptions.SetBitmapScalingMode(renderTarget, BitmapScalingMode.NearestNeighbor);   // This forces the scaling to be on even-pixel boundaries
            RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.NearestNeighbor);   // This forces the scaling to be on even-pixel boundaries
            RenderOptions.SetBitmapScalingMode(source, BitmapScalingMode.NearestNeighbor);   // This forces the scaling to be on even-pixel boundaries


            using (DrawingContext context = visual.RenderOpen())
            {
                VisualBrush sourceBrush = new VisualBrush(source);
                context.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0), new Point(actualWidth, actualHeight)));
            }
            source.Measure(source.RenderSize); //Important
            source.Arrange(new Rect(source.RenderSize)); //Important

            renderTarget.Render(visual);

            BitmapEncoder encoder = new PngBitmapEncoder();
            if (renderTarget != null)
            {
                bf = BitmapFrame.Create((BitmapSource)renderTarget);
                encoder.Frames.Add(bf);
                using (FileStream stream = new FileStream(@"C:\\tmp\test.png", FileMode.Create))
                {
                    encoder.Save(stream);
                }
            }

            try
            {
                return renderTarget;
                //return new CroppedBitmap(renderTarget, new Int32Rect(0, 0, (int)actualWidth, (int)actualHeight));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        internal static RenderTargetBitmap DrawCanvasImage(Visual targetControl)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(targetControl);


            double scaleX = 600 / 96.0;
            double scaleY = 600 / 96.0;

            RenderTargetBitmap rtb = new RenderTargetBitmap((int)(bounds.Width * scaleX),
                                                           (int)(bounds.Height * scaleY),
                                                           scaleX * 96.0,
                                                           scaleY * 96.0,
                                                           PixelFormats.Pbgra32);
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(targetControl);
                dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
            }

            rtb.Render(dv);

            return rtb;
        }

        internal static Size MeasureString(TextBox textblock, string candidate)
        {
            var formattedText = new FormattedText(
                candidate,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textblock.FontFamily, textblock.FontStyle, textblock.FontWeight, textblock.FontStretch),
                textblock.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1);

            return new Size(formattedText.Width, formattedText.Height);
        }

        internal static BitmapSource GetRenderTargetBitmapFromControl(Visual targetControl, double dpi = defaultDpi)
        {
            BitmapFrame bf;
            var bounds = VisualTreeHelper.GetDescendantBounds(targetControl);

            var drawingVisual = new DrawingVisual();
            var renderTargetBitmap = new RenderTargetBitmap((int)(bounds.Width * dpi / 96.0),
                                                           (int)(bounds.Height * dpi / 96.0),
                                                           dpi,
                                                           dpi,
                                                           PixelFormats.Pbgra32);

            RenderOptions.SetBitmapScalingMode(targetControl, BitmapScalingMode.NearestNeighbor);   // This forces the scaling to be on even-pixel boundaries
            RenderOptions.SetBitmapScalingMode(drawingVisual, BitmapScalingMode.NearestNeighbor);   // This forces the scaling to be on even-pixel boundaries
            RenderOptions.SetBitmapScalingMode(renderTargetBitmap, BitmapScalingMode.NearestNeighbor);   // This forces the scaling to be on even-pixel boundaries



            if (targetControl == null) return null;

         

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                var visualBrush = new VisualBrush(targetControl);
                drawingContext.DrawRectangle(visualBrush, null, new Rect(new Point(), bounds.Size));
            }

            renderTargetBitmap.Render(drawingVisual);

            BitmapEncoder encoder = new BmpBitmapEncoder();

            if (renderTargetBitmap != null)
            {
                bf = BitmapFrame.Create(renderTargetBitmap);
                encoder.Frames.Add(bf);

                using (FileStream stream = new FileStream(@"C:\\tmp\test.bmp", FileMode.Create))
                {
                    encoder.Save(stream);
                }
            }
            return renderTargetBitmap;
        }

        internal static BitmapImage Draw2dBarcode(string barcode)
        {
            var qrcode = new QRCodeWriter();
            var qrValue = barcode;

            var barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = 120,
                    Width = 120,
                    Margin = 1
                }
            };

            using (var bitmap = barcodeWriter.Write(qrValue))
            using (var stream = new MemoryStream())
            {
                var tmp = Path.GetTempPath();
                bitmap.Save($"{tmp}\\{barcode}.png", ImageFormat.Png);
                bitmap.Save(stream, ImageFormat.Png);

                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                stream.Seek(0, SeekOrigin.Begin);
                //bi.UriSource = $".\\{barcode}";
                bi.StreamSource = stream;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();

                return bi;
            }

        }

        internal static BitmapImage Draw2dBarcode(string barcode, double height, double width)
        {
            var qrcode = new QRCodeWriter();
            var qrValue = barcode;

            var barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = int.Parse(Math.Round(height).ToString()),
                    Width = int.Parse(Math.Round(width).ToString()),
                    Margin = 1
                }
            };

            using (var bitmap = barcodeWriter.Write(qrValue))
            using (var stream = new MemoryStream())
            {
                var tmp = Path.GetTempPath();
                bitmap.Save($"{tmp}\\{barcode}.png", ImageFormat.Png);
                bitmap.Save(stream, ImageFormat.Png);

                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                stream.Seek(0, SeekOrigin.Begin);
                //bi.UriSource = $".\\{barcode}";
                bi.StreamSource = stream;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();

                return bi;
            }

        }
    }
}
