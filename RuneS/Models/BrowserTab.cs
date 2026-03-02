using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace RuneS.Models
{
    public class BrowserTab : INotifyPropertyChanged
    {
        private string _title = "New Tab";
        private string _url = string.Empty;
        private bool _isLoading;
        private bool _isSelected;
        private bool _isActive;
        private bool _canGoBack;
        private bool _canGoForward;
        private bool _isMuted;
        private bool _isPinned;
        private BitmapImage _favicon;

        public Guid Id { get; } = Guid.NewGuid();

        // Reference to the UI element (tab button)
        public Button ChipElement { get; set; }

        public string Title
        {
            get => _title;
            set { _title = value; N(nameof(Title)); N(nameof(DisplayTitle)); }
        }

        public string DisplayTitle
        {
            get
            {
                var t = string.IsNullOrWhiteSpace(_title) ? "New Tab" : _title;
                return t.Length > 26 ? t.Substring(0, 25) + "\u2026" : t;
            }
        }

        public string Url
        {
            get => _url;
            set { _url = value; N(nameof(Url)); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; N(nameof(IsLoading)); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; N(nameof(IsSelected)); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; N(nameof(IsActive)); }
        }

        public bool CanGoBack
        {
            get => _canGoBack;
            set { _canGoBack = value; N(nameof(CanGoBack)); }
        }

        public bool CanGoForward
        {
            get => _canGoForward;
            set { _canGoForward = value; N(nameof(CanGoForward)); }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set { _isMuted = value; N(nameof(IsMuted)); }
        }

        public bool IsPinned
        {
            get => _isPinned;
            set { _isPinned = value; N(nameof(IsPinned)); }
        }

        public BitmapImage Favicon
        {
            get => _favicon;
            set { _favicon = value; N(nameof(Favicon)); N(nameof(HasFavicon)); }
        }

        public bool HasFavicon => _favicon != null;

        public event PropertyChangedEventHandler PropertyChanged;
        private void N(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}