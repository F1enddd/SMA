using System;
using System.Collections.Generic;

namespace SMA.Pages
{
    public partial class ListInputPage : ContentPage
    {
        private readonly TaskCompletionSource<int?> _tcs = new();

        public Task<int?> Result => _tcs.Task;

        public ListInputPage(List<string> options)
        {
            InitializeComponent();
            ItemsList.ItemsSource = options;
        }

        private void OnAcceptClicked(object sender, EventArgs e)
        {
            var selected = ItemsList.SelectedItem;

            if (selected == null)
            {
                _tcs.TrySetResult(null);
                return;
            }

            int index = ((List<string>)ItemsList.ItemsSource).IndexOf(selected.ToString());
            _tcs.TrySetResult(index);
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            _tcs.TrySetResult(null);
        }
    }
}
