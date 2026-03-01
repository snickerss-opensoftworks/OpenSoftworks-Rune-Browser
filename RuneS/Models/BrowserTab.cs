using System;
using System.ComponentModel;
using Microsoft.Web.WebView2.Wpf;

namespace RuneS.Models
{
    public class BrowserTab : INotifyPropertyChanged
    {
        private string _title  = "New Tab";
        private string _url    = string.Empty;
        private bool   _isLoading;
        private bool   _isSelected;
        private bool   _canGoBack;
        private bool   _canGoForward;
        private bool   _isSecure;
        private bool   _isMuted;

        public Guid    Id      { get; } = Guid.NewGuid();
        public WebView2 WebView { get; set; }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); OnPropertyChanged(nameof(DisplayTitle)); }
        }

        public string DisplayTitle
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_title)) return "New Tab";
                return _title.Length > 24 ? _title.Substring(0, 23) + "…" : _title;
            }
        }

        public string Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(nameof(Url)); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public bool CanGoBack
        {
            get => _canGoBack;
            set { _canGoBack = value; OnPropertyChanged(nameof(CanGoBack)); }
        }

        public bool CanGoForward
        {
            get => _canGoForward;
            set { _canGoForward = value; OnPropertyChanged(nameof(CanGoForward)); }
        }

        public bool IsSecure
        {
            get => _isSecure;
            set { _isSecure = value; OnPropertyChanged(nameof(IsSecure)); }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set { _isMuted = value; OnPropertyChanged(nameof(IsMuted)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
