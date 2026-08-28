using System.IO;
using System.Windows;
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
            await SendCurrentAsync();
        }
    }

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
        ConnectButton.IsEnabled = !busy;
        ModelCombo.IsEnabled = _client?.IsConnected != true;
        PortCombo.IsEnabled = _client?.IsConnected != true;
        BaudCombo.IsEnabled = _client?.IsConnected != true;
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
