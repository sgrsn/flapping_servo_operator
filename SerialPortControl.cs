using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO.Ports;
using System.Windows.Threading;
using System.Threading;
using System.ComponentModel;

namespace flapping_servo_operator
{
    public delegate void SerialReceivedHandle();
    public class SerialPortControl
    {
        public SerialPort port;
        private bool request_disconnection = false;
        private bool closing_port = false;

        private SerialReceivedHandle _handle;
        public int[] Register = new int[64];
        private int indata = 0;
        private byte[] buffer = new byte[16];
        private const byte HEAD_BYTE = 0x1D;
        private const byte ESCAPE_BYTE = 0x1E;
        private const byte ESCAPE_MASK = 0x1F;

        public SerialPortControl()
        {
        }

        public void SetReceiveInterrupt()
        {
            port.DataReceived += new SerialDataReceivedEventHandler(aDataReceivedHandler);
        }

        public void SetDatareceivedHandle(SerialReceivedHandle data_received_handle)
        {
            _handle = data_received_handle;
        }

        public bool IsAvailable()
        {
            if (port == null) return false;
            return port.IsOpen;
        }

        public void RequestDisconnection()
        {
            request_disconnection = true;
        }

        public void aDataReceivedHandler(
                        object sender,
                        SerialDataReceivedEventArgs e)
        {
            for (int i = 0; i < buffer.Length; i++) buffer[i] = 0;
            indata = 0;
            try
            {
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    ReceiveDataWithSize();
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(
                      DispatcherPriority.Background,
                      new Action(() => {
                            ReceiveDataWithSize();
                      }));
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("Unexpected exception : {0}", err.ToString());
            }
        }

        private void ReceiveDataWithSize()
        {
            // ポートが閉じている時は何もしない
            if (closing_port) return;
            
            indata = port.ReadByte();
            // HEAD_BYTE以外は無視
            if (indata != HEAD_BYTE)
            {
                return;
            }
            // HEAD_BYTEを受信したらパケットを受信
            else if (indata == HEAD_BYTE)
            {
                int size = port.ReadByte();
                if (size < buffer.Length)
                {
                    port.Read(buffer, 0, size);
                    ProcessingReceivedData(buffer);
                    _handle();
                }
            }

            // 切断処理
            if (request_disconnection)
            {
                port.DiscardInBuffer();
                port.Close();
                port.Dispose();
                request_disconnection = false;
                closing_port = true;
            }

        }

        // (HEAD SIZE) REG BYTE1 BYTE2... CHECKSUM
        private void ProcessingReceivedData(byte[] buffer)
        {
            int index = 0;
            byte reg = buffer[0];
            index++;
            byte checksum = 0;

            checksum += reg;
            byte[] bytes = new byte[4] { 0, 0, 0, 0 };

            for (int i = 0; i < 4; ++i)
            {
                byte d = buffer[index++];
                if (d == ESCAPE_BYTE)
                {
                    byte nextByte = buffer[index++];
                    bytes[i] = (byte)((int)nextByte ^ (int)ESCAPE_MASK);
                    checksum += (byte)((int)d + (int)nextByte);
                }
                else
                {
                    bytes[i] = d;
                    checksum += d;
                }
            }

            byte checksum_recv = buffer[index++];
            int DATA = 0x00;
            for (int i = 0; i < 4; i++)
            {
                DATA |= (((int)bytes[i]) << (24 - (i * 8)));
            }

            if (checksum == checksum_recv)
            {
                Register[reg] = DATA;
            }
            else
            {
                // data error
                Console.WriteLine("data error, checksum is wrong.");
            }

        }

        private void WriteOneByteData(byte data)
        {
            byte[] buffer = { data, 0 };
            port.Write(buffer, 0, 1);
        }

        // HEAD REG BYTE1 BYTE2... CHECKSUM
        // WriteOneByteDataを使用して一つずつ送信する
        public void WriteData(int data, byte reg)
        {
            byte[] dataBytes = new byte[4]
            {
            (byte)((data >> 24) & 0xFF),
            (byte)((data >> 16) & 0xFF),
            (byte)((data >>  8) & 0xFF),
            (byte)((data >>  0) & 0xFF)
            };

            WriteOneByteData(HEAD_BYTE);
            WriteOneByteData(reg);

            byte checksum = 0;
            for (int i = 0; i < 4; ++i)
            {
                if ((dataBytes[i] == ESCAPE_BYTE) || (dataBytes[i] == HEAD_BYTE))
                {
                    WriteOneByteData(ESCAPE_BYTE);
                    checksum += ESCAPE_BYTE;
                    WriteOneByteData((byte)((int)dataBytes[i] ^ (int)ESCAPE_MASK));
                    checksum += (byte)((int)dataBytes[i] ^ (int)ESCAPE_MASK);
                }
                else
                {
                    WriteOneByteData(dataBytes[i]);
                    checksum += dataBytes[i];
                }
            }

            // 末尾にチェックサムを追加で送信する
            WriteOneByteData(checksum);
        }

        // (同じ)HEAD REG BYTE1 BYTE2... CHECKSUM
        // Writeでまとめて送信する
        public void WritePieceData(int data, byte reg)
        {
            byte[] toWrite_bytes = new byte[11];

            toWrite_bytes[0] = HEAD_BYTE;
            toWrite_bytes[1] = reg;
            int index = 2;

            byte[] dataBytes = new byte[4]
            {
            (byte)((data >> 24) & 0xFF),
            (byte)((data >> 16) & 0xFF),
            (byte)((data >>  8) & 0xFF),
            (byte)((data >>  0) & 0xFF)
            };

            byte checksum = 0;
            for (int i = 0; i < 4; ++i)
            {
                if ((dataBytes[i] == ESCAPE_BYTE) || (dataBytes[i] == HEAD_BYTE))
                {
                    //WriteOneByteData(ESCAPE_BYTE);
                    toWrite_bytes[index] = ESCAPE_BYTE;
                    index++;
                    checksum += ESCAPE_BYTE;
                    //WriteOneByteData((byte)((int)dataBytes[i] ^ (int)ESCAPE_MASK));
                    toWrite_bytes[index] = (byte)((int)dataBytes[i] ^ (int)ESCAPE_MASK);
                    index++;
                    checksum += (byte)((int)dataBytes[i] ^ (int)ESCAPE_MASK);
                }
                else
                {
                    //WriteOneByteData(dataBytes[i]);
                    toWrite_bytes[index] = dataBytes[i];
                    index++;
                    checksum += dataBytes[i];
                }
            }
            // 末尾にチェックサムを追加で送信する
            //WriteOneByteData(checksum);
            toWrite_bytes[index] = checksum;
            index++;
            try
            {
                port.Write(toWrite_bytes, 0, index);
            }
            catch (Exception err)
            {
                Console.WriteLine("Unexpected exception : {0}", err.ToString());
            }
            /*catch(System.InvalidOperationException e)
            {
                Console.WriteLine(e);
            }*/
        }

        private enum ReceivedData
        {
            HEADER,
            REG_ADDRESS,
            DATA,
            CHECKSUM,
        }
    }
}