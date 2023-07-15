using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;

namespace RFIDReader
{

    public partial class Form1 : Form
    {
        private string Id { get; set; }
        private HttpClient httpClient { get; set; }

        public Form1()
        {
            InitializeComponent();
            var devices = RawInputDevice.GetDevices();

            // Keyboards will be returned as RawInputKeyboard.
            var keyboards = devices.OfType<RawInputKeyboard>();

            RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard, RawInputDeviceFlags.None, IntPtr.Zero);

            httpClient = new HttpClient();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_INPUT = 0x00FF;

            if (m.Msg == WM_INPUT)
            {
                var data = RawInputData.FromHandle(m.LParam);

                //var sourceDeviceHandle = data.Header.DeviceHandle;
                var sourceDevice = data.Device;

                if (sourceDevice.ProductName == "RFID Reader")
                {

                    if (data is RawInputKeyboardData keyboardData && keyboardData.Keyboard.Flags == RawKeyboardFlags.None)
                    {

                        var key = (Keys)keyboardData.Keyboard.VirutalKey;
                        if (key == Keys.Enter)
                        {
                            httpClient.PostAsync(
                                "https://webhook.site/7a543d4c-3c85-44f0-a28d-870112fd24d2", new StringContent(Id));

                            Id = String.Empty;
                        }
                        else
                        {
                            var buf = new StringBuilder(256);
                            ToUnicode((uint)keyboardData.Keyboard.VirutalKey, 0, new byte[256], buf, 256, 0);
                            Id += buf.ToString();
                        }
                    }
                }

            }

            base.WndProc(ref m);
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(uint virtualKeyCode, uint scanCode,
            byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)]
            StringBuilder receivingBuffer,
            int bufferSize, uint flags);
    }
}
