using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VehicleVision.SimComAt;

namespace VehicleVision.SimComAt.Gui;

public partial class MainWindow : Window
{
    private IAtTransport? _transport;
    private AtCommandClient? _client;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        Title = $"{Title} v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "development"}";
        ModelCombo.ItemsSource = ModemProfiles.All;
        ModelCombo.SelectedIndex = 0;
        RefreshPorts();
    }

    private ModemProfile? SelectedProfile => ModelCombo.SelectedItem as ModemProfile;

    private void ModelCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SelectedProfile is { } profile)
        {
            CommandGrid.ItemsSource = profile.Commands;
            BaudCombo.Text = profile.DefaultBaudRate.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private void RefreshPorts_Click(object sender, RoutedEventArgs e) => RefreshPorts();

    private void RefreshPorts()
    {
        var previous = PortCombo.SelectedItem as string;
        PortCombo.ItemsSource = SerialPortDiscovery.GetPortNames();
        PortCombo.SelectedItem = previous;
        if (PortCombo.SelectedIndex < 0 && PortCombo.Items.Count > 0) PortCombo.SelectedIndex = 0;
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        if (_client?.IsConnected == true)
        {
            await DisconnectAsync();
            return;
        }

        if (PortCombo.SelectedItem is not string port || !int.TryParse(BaudCombo.Text, out var baud) || baud <= 0)
        {
            MessageBox.Show(this, "ポートと正しいボーレートを選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            SetBusy(true);
            _transport = new SerialAtTransport(new SerialConnectionOptions(port, baud));
            _client = new AtCommandClient(_transport);
            _client.Trace += Client_Trace;
            await _client.ConnectAsync();
            StatusText.Text = $"接続中: {port}";
            ConnectButton.Content = "切断";
            if (SelectedProfile?.Family == SimComFamily.Generic)
                await DetectModemAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            await DisposeTransportAsync();
            MessageBox.Show(this, ex.Message, "接続エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Send_Click(object sender, RoutedEventArgs e) => await SendCurrentAsync();

    private async void CommandText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await SendCurrentAsync();
        }
    }

    private async void CommandGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CommandGrid.SelectedItem is AtCommandDefinition definition)
        {
            CommandText.Text = definition.Command;
            if (definition.IsParameterized)
            {
                AppendLog("-- パラメーター付きコマンドは専用設定画面を使用してください。");
                return;
            }
            if (definition.Risk == AtCommandRisk.Destructive &&
                MessageBox.Show(this, $"「{definition.DisplayName}」を実行しますか？\n{definition.Description}", "実行確認",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            await SendCurrentAsync();
        }
    }

    private async void Detect_Click(object sender, RoutedEventArgs e)
    {
        if (_client?.IsConnected != true || _busy) return;
        try
        {
            SetBusy(true);
            await DetectModemAsync();
        }
        catch (Exception ex)
        {
            AppendLog($"!! 機種判定失敗: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task DetectModemAsync()
    {
        if (_client is null) return;
        var identity = await ModemDetector.DetectAsync(_client);
        ModelCombo.SelectedItem = identity.Profile;
        var model = string.IsNullOrWhiteSpace(identity.Model) ? "不明" : identity.Model;
        StatusText.Text = $"接続中: {model} / {identity.Profile.DisplayName}";
        AppendLog($"-- 検出: {identity.Manufacturer} {model} {identity.Revision}".TrimEnd());
    }

    private async void ApplyPdp_Click(object sender, RoutedEventArgs e) =>
        await RunPdpOperationAsync(async service =>
        {
            var result = await service.ConfigureAsync(ReadPdpSettings());
            AppendLog(result.IsSuccess ? "-- APN・認証設定完了" : $"!! 設定失敗: {result.FailedStep?.RawResponse}");
        });

    private async void Attach_Click(object sender, RoutedEventArgs e) =>
        await RunPdpOperationAsync(async service => Report(await service.AttachAsync(), "アタッチ"));

    private async void Activate_Click(object sender, RoutedEventArgs e) =>
        await RunPdpOperationAsync(async service => Report(await service.ActivateAsync(ReadContextId()), "PDP有効化"));

    private async void Deactivate_Click(object sender, RoutedEventArgs e) =>
        await RunPdpOperationAsync(async service => Report(await service.DeactivateAsync(ReadContextId()), "PDP無効化"));

    private async void SendSms_Click(object sender, RoutedEventArgs e)
    {
        if (_client?.IsConnected != true || _busy) return;
        try
        {
            SetBusy(true);
            var encodingText = (SmsEncodingCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Gsm";
            var message = new SmsMessage(SmsDestinationText.Text.Trim(), SmsBodyText.Text,
                Enum.Parse<SmsTextEncoding>(encodingText, true));
            if (MessageBox.Show(this, $"{message.Destination} へSMSを送信しますか？", "SMS送信確認",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            var result = await new SmsService(_client).SendAsync(message);
            AppendLog(result.IsSuccess ? "-- SMS送信完了" : $"!! SMS送信失敗: {result.FailedStep?.RawResponse}");
        }
        catch (Exception ex)
        {
            AppendLog($"!! SMS送信失敗: {ex.Message}");
            MessageBox.Show(this, ex.Message, "SMS送信エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void GnssOn_Click(object sender, RoutedEventArgs e) =>
        await RunGnssOperationAsync(async service => Report(await service.PowerOnAsync(), "GNSS電源ON"));

    private async void GnssInfo_Click(object sender, RoutedEventArgs e) =>
        await RunGnssOperationAsync(async service => Report(await service.GetInformationAsync(), "GNSS情報取得"));

    private async void GnssOff_Click(object sender, RoutedEventArgs e) =>
        await RunGnssOperationAsync(async service => Report(await service.PowerOffAsync(), "GNSS電源OFF"));

    private async Task RunGnssOperationAsync(Func<GnssService, Task> operation)
    {
        if (_client?.IsConnected != true || SelectedProfile is null || _busy) return;
        try
        {
            SetBusy(true);
            await operation(new GnssService(_client, SelectedProfile));
        }
        catch (Exception ex)
        {
            AppendLog($"!! {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void HttpSend_Click(object sender, RoutedEventArgs e)
    {
        if (_client?.IsConnected != true || _busy) return;
        try
        {
            SetBusy(true);
            var methodText = (HttpMethodCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Get";
            var settings = new HttpRequestSettings(HttpUrlText.Text.Trim(),
                Enum.Parse<HttpRequestMethod>(methodText, true), HttpBodyText.Text, HttpContentTypeText.Text.Trim());
            var result = await new HttpService(_client).SendAsync(settings);
            if (result.StatusCode is not null) AppendLog($"-- HTTP {result.StatusCode} ({result.ContentLength} bytes)");
            if (result.Content.Length > 0) AppendLog(result.Content);
            if (!result.Workflow.IsSuccess) AppendLog($"!! HTTP失敗: {result.Workflow.FailedStep?.RawResponse}");
        }
        catch (Exception ex)
        {
            AppendLog($"!! HTTP失敗: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void MqttConnect_Click(object sender, RoutedEventArgs e) =>
        await RunMqttOperationAsync(async service =>
        {
            var result = await service.ConnectAsync(new MqttConnectionSettings(
                MqttBrokerText.Text.Trim(), MqttClientIdText.Text.Trim(), MqttUserText.Text, MqttPasswordText.Password));
            AppendLog(result.IsSuccess ? "-- MQTT接続完了" : $"!! MQTT接続失敗: {result.FailedStep?.RawResponse}");
        });

    private async void MqttPublish_Click(object sender, RoutedEventArgs e) =>
        await RunMqttOperationAsync(async service =>
        {
            var result = await service.PublishAsync(0, new MqttPublishSettings(MqttTopicText.Text, MqttPayloadText.Text));
            AppendLog(result.IsSuccess ? "-- MQTT publish完了" : $"!! MQTT publish失敗: {result.FailedStep?.RawResponse}");
        });

    private async void MqttDisconnect_Click(object sender, RoutedEventArgs e) =>
        await RunMqttOperationAsync(async service =>
        {
            var result = await service.DisconnectAsync(0);
            AppendLog(result.IsSuccess ? "-- MQTT切断完了" : $"!! MQTT切断失敗: {result.FailedStep?.RawResponse}");
        });

    private async Task RunMqttOperationAsync(Func<MqttService, Task> operation)
    {
        if (_client?.IsConnected != true || _busy) return;
        try
        {
            SetBusy(true);
            await operation(new MqttService(_client));
        }
        catch (Exception ex)
        {
            AppendLog($"!! MQTT操作失敗: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RunPdpOperationAsync(Func<PdpContextService, Task> operation)
    {
        if (_client?.IsConnected != true || _busy) return;
        try
        {
            SetBusy(true);
            await operation(new PdpContextService(_client));
        }
        catch (Exception ex)
        {
            AppendLog($"!! {ex.Message}");
            MessageBox.Show(this, ex.Message, "PDP設定エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private PdpContextSettings ReadPdpSettings()
    {
        var typeText = (PdpTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "IP";
        var authText = (AuthCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "None";
        return new PdpContextSettings(
            ReadContextId(),
            ApnText.Text.Trim(),
            Enum.Parse<PdpType>(typeText, true),
            Enum.Parse<PdpAuthentication>(authText, true),
            ApnUserText.Text,
            ApnPasswordText.Password);
    }

    private int ReadContextId() => int.TryParse(ContextIdText.Text, out var cid)
        ? cid
        : throw new ArgumentException("CIDは1～16の数値で入力してください。");

    private void Report(AtCommandResult result, string operation) =>
        AppendLog(result.IsSuccess ? $"-- {operation}完了" : $"!! {operation}失敗: {result.RawResponse}");

    private async Task SendCurrentAsync()
    {
        if (_client?.IsConnected != true || _busy) return;
        try
        {
            SetBusy(true);
            var result = await _client.ExecuteAsync(CommandText.Text);
            if (result.TimedOut) AppendLog("!! TIMEOUT");
        }
        catch (Exception ex)
        {
            AppendLog($"!! {ex.Message}");
        }
        finally
        {
            SetBusy(false);
            CommandText.Focus();
            CommandText.SelectAll();
        }
    }

    private void Client_Trace(object? sender, AtTraceEntry entry) =>
        Dispatcher.Invoke(() => AppendLog($"{entry.Timestamp:HH:mm:ss.fff} {(entry.IsTransmit ? ">>" : "<<")} {entry.Text}"));

    private void AppendLog(string text)
    {
        LogText.AppendText(text + Environment.NewLine);
        LogText.ScrollToEnd();
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e) => LogText.Clear();

    private void SetBusy(bool busy)
    {
        _busy = busy;
        SendButton.IsEnabled = !busy && _client?.IsConnected == true;
        DetectButton.IsEnabled = !busy && _client?.IsConnected == true;
        ConnectButton.IsEnabled = !busy;
        ModelCombo.IsEnabled = _client?.IsConnected != true;
        PortCombo.IsEnabled = _client?.IsConnected != true;
        BaudCombo.IsEnabled = _client?.IsConnected != true;
        var packetDataEnabled = !busy && _client?.IsConnected == true && SelectedProfile?.Supports(ModemCapability.PacketData) == true;
        ApplyPdpButton.IsEnabled = packetDataEnabled;
        AttachButton.IsEnabled = packetDataEnabled;
        ActivateButton.IsEnabled = packetDataEnabled;
        DeactivateButton.IsEnabled = packetDataEnabled;
        SendSmsButton.IsEnabled = !busy && _client?.IsConnected == true && SelectedProfile?.Supports(ModemCapability.Sms) == true;
        var gnssEnabled = !busy && _client?.IsConnected == true && SelectedProfile?.Supports(ModemCapability.Gnss) == true;
        GnssOnButton.IsEnabled = gnssEnabled;
        GnssInfoButton.IsEnabled = gnssEnabled;
        GnssOffButton.IsEnabled = gnssEnabled;
        HttpSendButton.IsEnabled = !busy && _client?.IsConnected == true && SelectedProfile?.Supports(ModemCapability.Http) == true;
        var mqttEnabled = !busy && _client?.IsConnected == true && SelectedProfile?.Supports(ModemCapability.Mqtt) == true;
        MqttConnectButton.IsEnabled = mqttEnabled;
        MqttPublishButton.IsEnabled = mqttEnabled;
        MqttDisconnectButton.IsEnabled = mqttEnabled;
    }

    private async Task DisconnectAsync()
    {
        try
        {
            SetBusy(true);
            if (_client is not null) await _client.DisconnectAsync();
        }
        finally
        {
            await DisposeTransportAsync();
            StatusText.Text = "未接続";
            ConnectButton.Content = "接続";
            SetBusy(false);
        }
    }

    private async Task DisposeTransportAsync()
    {
        if (_client is not null) _client.Trace -= Client_Trace;
        if (_transport is not null) await _transport.DisposeAsync();
        _client = null;
        _transport = null;
    }

    protected override async void OnClosed(EventArgs e)
    {
        await DisposeTransportAsync();
        base.OnClosed(e);
    }
}
