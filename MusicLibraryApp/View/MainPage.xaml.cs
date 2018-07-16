using MusicLibraryApp.AppDialogs;
using MusicLibraryApp.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MusicLibraryApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MainViewModel vm;
        private MediaSource _mediaSource;

        public MainPage()
        {
            this.InitializeComponent();
            vm = new MainViewModel();
            vm.GetAllSongs();
            //to display all songs
            this.DataContext = vm;

        }

        private async void SoundGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Song songInContext = (Song)e.ClickedItem;

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await localFolder.GetFileAsync(songInContext.SongFileName);
            if (file != null)
            {
                _mediaSource = MediaSource.CreateFromStorageFile(file);
                this.mediaPlayer.SetPlaybackSource(_mediaSource);
                this.mediaPlayer.AutoPlay = true;
            }
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
            Search.Visibility = MySplitView.IsPaneOpen ? Visibility.Collapsed : Visibility.Visible;

        }
         
            private void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
            {
                if (SearchAutoSuggestBox.Text.Trim() != "")
                {
                    vm.SearchSongs(SearchAutoSuggestBox.Text);
                    this.SongGridView.ItemsSource = vm.songsList;
                }
                else {
                    vm.GetAllSongs();
                    this.SongGridView.ItemsSource = vm.songsList;
                }
                MySplitView.IsPaneOpen = false;
                Search.Visibility = Visibility.Visible;

            }

            private void DisplaySongList_Click(object sender, RoutedEventArgs e)
            {
                vm.GetAllSongs();
                this.DataContext = vm;
            }

            private async void AddSongButton_Click(object sender, RoutedEventArgs e)
            {
                var dialog = new ContentDialog1();
                await dialog.ShowAsync();
            }

            private void SongGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {

            }

            private void Search_Click(object sender, RoutedEventArgs e)
            {
                MySplitView.IsPaneOpen = true;
                Search.Visibility = Visibility.Collapsed;
            }
        }
    }

