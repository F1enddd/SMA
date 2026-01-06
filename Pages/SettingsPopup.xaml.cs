using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

namespace SMA.Pages;

public partial class SettingsPopup : Popup
{
	public SettingsPopup()
	{
		InitializeComponent();
	}

    private void Stepper_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        lblInterval.Text = $"Seconds between checking: {(int)e.NewValue}";
    }

    private void btnSave_Clicked(object sender, EventArgs e)
    {
        this.CloseAsync();
    }
}