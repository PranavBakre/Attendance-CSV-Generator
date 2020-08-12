using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void ChangeTheme(object sender, RoutedEventArgs e)
        {

            if (this.RequestedTheme == ElementTheme.Light)
            {
                this.RequestedTheme = ElementTheme.Dark;
            }
            else
            {
                this.RequestedTheme = ElementTheme.Light;
            }

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".csv");
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                OutputTextBlock.Text = "Picked photo: " + file.Name;
            }
            else
            {
                OutputTextBlock.Text = " Operation cancelled.";
            }
        }
    }
}
