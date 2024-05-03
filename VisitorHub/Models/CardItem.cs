using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SwipeK12
{
    public class CardItem
    {
        public string Side { get; set; }
        public double Top { get; set; }
        public double Left { get; set; }
        public int ZIndex { get; set; }
        public string ControlType { get; set; }
        public string FieldType { get; set; }
        public string Name { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Foreground { get; set; }
        public string Background { get; set; }
        public string Source { get; set; }
        public ImageSource ImageSource { get; set; }
        public string Text { get; set; }
        public string Alignment { get; set; }
        public string TextFont { get; set; }
        public double TextSize { get; set; }
        public bool TextBold {get; set;}
        public bool TextItalic { get; set; }
        public bool TextUnderline { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
