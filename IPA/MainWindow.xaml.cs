using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace IPA
{
    public partial class MainWindow : Window
    {
        private const string FileFilter =
            "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png, *.bmp) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png; *.bmp";

        private Bitmap _loadedImage;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Load_Image(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = FileFilter
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            using var bitmap = new Bitmap(openFileDialog.FileName);
            _loadedImage = (Bitmap)bitmap.Clone();

            Image1.Source = GetBitMapSource(bitmap);
        }

        private void ImageToBinary(object sender, RoutedEventArgs e)
        {
            IpaService.ImageToBinary(_loadedImage);
            Image2.Source = GetBitMapSource(_loadedImage);
        }
        
        private void GetLinkedAreas(object sender, RoutedEventArgs e)
        {
            IpaService.GetLinkedAreas(_loadedImage);
            Image2.Source = GetBitMapSource(_loadedImage);
        }

        private static BitmapSource? GetBitMapSource(Bitmap? bitmap)
        {
            if (bitmap != null)
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

            return null;
        }
    }
}