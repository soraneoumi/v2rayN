namespace ServiceLib.Handler;

/// <summary>
/// Core configuration file processing class
/// </summary>
public static class CoreConfigHandler
{
    private static readonly string _tag = "CoreConfigHandler";

    public static async Task<RetResult> GenerateClientConfig(CoreConfigContext context, string? fileName)
    {
        var config = AppManager.Instance.Config;
        var result = new RetResult();
        var node = context.Node;

        if (node.ConfigType == EConfigType.Custom)
        {
            result = node.CoreType switch
            {
                ECoreType.mihomo => await new CoreConfigClashService(config).GenerateClientCustomConfig(node, fileName),
                ECoreType.SSRR => GenerateSsrrConfig(node),
                _ => await GenerateClientCustomConfig(node, fileName)
            };
        }
        else if (context.RunCoreType == ECoreType.sing_box)
        {
            result = new CoreConfigSingboxService(context).GenerateClientConfigContent();
        }
        else
        {
            result = new CoreConfigV2rayService(context).GenerateClientConfigContent();
        }
        if (result.Success != true)
        {
            return result;
        }
        if (fileName.IsNotEmpty() && result.Data != null)
        {
            await File.WriteAllTextAsync(fileName, result.Data.ToString());
        }

        return result;
    }

    private static async Task<RetResult> GenerateClientCustomConfig(ProfileItem node, string? fileName)
    {
        var ret = new RetResult();
        try
        {
            if (node == null || fileName is null)
            {
                ret.Msg = ResUI.CheckServerSettings;
                return ret;
            }

            if (File.Exists(fileName))
            {
                File.SetAttributes(fileName, FileAttributes.Normal); //If the file has a read-only attribute, direct deletion will fail
                File.Delete(fileName);
            }

            var addressFileName = node.Address;
            if (!File.Exists(addressFileName))
            {
                addressFileName = Utils.GetConfigPath(addressFileName);
            }
            if (!File.Exists(addressFileName))
            {
                ret.Msg = ResUI.FailedGenDefaultConfiguration;
                return ret;
            }
            File.Copy(addressFileName, fileName);
            File.SetAttributes(fileName, FileAttributes.Normal); //Copy will keep the attributes of addressFileName, so we need to add write permissions to fileName just in case of addressFileName is a read-only file.

            //check again
            if (!File.Exists(fileName))
            {
                ret.Msg = ResUI.FailedGenDefaultConfiguration;
                return ret;
            }

            ret.Msg = string.Format(ResUI.SuccessfulConfiguration, "");
            ret.Success = true;
            return await Task.FromResult(ret);
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
            ret.Msg = ResUI.FailedGenDefaultConfiguration;
            return ret;
        }
    }

    private static RetResult GenerateSsrrConfig(ProfileItem node)
    {
        var ret = new RetResult();
        var protocolExtra = node.GetProtocolExtra();

        if (node.Address.IsNullOrEmpty()
            || node.Port is <= 0 or > 65535
            || node.Password.IsNullOrEmpty()
            || !(node.PreSocksPort is > 0 and <= 65535)
            || protocolExtra.SsrrProtocol.IsNullOrEmpty()
            || protocolExtra.SsrrObfs.IsNullOrEmpty())
        {
            ret.Msg = ResUI.CheckServerSettings;
            return ret;
        }

        var localPort = node.PreSocksPort.Value;
        var toml = new StringBuilder()
            .AppendLine("[logging]")
            .AppendLine("level = \"info\"")
            .AppendLine()
            .AppendLine("[local]")
            .Append("listen = ").AppendTomlString($"{Global.Loopback}:{localPort}").AppendLine()
            .AppendLine("read_buffer_size = 65536")
            .AppendLine("tcp_warm_pool_size = 1")
            .AppendLine()
            .AppendLine("[server]")
            .Append("remote = ").AppendTomlString($"{node.Address}:{node.Port}").AppendLine()
            .AppendLine("cipher = \"none\"")
            .AppendLine()
            .AppendLine("[obfs]")
            .Append("method = ").AppendTomlString(protocolExtra.SsrrObfs).AppendLine()
            .Append("obfs_param = ").AppendTomlString(protocolExtra.SsrrObfsParam ?? string.Empty).AppendLine()
            .Append("host = ").AppendTomlString(protocolExtra.SsrrObfsParam ?? string.Empty).AppendLine()
            .AppendLine()
            .AppendLine("[protocol]")
            .Append("method = ").AppendTomlString(protocolExtra.SsrrProtocol).AppendLine()
            .Append("password = ").AppendTomlString(node.Password).AppendLine()
            .Append("protocol_param = ").AppendTomlString(protocolExtra.SsrrProtocolParam ?? string.Empty).AppendLine()
            .AppendLine("tcp_mss = 0")
            .AppendLine("overhead = 4")
            .AppendLine("udp_packet_size = 1452")
            .AppendLine()
            .AppendLine("[dns]")
            .AppendLine("remote = \"8.8.8.8:53\"")
            .AppendLine("china = \"223.5.5.5:53\"")
            .AppendLine("custom_acl_file = \"\"")
            .ToString();

        ret.Msg = string.Format(ResUI.SuccessfulConfiguration, "");
        ret.Success = true;
        ret.Data = toml;
        return ret;
    }

    private static StringBuilder AppendTomlString(this StringBuilder builder, string value)
    {
        builder.Append('"');
        foreach (var ch in value)
        {
            if (ch is '\\' or '"')
            {
                builder.Append('\\');
            }
            builder.Append(ch);
        }
        return builder.Append('"');
    }

    public static async Task<RetResult> GenerateClientSpeedtestConfig(Config config, string fileName, List<ServerTestItem> selecteds, ECoreType coreType)
    {
        var result = new RetResult();
        var dummyNode = new ProfileItem
        {
            CoreType = coreType
        };
        var builderResult = await CoreConfigContextBuilder.Build(config, dummyNode);
        var context = builderResult.Context;
        foreach (var testItem in selecteds)
        {
            var node = testItem.Profile;
            var (actNode, _) = await CoreConfigContextBuilder.ResolveNodeAsync(context, node, true);
            if (node.IndexId == actNode.IndexId)
            {
                continue;
            }
            context.ServerTestItemMap[node.IndexId] = actNode.IndexId;
        }
        if (coreType == ECoreType.sing_box)
        {
            result = new CoreConfigSingboxService(context).GenerateClientSpeedtestConfig(selecteds);
        }
        else if (coreType == ECoreType.Xray)
        {
            result = new CoreConfigV2rayService(context).GenerateClientSpeedtestConfig(selecteds);
        }
        if (result.Success != true)
        {
            return result;
        }
        await File.WriteAllTextAsync(fileName, result.Data.ToString());
        return result;
    }

    public static async Task<RetResult> GenerateClientSpeedtestConfig(Config config, CoreConfigContext context, ServerTestItem testItem, string fileName)
    {
        var result = new RetResult();
        var initPort = AppManager.Instance.GetLocalPort(EInboundProtocol.speedtest);
        var port = Utils.GetFreePort(initPort + testItem.QueueNum);
        testItem.Port = port;

        if (context.RunCoreType == ECoreType.sing_box)
        {
            result = new CoreConfigSingboxService(context).GenerateClientSpeedtestConfig(port);
        }
        else
        {
            result = new CoreConfigV2rayService(context).GenerateClientSpeedtestConfig(port);
        }
        if (result.Success != true)
        {
            return result;
        }

        await File.WriteAllTextAsync(fileName, result.Data.ToString());
        return result;
    }
}
