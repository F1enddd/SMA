using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using SteamAuth;

namespace SMA.Pages;

public partial class TradePopupPage : Popup
{
    private SteamGuardAccount acc;
    private List<Confirmation> confirms = new List<Confirmation>();
    private bool deny2, accept2;

    public TradePopupPage()
    {
        InitializeComponent();
        lblStatus.Text = "";
    }

    public SteamGuardAccount Account
    {
        get => acc;
        set
        {
            acc = value;
            lblAccount.Text = acc.AccountName;
        }
    }

    public Confirmation[] Confirmations
    {
        get => confirms.ToArray();
        set => confirms = new List<Confirmation>(value);
    }

    private void btnAccept_Clicked(object sender, EventArgs e)
    {
        if (!accept2)
        {
            lblStatus.Text = "Press Accept again to confirm";
            btnAccept.BackgroundColor = Colors.LightGreen;
            accept2 = true;
        }
        else
        {
            lblStatus.Text = "Accepting...";
            if (confirms.Count > 0)
            {
                acc.AcceptConfirmation(confirms[0]);
                confirms.RemoveAt(0);
            }
            Reset();
        }
    }

    private void btnDeny_Clicked(object sender, EventArgs e)
    {
        if (!deny2)
        {
            lblStatus.Text = "Press Deny again to confirm";
            btnDeny.BackgroundColor = Colors.LightYellow;
            deny2 = true;
        }
        else
        {
            lblStatus.Text = "Denying...";
            if (confirms.Count > 0)
            {
                acc.DenyConfirmation(confirms[0]);
                confirms.RemoveAt(0);
            }
            Reset();
        }
    }

    private void Reset()
    {
        deny2 = false;
        accept2 = false;

        btnAccept.BackgroundColor = Colors.LightGreen;
        btnDeny.BackgroundColor = Colors.LightYellow;

        btnAccept.Text = "Accept";
        btnDeny.Text = "Deny";
        lblAccount.Text = "";
        lblStatus.Text = "";

        if (confirms.Count == 0)
        {
            this.CloseAsync();
        }
        else
        {
            lblDesc.Text = "Confirmation";
        }
    }

    public void ShowPopup(Page page)
    {
        Reset();
        page.ShowPopup(this);
    }
}
