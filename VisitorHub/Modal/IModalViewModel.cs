

using ReactiveUI;


namespace SwipeDesktop.Modal
{
    public interface IModalViewModel
    {
        bool ShowPopup { get; set; }
        int Height { get; set; }
        int Width { get; set; }
        int HorizontalOffset { get; set; }
        ReactiveCommand<object> HidePopup { get; }
    }
}
