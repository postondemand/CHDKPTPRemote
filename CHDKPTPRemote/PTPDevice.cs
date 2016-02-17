// Copyright Muck van Weerdenburg 2011.
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)

namespace PTP
{
    using LibUsbDotNet;
    using LibUsbDotNet.Main;

    public class PTPDevice
    {
        protected string _Name;

        public byte ConfigurationID;

        public int InterfaceID;

        public bool PTPSupported;

        public UsbEndpointReader Reader;

        public ReadEndpointID ReaderEndpointID;

        public UsbEndpointWriter Writer;

        public WriteEndpointID WriterEndpointID;

        public PTPDevice(UsbDevice dev)
        {
            this.Device = dev;
            this.PTPSupported = false;
            this._Name = dev.Info.ProductString; // TODO: try get better name
            this.Reader = null;
            this.Writer = null;
            this.ConfigurationID = 1;
            this.InterfaceID = 0;
            this.ReaderEndpointID = ReadEndpointID.Ep01;
            this.WriterEndpointID = WriteEndpointID.Ep02;
        }

        public UsbDevice Device { get; private set; }

        public bool IsOpen
        {
            get
            {
                return this.Device.IsOpen;
            }
        }

        public string Name
        {
            get
            {
                return this._Name;
            }
        }

        ~PTPDevice()
        {
            if (this.IsOpen)
            {
                this.Close();
            }
        }

        public bool Open()
        {
            if (this.IsOpen)
            {
                return false;
            }

            if (!this.Device.Open())
            {
                return false;
            }

            var whole = this.Device as IUsbDevice;
            if (!ReferenceEquals(whole, null))
            {
                if (!whole.SetConfiguration(this.ConfigurationID) || !whole.ClaimInterface(this.InterfaceID))
                {
                    this.Device.Close();
                    throw new PTPException(
                        "could not set USB device configuration and interface to " + this.ConfigurationID + " and "
                        + this.InterfaceID + ", respectively");
                }
            }

            this.Writer = this.Device.OpenEndpointWriter(this.WriterEndpointID);
            this.Reader = this.Device.OpenEndpointReader(this.ReaderEndpointID);

            return true;
        }

        public bool Close()
        {
            if (!this.IsOpen)
            {
                return false;
            }

            var whole = this.Device as IUsbDevice;
            if (!ReferenceEquals(whole, null))
            {
                whole.ReleaseInterface(this.InterfaceID);
            }

            return this.Device.Close();
        }

        public override string ToString()
        {
            return this.Device.Info.ProductString;
        }
    }
}