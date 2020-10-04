using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using flapping_servo_operator;
using InteractiveDataDisplay.WPF;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows;

public class GraphData
{
    public int register = 0;
    public int[] x = new int[100];
    public int[] y = new int[100];
    public LineGraph linegraph = new LineGraph();
    public SolidColorBrush lg_color = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
    public string description = "";

    public void ShiftData(int new_x, int new_y)
    {
        int size = x.Length;
        for (int i = 0; i < size - 1; i++)
        {
            x[i] = x[i + 1];
            y[i] = y[i + 1];
        }
        x[size - 1] = new_x;
        y[size - 1] = new_y;
    }

    public void PlotGraph()
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            linegraph.Plot(x, y);
        }
        else
        {
            int[] tmpx = x;
            int[] tmpy = y;
            LineGraph tmp_lg = linegraph;
            Application.Current.Dispatcher.BeginInvoke(
              DispatcherPriority.Background,
              new Action(() => {
                  tmp_lg.Plot(tmpx, tmpy);
              }));
        }
    }
}

public class Device
{
    public SerialPortControl serial_control;// = new SerialPortControl();
    public List<GraphData> graph = new List<GraphData>();
    
    public void Update(int frame_count)
    {
        foreach (var graphdata in graph)
        {
            int addr = graphdata.register;
            graphdata.ShiftData(frame_count, serial_control.Register[addr]);

            // ここの処理はlineが増えていくとどうしようかな
            graphdata.linegraph.PlotOriginX = graphdata.x[0];
            graphdata.linegraph.PlotWidth = 100;

            graphdata.PlotGraph();
        }
    }
}

// シリアルポートからの読み取りとグラフの描画
static class DrawSerialGraph
{
    private static List<Device> device = new List<Device>();
    private static MainWindow mainWindow;
    private static ComboBox SerialPortComboBox;
    private static int frame_count;
    public static void Init()
    {
        mainWindow = (MainWindow)App.Current.MainWindow;
        SerialPortComboBox = mainWindow.SerialPortComboBox;

        device.Add(new Device());
        device.Last().serial_control = new SerialPortControl();
    }

    public static void AddSerialDevice(string port_name)
    {
        SerialReceivedHandle data_received_handler = UpdateChartHandler;
        device.Last().serial_control.SetDatareceivedHandle(data_received_handler);

        COMPortSelector.ConnectPort(port_name, ref device.Last().serial_control);

        device.Add(new Device());   // 次に使うやつ
        device.Last().serial_control = new SerialPortControl();
    }
    public static void RemoveSerialDevice(string port_name)
    {
        Device tmp = new Device();
        foreach (var d in device)
        {
            if (d.serial_control.port != null)
                if (d.serial_control.port.PortName == port_name)
                {
                    COMPortSelector.DisconnectPort(port_name, ref d.serial_control);
                    tmp = d;    
                }
        }
        device.Remove(tmp);

        // lineの削除
        foreach (var graph in tmp.graph)
        {
            mainWindow.lines_left.Children.Remove(graph.linegraph);
            mainWindow.lines_right.Children.Remove(graph.linegraph);
        }
    }

    public static void Write2Devce(int index, int data, byte reg)
    {
        device[index].serial_control.WritePieceData(data, reg);
    }

    public static void LinkingRegister2Graph(int register, int left_or_right)
    {
        device.Last().graph.Add(new GraphData());
        device.Last().graph.Last().register = register;
        if (left_or_right == 0)
        {
            mainWindow.lines_left.Children.Add(device.Last().graph.Last().linegraph);
        }
        else
        {
            mainWindow.lines_right.Children.Add(device.Last().graph.Last().linegraph);
        }

        device.Last().graph.Last().linegraph.Stroke = device.Last().graph.Last().lg_color;
        device.Last().graph.Last().linegraph.Description = String.Format(device.Last().graph.Last().description);
        device.Last().graph.Last().linegraph.StrokeThickness = 2;
    }

    public static void UpdateChartHandler()
    {
        frame_count++;

        for (int index = 0; index < device.Count - 1; index++)
        {
            device[index].Update(frame_count);
        }
    }

}
