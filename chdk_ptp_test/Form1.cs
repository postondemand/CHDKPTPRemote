// Copyright Muck van Weerdenburg 2011.
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)

namespace chdk_ptp_test
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Windows.Forms;

    using CHDKPTP;

    using CHDKPTPRemote;

    using LibUsbDotNet;

    public partial class Form1 : Form
    {
        private bool connected;

        private CHDKPTPDevice connected_device;

        private int display_width, display_height;

        private readonly Bitmap live_image = null;

        private readonly Bitmap live_overlay = null;

        private readonly StreamWriter Log;

        private Session session;

        public Form1()
        {
            this.InitializeComponent();
            this.Log = File.AppendText("chdk_ptp_test.log");
            this.LogLine("=== program started ===");
            UsbDevice.UsbErrorEvent += this.UsbDevice_UsbErrorEvent;
        }

        private void refresh_camera_list()
        {
            this.LogLine("refreshing camera list...");
            this.devicecombobox.Items.Clear();
            this.devicecombobox.Text = "<select a device>";

            try
            {
                foreach (var dev in Session.ListDevices(false))
                {
                    this.LogLine(
                        "found device: " + dev.Name
                        + (dev.CHDKSupported ? " (CHDK PTP supported)" : dev.PTPSupported ? " (PTP supported)" : ""));
                    if (dev.PTPSupported && !dev.CHDKSupported && dev.CHDKVersionMajor != -1)
                    {
                        this.LogLine("CHDK version: " + dev.CHDKVersionMajor + "." + dev.CHDKVersionMinor);
                    }
                    if (dev.CHDKSupported)
                    {
                        this.devicecombobox.Items.Add(dev);
                    }
                }
                this.LogLine("done.");
            }
            catch (Exception ex)
            {
                this.LogLine("exception: " + ex.Message + Environment.NewLine + ex.StackTrace);
                MessageBox.Show("could not open PTP session: " + ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        private void LogLine(string s)
        {
            this.Log.WriteLine(s);
            this.Log.Flush();
        }

        private void UsbDevice_UsbErrorEvent(object sender, UsbError e)
        {
            this.LogLine("usb error: " + e);
            MessageBox.Show("UsbError: " + e);
        }

        ~Form1()
        {
            this.LogLine("closing...");
            UsbDevice.Exit();
            this.LogLine("=== program ended ===");
        }

        private void refreshbutton_Click(object sender, EventArgs e)
        {
            this.refresh_camera_list();
        }

        private void connectbutton_Click(object sender, EventArgs e)
        {
            if (this.connected)
            {
                MessageBox.Show("Already opened a device!", "Error");
                return;
            }

            if (this.devicecombobox.SelectedItem == null)
            {
                MessageBox.Show("No device selected!", "Error");
                return;
            }

            this.connected_device = this.devicecombobox.SelectedItem as CHDKPTPDevice;

            this.LogLine("opening device: " + this.connected_device.Name);
            try
            {
                this.session = new Session(this.connected_device);
                this.session.Connect();
            }
            catch (Exception ex)
            {
                this.LogLine("exception: " + ex.Message + Environment.NewLine + ex.StackTrace);
                this.connected = false;
                this.connected_device = null;
                this.session = null;
                MessageBox.Show("could not open PTP session: " + ex.Message + "\n\n" + ex.StackTrace);
                return;
            }
            this.LogLine("connected.");
            this.connected = true;
            this.statuslabel.Text = "Connected to: " + this.connected_device;
        }

        private void disconnectbutton_Click(object sender, EventArgs e)
        {
            if (this.connected)
            {
                this.LogLine("closing connection...");
                try
                {
                    this.session.Disconnect();
                }
                catch (Exception ex)
                {
                    this.LogLine("exception: " + ex.Message + Environment.NewLine + ex.StackTrace);
                }

                this.LogLine("closed.");
                this.statuslabel.Text = "Not connected";
                this.connected_device = null;
                this.connected = false;
            }
        }

        private void getimagebutton_Click(object sender, EventArgs e)
        {
            if (!this.connected)
            {
                return;
            }
        }

        private void recordbutton_Click(object sender, EventArgs e)
        {
            if (!this.connected)
            {
                return;
            }

            this.LogLine("switching to record mode...");
            try
            {
                this.session.ExecuteScript("switch_mode_usb(1)");
            }
            catch (Exception ex)
            {
                this.LogLine("exception: " + ex.Message + Environment.NewLine + ex.StackTrace);
                MessageBox.Show("could not switch to record mode: " + ex.Message + "\n\n" + ex.StackTrace);
                return;
            }
            this.LogLine("done.");
        }

        private void playbackbutton_Click(object sender, EventArgs e)
        {
            if (!this.connected)
            {
                return;
            }

            this.LogLine("switching to playback mode...");
            try
            {
                this.session.ExecuteScript("switch_mode_usb(0)");
            }
            catch (Exception ex)
            {
                this.LogLine("exception: " + ex.Message + Environment.NewLine + ex.StackTrace);
                MessageBox.Show("could not switch to playback mode: " + ex.Message + "\n\n" + ex.StackTrace);
                return;
            }
            this.LogLine("done.");
        }

        private void shutdownbutton_Click(object sender, EventArgs e)
        {
            if (!this.connected)
            {
                return;
            }

            this.LogLine("shutting camera down... (may result in exceptions due to loss of connection)");
            try
            {
                this.session.ExecuteScript("shut_down()");
            }
            catch (Exception ex)
            {
                this.LogLine("exception: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            this.disconnectbutton.PerformClick();
            this.LogLine("shut down complete.");
        }

        private void execbutton_Click(object sender, EventArgs e)
        {
            if (!this.connected)
            {
                return;
            }

            this.LogLine("executing script: " + this.scriptedit.Text);
            try
            {
                var r = this.session.ExecuteScript(this.scriptedit.Text);
                if (r == null)
                {
                    this.outputlabel.Text = "(none)";
                }
                else if (r.GetType() == typeof(bool))
                {
                    this.outputlabel.Text = r.ToString();
                }
                else if (r.GetType() == typeof(int))
                {
                    this.outputlabel.Text = r.ToString();
                }
                else if (r.GetType() == typeof(string))
                {
                    this.outputlabel.Text = (string)r;
                }
                else
                {
                    this.outputlabel.Text = "(unsupported type)";
                }
            }
            catch (Exception ex)
            {
                this.LogLine("exception: " + ex.Message + Environment.NewLine + ex.StackTrace);
                this.outputlabel.Text = ex.Message;
            }
            this.LogLine("done.");
        }

        private void scriptedit_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\n')
            {
                this.execbutton.PerformClick();
            }
        }

        private void overlaybutton_Click(object sender, EventArgs e)
        {
            if (!this.connected)
            {
                return;
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (this.live_image != null)
            {
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(
                    this.live_image,
                    this.getimagebutton.Left,
                    this.getimagebutton.Bottom + 10,
                    this.display_width,
                    this.display_height);
            }
            if (this.live_overlay != null)
            {
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(
                    this.live_overlay,
                    this.getimagebutton.Left,
                    this.getimagebutton.Bottom + 10,
                    this.display_width,
                    this.display_height);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // TODO: Load doesn't seem the right place as we don't get usb error messages here
            this.refresh_camera_list();
        }
    }
}