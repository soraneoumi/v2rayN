using v2rayN.Desktop.Base;
using v2rayN.Desktop.Common;

namespace v2rayN.Desktop.Views;

public partial class AddServer2Window : WindowBase<AddServer2ViewModel>
{
    public AddServer2Window()
    {
        InitializeComponent();
    }

    public AddServer2Window(ProfileItem profileItem)
    {
        InitializeComponent();

        Loaded += Window_Loaded;
        btnCancel.Click += (s, e) => Close();
        ViewModel = new AddServer2ViewModel(profileItem, UpdateViewHandler);

        cmbCoreType.ItemsSource = Utils.GetEnumNames<ECoreType>().Where(t => t != nameof(ECoreType.v2rayN)).ToList().AppendEmpty();
        cmbSsrrMethod.ItemsSource = ViewModel.SsrrMethods;
        cmbSsrrProtocol.ItemsSource = ViewModel.SsrrProtocols;
        cmbSsrrObfs.ItemsSource = ViewModel.SsrrObfsList;

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.SelectedSource.Remarks, v => v.txtRemarks.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Address, v => v.txtAddress.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Port, v => v.txtSsrrPort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Password, v => v.txtSsrrPassword.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.CoreType, v => v.cmbCoreType.SelectedValue).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SsrrMethod, v => v.cmbSsrrMethod.SelectedValue).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SsrrProtocol, v => v.cmbSsrrProtocol.SelectedValue).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SsrrProtocolParam, v => v.txtSsrrProtocolParam.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SsrrObfs, v => v.cmbSsrrObfs.SelectedValue).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SsrrObfsParam, v => v.txtSsrrObfsParam.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.DisplayLog, v => v.togDisplayLog.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.PreSocksPort, v => v.txtPreSocksPort.Text).DisposeWith(disposables);
            this.WhenAnyValue(x => x.ViewModel.IsSsrrCore)
                .Subscribe(SetCoreMode)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.BrowseServerCmd, v => v.btnBrowse).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.EditServerCmd, v => v.btnEdit).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SaveServerCmd, v => v.btnSave).DisposeWith(disposables);
        });
    }

    private void SetCoreMode(bool isSsrrCore)
    {
        Title = isSsrrCore ? "SSRR" : ResUI.menuAddCustomServer;
        txtAddressTitle.Text = ResUI.TbAddress;
        txtAddress.IsReadOnly = !isSsrrCore;
        panFileButtons.IsVisible = !isSsrrCore;
        txtCoreTypeTitle.IsVisible = !isSsrrCore;
        cmbCoreType.IsVisible = !isSsrrCore;
        panCustomTips.IsVisible = !isSsrrCore;
        panSsrrSettings.IsVisible = isSsrrCore;
    }

    private async Task<bool> UpdateViewHandler(EViewAction action, object? obj)
    {
        switch (action)
        {
            case EViewAction.CloseWindow:
                Close(true);
                break;

            case EViewAction.BrowseServer:
                var fileName = await UI.OpenFileDialog(this, null);
                if (fileName.IsNullOrEmpty())
                {
                    return false;
                }
                ViewModel?.BrowseServer(fileName);
                break;
        }

        return await Task.FromResult(true);
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        txtRemarks.Focus();
    }
}
