using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RFIDReader
{

    public partial class RFIDReader : Form
    {
        private string ChipId { get; set; }
        private HttpClient httpClient { get; set; }

        public RFIDReader()
        {
            InitializeComponent();
            //var devices = RawInputDevice.GetDevices();

            // Keyboards will be returned as RawInputKeyboard.
            //var keyboards = devices.OfType<RawInputKeyboard>();

            RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, this.Handle);

            httpClient = new HttpClient();
  }
        
        protected override void WndProc(ref Message m)
        {
            const int WM_INPUT = 0x00FF;

            if (m.Msg == WM_INPUT)
            {
                try
                {
                    var data = RawInputData.FromHandle(m.LParam);

                    //var sourceDeviceHandle = data.Header.DeviceHandle;
                    var sourceDevice = data.Device;

                    if (sourceDevice != null && sourceDevice.ManufacturerName == "Sycreader RFID Technology Co., Ltd")
                    {
                        if (data is RawInputKeyboardData keyboardData && keyboardData.Keyboard.Flags == RawKeyboardFlags.None)
                        {

                            var key = (Keys)keyboardData.Keyboard.VirutalKey;
                            if (key == Keys.Enter)
                            {
                                var request = new
                                {
                                    token = System.Configuration.ConfigurationManager.AppSettings["token"],
                                    rfid = ChipId,
                                    missed_checkpoints = 0
                                };

                                File.AppendAllText("log.txt", ChipId + Environment.NewLine);

                                var result = httpClient.PostAsync(System.Configuration.ConfigurationManager.AppSettings["resultsUrl"],
                                    new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")).Result;

                                var isSuccess = false;
                                var errorMessage = "";
                                var competitorName = "";

                                if (result.StatusCode == HttpStatusCode.OK)
                                {
                                    var content = result.Content.ReadAsStringAsync().Result;

                                    try
                                    {
                                        dynamic obj = JObject.Parse(content);
                                        var resultProp = obj.result;
                                        if (resultProp != null)
                                        {
                                            isSuccess = true;
                                            try
                                            {
                                                var person = obj.start_number.competitor.person;
                                                competitorName = $"{person.name} {person.surname}";
                                            }
                                            catch (Exception ex)
                                            {
                                                //todo log
                                            }
                                        }
                                        else
                                        {
                                            errorMessage = $"Natjecatelj {ChipId} nije pronađen";
                                        }
                                    }
                                    catch (Exception)
                                    {

                                        var arr = JArray.Parse(content);
                                        var resultProp = arr[1];
                                        //if (resultProp != null)
                                        //{
                                        //    isSuccess = true;
                                        //}
                                        //else
                                        //{
                                        errorMessage = resultProp.ToString();
                                        //}
                                    }

                                }
                                else if (result.StatusCode == HttpStatusCode.BadRequest)
                                {
                                    var content = result.Content.ReadAsStringAsync().Result;
                                    dynamic obj = JObject.Parse(content);
                                    errorMessage = obj.error;
                                }

                                var item = new ListViewItem(ChipId);
                                item.SubItems.Add(competitorName);
                                item.SubItems.Add(isSuccess ? "Yes" : "No");
                                item.SubItems.Add(errorMessage);

                                listView1.Items.Insert(0, item);

                                if (!isSuccess)
                                {
                                    item.ForeColor = Color.Red;
                                }

                                ChipId = string.Empty;
                            }
                            else
                            {
                                var buf = new StringBuilder(256);
                                ToUnicode((uint)keyboardData.Keyboard.VirutalKey, 0, new byte[256], buf, 256, 0);
                                ChipId += buf.ToString();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var message = ex.Message;

                    if (ex is AggregateException aex)
                    {
                        foreach (var iex in aex.InnerExceptions)
                        {
                            message += $" {iex.Message}";
                        }
                    }
                    var item = new ListViewItem("Err");
                    item.SubItems.Add("");
                    item.SubItems.Add("");
                    item.SubItems.Add(message);
                    
                    listView1.Items.Insert(0, item);

                    ChipId = string.Empty;
                    return;
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

        private void listView1_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }
    }
}
