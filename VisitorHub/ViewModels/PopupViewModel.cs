using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using ReactiveUI;

using SwipeDesktop.Common;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Modal;

namespace SwipeDesktop.ViewModels
{
    public class PopupViewModel : ReactiveObject, IModalViewModel
    {
        public PopupViewModel()
        {
            Height = 300;
            Width = 300;
            //HorizontalOffset
            CurrentContent = new NullViewModel();

            HidePopup = this.WhenAny(x => x.CancelText, x => !string.IsNullOrEmpty(x.Value)).ToCommand();
            HidePopup.Subscribe(x => HideAction(this));

            ConfirmPopup = this.WhenAny(x => x.ConfirmText, x => !string.IsNullOrEmpty(x.Value)).ToCommand();
            ConfirmPopup.Subscribe(x =>
            {
                SaveAction(this);
            });

            TakePhoto = this.WhenAny(x => x.ShowPhotoButton, x => x.Value).ToCommand();
            TakePhoto.Subscribe(x => ShowPhotoAction(this));

            this.WhenAnyValue(x => x.ConfirmText).Select(x => !string.IsNullOrEmpty(x)).ToProperty(this, x => x.ShowConfirmButton, out _showConfirm);
            this.WhenAnyValue(x => x.CurrentContent).Where(x=>x != null).Select(x => x.GetType() != typeof(NullViewModel)).Subscribe(x =>
            {
                ShowPopup = true;
            });


        }

        public Action<object> HideAction { get; set; }
        public Action<object> SaveAction { get; set; }
        public Action<object> ShowPhotoAction { get; set; }

        IViewModel _content;
        public IViewModel CurrentContent
        {
            get { return _content; }
            set { 
                this.RaiseAndSetIfChanged(ref _content, value); 
            }
        }


        private bool _showPopup;
        public bool ShowPopup
        {
            get { return _showPopup; }
            set { this.RaiseAndSetIfChanged(ref _showPopup, value); }
        }


        private PlacementMode _placement;
        public PlacementMode Placement
        {
            get { return _placement; }
            set { this.RaiseAndSetIfChanged(ref _placement, value); }
        }

        private int _height;
        public int Height
        {
            get { return _height; }
            set { this.RaiseAndSetIfChanged(ref _height, value); }
        }

        private int _width;
        public int Width
        {
            get { return _width; }
            set { this.RaiseAndSetIfChanged(ref _width, value); }
        }

        private int _hOffset;
        public int HorizontalOffset
        {
            get { return _hOffset; }
            set { this.RaiseAndSetIfChanged(ref _hOffset, value); }
        }

        private int _vOffset;
        public int VerticalOffset
        {
            get { return _vOffset; }
            set { this.RaiseAndSetIfChanged(ref _vOffset, value); }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { this.RaiseAndSetIfChanged(ref _title, value); }
        }

        private string _confirm;
        public string ConfirmText
        {
            get { return _confirm; }
            set { this.RaiseAndSetIfChanged(ref _confirm, value); }
        }

        ObservableAsPropertyHelper<bool> _showConfirm;
        public bool ShowConfirmButton
        {
            get { return _showConfirm.Value; }
        }

        private bool _showPhoto;
        public bool ShowPhotoButton
        {
            get { return _showPhoto; }
            set { this.RaiseAndSetIfChanged(ref _showPhoto, value); }
        }

        private string _cancel;
        public string CancelText
        {
            get { return _cancel; }
            set { this.RaiseAndSetIfChanged(ref _cancel, value); }
        }

        public ReactiveCommand<object> HidePopup { get; private set; }
        public ReactiveCommand<object> ConfirmPopup { get; private set; }
        public ReactiveCommand<object> TakePhoto { get; private set; }

    }
}
