using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ReactiveUI;
using SwipeDesktop.Interfaces;
using SwipeK12;

namespace SwipeDesktop.ViewModels
{
    public class IdCardDesignerViewModel : ReactiveObject, IViewModel
    {

        /*
        public void SaveCard()
        {
            List<CardItem> cardItems = new List<CardItem>();
            BuildCardItemList(cnvCardFront, cardItems, App.APP_CARD_FRONT);
            BuildCardItemList(cnvCardBack, cardItems, App.APP_CARD_BACK);

            string frontBG = getCanvasBackgroundFile(cnvCardFront);
            string backBG = getCanvasBackgroundFile(cnvCardBack);

            var xml = Serialisation.SerializeObject<List<CardItem>>(cardItems);

            // Remove leading invalid characters that may exist due to encoding conflicts
            if (xml.Length > 0 && xml[0] != '<')
            {
                xml = xml.Substring(1, xml.Length - 1);
            }

            int recUpdated = scBLL.UpdateCardByIdAndSchool(frontBG, cnvCardFront.Background.Opacity, backBG, cnvCardBack.Background.Opacity, isCardDualSided(), xml, true, App.getCurrentCardID(), App.getCurrentSchoolID());

            if (recUpdated == 1)
            {
                MessageBox.Show("Card '" + txtBlkCardName.Text + "' saved succesfully.", "Card Saved", MessageBoxButton.OK);
            }
            else
            {
                MessageBox.Show("Card '" + txtBlkCardName.Text + "' save failed.", "Card Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        */

        private void BuildCardItemList(Canvas c, List<CardItem> cardItems, string side)
        {
            foreach (UIElement uie in c.Children)
            {
                CardItem item = new CardItem();
                item.Side = side;
                item.Top = Canvas.GetTop(uie);
                item.Left = Canvas.GetLeft(uie);
                item.ZIndex = Canvas.GetZIndex(uie);

                if (uie is Label)
                {
                    Label lbl = (Label)uie;
                    item.ControlType = lbl.GetType().ToString();
                    item.FieldType = lbl.Name;

                    if (!Double.IsNaN(lbl.Width))
                    {
                        item.Width = lbl.Width;
                    }
                    if (!Double.IsNaN(lbl.Height))
                    {
                        item.Height = lbl.Height;
                    }

                    item.Foreground = SwipeUtils.GetColor(lbl.Foreground).ToString();
                    item.Background = SwipeUtils.GetColor(lbl.Background).ToString();
                    item.TextFont = lbl.FontFamily.Source;
                    item.TextSize = lbl.FontSize;
                    item.TextBold = lbl.FontWeight.Equals(FontWeights.Bold);
                    item.TextItalic = lbl.FontStyle.Equals(FontStyles.Italic);
                }
                else if (uie is Image)
                {
                    Image img = (Image)uie;
                    item.ControlType = img.GetType().ToString();
                    item.FieldType = img.Name;
                    item.Source = (img.Source as BitmapImage).UriSource.AbsoluteUri;

                    if (!Double.IsNaN(img.Width))
                    {
                        item.Width = img.Width;
                    }
                    if (!Double.IsNaN(img.Height))
                    {
                        item.Height = img.Height;
                    }
                }
                else if (uie is TextBox)
                {
                    TextBox txt = (TextBox)uie;
                    item.ControlType = txt.GetType().ToString();
                    item.FieldType = txt.Name;

                    if (!Double.IsNaN(txt.Width))
                    {
                        item.Width = txt.Width;
                    }
                    if (!Double.IsNaN(txt.Height))
                    {
                        item.Height = txt.Height;
                    }

                    item.Foreground = SwipeUtils.GetColor(txt.Foreground).ToString();
                    item.Background = SwipeUtils.GetColor(txt.Background).ToString();
                    item.Text = txt.Text;
                    item.Alignment = txt.TextAlignment.ToString();
                    item.TextFont = txt.FontFamily.Source;
                    item.TextSize = txt.FontSize;
                    item.TextBold = txt.FontWeight.Equals(FontWeights.Bold);
                    item.TextItalic = txt.FontStyle.Equals(FontStyles.Italic);
                    item.TextUnderline = txt.TextDecorations.Contains(TextDecorations.Underline[0]);
                }
                else if (uie is TextBlock)
                {
                    TextBlock tb = (TextBlock)uie;
                    item.ControlType = tb.GetType().ToString();
                    item.FieldType = tb.Name;

                    if (!Double.IsNaN(tb.Width))
                    {
                        item.Width = tb.Width;
                    }
                    if (!Double.IsNaN(tb.Height))
                    {
                        item.Height = tb.Height;
                    }

                    item.Foreground = SwipeUtils.GetColor(tb.Foreground).ToString();
                    item.Background = SwipeUtils.GetColor(tb.Background).ToString();
                    item.Alignment = tb.TextAlignment.ToString();
                    item.TextFont = tb.FontFamily.Source;
                    item.TextSize = tb.FontSize;
                    item.TextBold = tb.FontWeight.Equals(FontWeights.Bold);
                    item.TextItalic = tb.FontStyle.Equals(FontStyles.Italic);
                    item.TextUnderline = tb.TextDecorations.Contains(TextDecorations.Underline[0]);
                }

                cardItems.Add(item);
            }
        }
    }
}
