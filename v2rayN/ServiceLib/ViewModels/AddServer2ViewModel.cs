namespace ServiceLib.ViewModels;

public class AddServer2ViewModel : MyReactiveObject
{
    [Reactive]
    public ProfileItem SelectedSource { get; set; }

    [Reactive]
    public string? CoreType { get; set; }

    [Reactive]
    public bool IsSsrrCore { get; set; }

    [Reactive]
    public string? SsrrMethod { get; set; }

    [Reactive]
    public string? SsrrProtocol { get; set; }

    [Reactive]
    public string? SsrrProtocolParam { get; set; }

    [Reactive]
    public string? SsrrObfs { get; set; }

    [Reactive]
    public string? SsrrObfsParam { get; set; }

    public List<string> SsrrMethods { get; } = ["none"];

    public List<string> SsrrProtocols { get; } =
    [
        "auth_chain_d",
        "auth_chain_e",
        "auth_chain_f",
        "auth_akarin_rand",
        "auth_akarin_spec_a",
        "auth_akarin_spec_a_v2",
        "auth_akarin_spec_time_v2"
    ];

    public List<string> SsrrObfsList { get; } =
    [
        "tls1.2_ticket_auth",
        "tls1.2_ticket_fastauth",
        "tls1.3_ticket_auth"
    ];

    public ReactiveCommand<Unit, Unit> BrowseServerCmd { get; }
    public ReactiveCommand<Unit, Unit> EditServerCmd { get; }
    public ReactiveCommand<Unit, Unit> SaveServerCmd { get; }
    public bool IsModified { get; set; }

    public AddServer2ViewModel(ProfileItem profileItem, Func<EViewAction, object?, Task<bool>>? updateView)
    {
        _config = AppManager.Instance.Config;
        _updateView = updateView;

        BrowseServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            _updateView?.Invoke(EViewAction.BrowseServer, null);
            await Task.CompletedTask;
        });
        EditServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await EditServer();
        });
        SaveServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SaveServerAsync();
        });

        SelectedSource = profileItem.IndexId.IsNullOrEmpty() ? profileItem : JsonUtils.DeepCopy(profileItem);
        CoreType = SelectedSource?.CoreType?.ToString();
        var protocolExtra = SelectedSource.GetProtocolExtra();
        SsrrMethod = protocolExtra.SsMethod ?? "none";
        SsrrProtocol = protocolExtra.SsrrProtocol ?? "auth_akarin_spec_a_v2";
        SsrrProtocolParam = protocolExtra.SsrrProtocolParam ?? "64";
        SsrrObfs = protocolExtra.SsrrObfs ?? "tls1.2_ticket_auth";
        SsrrObfsParam = protocolExtra.SsrrObfsParam ?? string.Empty;
        UpdateCoreMode();

        this.WhenAnyValue(x => x.CoreType)
            .Subscribe(_ => UpdateCoreMode());
    }

    private async Task SaveServerAsync()
    {
        var remarks = SelectedSource.Remarks;
        if (remarks.IsNullOrEmpty())
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseFillRemarks);
            return;
        }

        SelectedSource.CoreType = CoreType.IsNullOrEmpty() ? null : Enum.Parse<ECoreType>(CoreType);

        if (IsSsrrCore)
        {
            if (SelectedSource.Address.IsNullOrEmpty())
            {
                NoticeManager.Instance.Enqueue(ResUI.FillServerAddressCustom);
                return;
            }
            if (SelectedSource.Port is <= 0 or > 65535)
            {
                NoticeManager.Instance.Enqueue(string.Format(ResUI.MsgInvalidProperty, ResUI.TbPort));
                return;
            }
            if (SelectedSource.Password.IsNullOrEmpty())
            {
                NoticeManager.Instance.Enqueue(ResUI.FillPassword);
                return;
            }
            if (!(SelectedSource.PreSocksPort is > 0 and <= 65535))
            {
                NoticeManager.Instance.Enqueue(string.Format(ResUI.MsgInvalidProperty, ResUI.TbPreSocksPort));
                return;
            }
            if (SsrrMethod.IsNullOrEmpty()
                || SsrrProtocol.IsNullOrEmpty()
                || SsrrObfs.IsNullOrEmpty())
            {
                NoticeManager.Instance.Enqueue(ResUI.CheckServerSettings);
                return;
            }

            SelectedSource.SetProtocolExtra(SelectedSource.GetProtocolExtra() with
            {
                SsMethod = SsrrMethod,
                SsrrProtocol = SsrrProtocol,
                SsrrProtocolParam = SsrrProtocolParam,
                SsrrObfs = SsrrObfs,
                SsrrObfsParam = SsrrObfsParam,
            });
        }
        else if (SelectedSource.Address.IsNullOrEmpty())
        {
            NoticeManager.Instance.Enqueue(ResUI.FillServerAddressCustom);
            return;
        }

        if (await ConfigHandler.SaveCustomServer(_config, SelectedSource) == 0)
        {
            NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);
            _updateView?.Invoke(EViewAction.CloseWindow, null);
        }
        else
        {
            NoticeManager.Instance.Enqueue(ResUI.OperationFailed);
        }
    }

    private void UpdateCoreMode()
    {
        IsSsrrCore = CoreType == nameof(ECoreType.SSRR);
        if (IsSsrrCore)
        {
            SelectedSource.CoreType = ECoreType.SSRR;
            SelectedSource.PreSocksPort ??= Utils.GetFreePort(AppManager.Instance.GetLocalPort(EInboundProtocol.speedtest) + 100);
        }
    }

    public async Task BrowseServer(string fileName)
    {
        if (fileName.IsNullOrEmpty())
        {
            return;
        }

        var item = await AppManager.Instance.GetProfileItem(SelectedSource.IndexId);
        item ??= SelectedSource;
        item.Address = fileName;
        if (await ConfigHandler.AddCustomServer(_config, item, false) == 0)
        {
            NoticeManager.Instance.Enqueue(ResUI.SuccessfullyImportedCustomServer);
            if (item.IndexId.IsNotEmpty())
            {
                SelectedSource = JsonUtils.DeepCopy(item);
            }
            IsModified = true;
        }
        else
        {
            NoticeManager.Instance.Enqueue(ResUI.FailedImportedCustomServer);
        }
    }

    private async Task EditServer()
    {
        var address = SelectedSource.Address;
        if (address.IsNullOrEmpty())
        {
            NoticeManager.Instance.Enqueue(ResUI.FillServerAddressCustom);
            return;
        }

        address = Utils.GetConfigPath(address);
        if (File.Exists(address))
        {
            ProcUtils.ProcessStart(address);
        }
        else
        {
            NoticeManager.Instance.Enqueue(ResUI.FailedReadConfiguration);
        }
        await Task.CompletedTask;
    }
}
