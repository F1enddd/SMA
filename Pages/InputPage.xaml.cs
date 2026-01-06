using Microsoft.Maui.Controls;

namespace SMA.Pages;

public partial class InputPage : ContentPage
{
    private TaskCompletionSource<string?> _tcs;
    private readonly bool _isPassword;
    public bool cancelled = false;

    public InputPage(string label, bool password = false)
    {
        InitializeComponent();

        _tcs = new TaskCompletionSource<string>();
        LabelText.Text = label;
        _isPassword = password;
        InputBox.IsPassword = password;
    }

    private void OnAcceptClicked(object sender, EventArgs e)
    {
        cancelled = false;
        if (string.IsNullOrWhiteSpace(InputBox.Text))
        {
            _tcs.TrySetResult(null); 
        }
        else
        {
            _tcs.TrySetResult(InputBox.Text); 
        }

        Navigation.PopModalAsync();
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        cancelled = true;
        _tcs.TrySetResult(null); 
        Navigation.PopModalAsync();
    }

    protected override void OnDisappearing()
    {
        if (!_tcs.Task.IsCompleted)
        {
            cancelled = true;
            _tcs.TrySetResult(null);
        }

        base.OnDisappearing();
    }

    public Task<string?> ShowAsync()
    {
        Application.Current.MainPage.Navigation.PushModalAsync(this);
        return _tcs.Task;
    }

    public string GetText()
    {
        return InputBox.Text;
    }
}
