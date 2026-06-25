namespace v2rayN.Views;

public partial class AddServer2Window
{
    public AddServer2Window(ProfileItem profileItem)
    {
        InitializeComponent();

        Owner = Application.Current.MainWindow;
        Loaded += Window_Loaded;
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
            this.Bind(ViewModel, vm => vm.CoreType, v => v.cmbCoreType.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SsrrMethod, v => v.cmbSsrrMethod.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SsrrProtocol, v => v.cmbSsrrProtocol.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SsrrProtocolParam, v => v.txtSsrrProtocolParam.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SsrrObfs, v => v.cmbSsrrObfs.Text).DisposeWith(disposables);
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
        WindowsUtils.SetDarkBorder(this, AppManager.Instance.Config.UiItem.CurrentTheme);
    }

    private void SetCoreMode(bool isSsrrCore)
    {
        Title = isSsrrCore ? "SSRR" : ResUI.menuAddCustomServer;
        txtAddressTitle.Text = ResUI.TbAddress;
        txtAddress.IsReadOnly = !isSsrrCore;
        panFileButtons.Visibility = isSsrrCore ? Visibility.Collapsed : Visibility.Visible;
        txtCoreTypeTitle.Visibility = isSsrrCore ? Visibility.Collapsed : Visibility.Visible;
        cmbCoreType.Visibility = isSsrrCore ? Visibility.Collapsed : Visibility.Visible;
        panCustomTips.Visibility = isSsrrCore ? Visibility.Collapsed : Visibility.Visible;
        panSsrrSettings.Visibility = isSsrrCore ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task<bool> UpdateViewHandler(EViewAction action, object? obj)
    {
        switch (action)
        {
            case EViewAction.CloseWindow:
                DialogResult = true;
                break;

            case EViewAction.BrowseServer:
                if (UI.OpenFileDialog(out var fileName, "Config|*.json|TOML|*.toml|YAML|*.yaml;*.yml|All|*.*") != true)
                {
                    return false;
                }
                ViewModel?.BrowseServer(fileName);
                break;
        }

        return await Task.FromResult(true);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        txtRemarks.Focus();
    }
}
