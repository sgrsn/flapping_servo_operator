using System;
using System.Windows.Controls;
using System.IO.Ports;
using System.Windows.Threading;
using System.ComponentModel;
using System.Collections.Generic;

using flapping_servo_operator;

delegate void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e);

//シリアルポートの接続と切断を管理
static class COMPortSelector
{
    private static int BAUDRATE = 1000000;//115200;
    private static MainWindow mainWindow;
    private static ComboBox SerialPortComboBox;
    private static Button ConnectButton;
    private static DispatcherTimer _timer;

    private static List<string> Connected_list = new List<string>();

    private static DataReceivedHandler data_received_handle_;

    public static void Init()
    {
        mainWindow = (MainWindow)App.Current.MainWindow;
        SerialPortComboBox = mainWindow.SerialPortComboBox;
        ConnectButton = mainWindow.ConnectButton;
        SerialPortComboBox.SelectedIndex = 0;
        SetTimer();
    }
    public static void SetBaudrate(int baudrate)
    {
        BAUDRATE = baudrate;
    }
    public static bool IsConnected(string port_name)
    {
        return Connected_list.Contains(port_name);
    }

    public static bool IsComboBoxItemConnected()
    {
        return IsConnected((string)SerialPortComboBox.SelectedItem);
    }

    public static void ConnectPort(string port_name, ref SerialPortControl serial_control)
    {
        UpdateSerialPortComboBox();
        if (String.IsNullOrEmpty(port_name)) return;
        serial_control.port = new SerialPort(port_name, BAUDRATE, Parity.None, 8, StopBits.One);
        try
        {
            serial_control.port.Open();
            serial_control.port.DtrEnable = true;
            serial_control.port.RtsEnable = true;
            ConnectButton.Content = "Disconnect";
            Console.WriteLine("Connected.");
            serial_control.port.DiscardInBuffer();
            serial_control.SetReceiveInterrupt();

            Connected_list.Add(serial_control.port.PortName);
        }
        catch (Exception err)
        {
            Console.WriteLine("Unexpected exception : {0}", err.ToString());
        }
    }
    public static void DisconnectPort(string port_name, ref SerialPortControl serialport_control)
    {
        if (IsConnected(port_name))
        {
            serialport_control.RequestDisconnection();
            ConnectButton.Content = "Connect";
            Console.WriteLine("Disconnected.");
            Connected_list.Remove(serialport_control.port.PortName);
        }
    }

    public static void SetDataReceivedHandle(DataReceivedHandler data_received_handle)
    {
        data_received_handle_ = data_received_handle;
    }

    private static void UpdateSerialPortComboBox()
    {
        // 前に選んでいたポートの取得
        string prev_selected_port = "";
        if (SerialPortComboBox.SelectedItem != null)
            prev_selected_port = SerialPortComboBox.SelectedItem.ToString();

        // ポート一覧の更新
        string[] port_list = SerialPort.GetPortNames();
        SerialPortComboBox.Items.Clear();
        foreach (var i in port_list) SerialPortComboBox.Items.Add(i);

        // 前に選択していたポートを再度選択
        for (int i = 0; i < SerialPortComboBox.Items.Count; i++)
        {
            if (SerialPortComboBox.Items[i].ToString() == prev_selected_port)
                SerialPortComboBox.SelectedIndex = i;
        }
        // ポート数が1以下であれば0番目を選択
        if (SerialPortComboBox.Items.Count <= 1)
            SerialPortComboBox.SelectedIndex = 0;

        if (IsConnected((string)SerialPortComboBox.SelectedItem))
        {
            ConnectButton.Content = "Disconnect";
        }
        else
        {
            ConnectButton.Content = "Connect";
        }
    }
    private static void SetTimer()
    {
        _timer = new DispatcherTimer();
        _timer.Interval = new TimeSpan(0, 0, 1);
        _timer.Tick += new EventHandler(OnTimedEvent);
        _timer.Start();
        mainWindow.Closing += new CancelEventHandler(StopTimer);
    }
    private static void OnTimedEvent(Object source, EventArgs e)
    {
        UpdateSerialPortComboBox();
    }
    private static void StopTimer(object sender, CancelEventArgs e)
    {
        _timer.Stop();
    }
}