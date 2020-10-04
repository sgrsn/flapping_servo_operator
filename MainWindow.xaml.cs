using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO.Ports;
using System.Timers;
using System.Windows.Threading;
using System.ComponentModel;

using static RegisterMap;
using static COMPortSelector;
using InteractiveDataDisplay.WPF;

/*
 * COMPortSelector
 * 存在するCOMポートと、接続について管理
 * 
 * SerialPortController
 * 接続されているか返す
 * 
*/

namespace flapping_servo_operator
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {

        public MainWindow()
        {
            InitializeComponent();
            COMPortSelector.Init();
            DrawSerialGraph.Init();

            this.Closing += new CancelEventHandler(CloseSerialPort);

        }

        private void CloseSerialPort(object sender, CancelEventArgs e)
        {
            //mySerial.ClosePort();
            //COMPortSelector.CloseAll();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if(!COMPortSelector.IsComboBoxItemConnected())
            {
                DrawSerialGraph.LinkingRegister2Graph(0x09, 0);
                DrawSerialGraph.LinkingRegister2Graph(0x12, 0);
                DrawSerialGraph.LinkingRegister2Graph(0x11, 1);
                DrawSerialGraph.LinkingRegister2Graph(0x13, 1);
                DrawSerialGraph.AddSerialDevice(SerialPortComboBox.Text);
            }

            else
            {
                DrawSerialGraph.RemoveSerialDevice(SerialPortComboBox.Text);
            }
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Position_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            DrawSerialGraph.Write2Devce(0, 1, COMMAND_MODE);
            DrawSerialGraph.Write2Devce(0, 123, COMMAND_START);

            if (String.IsNullOrEmpty(e.NewValue.ToString())) return;
            else
            {
                double position_slider_value = Convert.ToDouble(e.NewValue.ToString());
                //Console.WriteLine(motor_slider_value);
                //SliderTextBlock.Text = ((int)motor_slider_value).ToString();

                DrawSerialGraph.Write2Devce(0, (int)(position_slider_value), COMMAND_MOTOR);
                DrawSerialGraph.Write2Devce(0, (int)(position_slider_value), COMMAND_MOTOR_CONFIRM);
            }
        }

        private void Speed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            DrawSerialGraph.Write2Devce(0, 2, COMMAND_MODE);
            DrawSerialGraph.Write2Devce(0, 123, COMMAND_START);

            if (String.IsNullOrEmpty(e.NewValue.ToString())) return;
            else
            {
                double speed_slider_value = Convert.ToDouble(e.NewValue.ToString());
                //Console.WriteLine(motor_slider_value);
                //SliderTextBlock.Text = ((int)motor_slider_value).ToString();

                DrawSerialGraph.Write2Devce(0, (int)(speed_slider_value), COMMAND_MOTOR);
                DrawSerialGraph.Write2Devce(0, (int)(speed_slider_value), COMMAND_MOTOR_CONFIRM);
            }
        }
    }
}
