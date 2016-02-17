// Copyright Muck van Weerdenburg 2011.
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)

namespace PTP
{
    using System;
    using System.Runtime.InteropServices;

    using LibUsbDotNet.Main;

    // TODO: try to ensure connection is not unnecessarily messed up after error
    public class PTPCommunication
    {
        public int NParams;

        private readonly IntPtr p_ptpdata;

        private readonly IntPtr p_reqres;

        private PTPData ptpdata;

        // where we store the actual data to be sent/received
        private PTPReqRes reqres;

        public PTPCommunication(PTPDevice dev)
        {
            this.device = dev;

            this.reqres = new PTPReqRes();
            this.reqres.data = new byte[484];
            this.ptpdata = new PTPData();
            this.ptpdata.data = new byte[500];
            this.p_reqres = Marshal.AllocHGlobal(Marshal.SizeOf(this.reqres));
            this.p_ptpdata = Marshal.AllocHGlobal(Marshal.SizeOf(this.ptpdata));

            this.ResetAll();
        }

        // PTP Request/Response contents
        // XXX might need to do little endian to/from big endian conversions!
        public ushort Code
        {
            get
            {
                return this.reqres.Code;
            }
            set
            {
                this.reqres.Code = value;
            }
        }

        //public int SessionId; // never really used!
        private uint TransactionId
        {
            get
            {
                return this.reqres.TransactionId;
            }
            set
            {
                this.reqres.TransactionId = value;
            }
        } // not really useful as public

        public int Param1
        {
            get
            {
                return this.reqres.Param1;
            }
            set
            {
                this.reqres.Param1 = value;
            }
        }

        public int Param2
        {
            get
            {
                return this.reqres.Param2;
            }
            set
            {
                this.reqres.Param2 = value;
            }
        }

        public int Param3
        {
            get
            {
                return this.reqres.Param3;
            }
            set
            {
                this.reqres.Param3 = value;
            }
        }

        public int Param4
        {
            get
            {
                return this.reqres.Param4;
            }
            set
            {
                this.reqres.Param4 = value;
            }
        }

        public int Param5
        {
            get
            {
                return this.reqres.Param5;
            }
            set
            {
                this.reqres.Param5 = value;
            }
        }

        public PTPDevice device { get; private set; }

        ~PTPCommunication()
        {
            Marshal.FreeHGlobal(this.p_ptpdata);
            Marshal.FreeHGlobal(this.p_reqres);
        }

        public void ResetAll()
        {
            this.Code = 0;
            //SessionId = 1; // not used
            this.TransactionId = 0;

            this.ResetParams();
        }

        public void ResetParams()
        {
            this.Param1 = 0;
            this.Param2 = 0;
            this.Param3 = 0;
            this.Param4 = 0;
            this.Param5 = 0;
            this.NParams = 0;
        }

        // TODO: add handler for USB errors to use them in the exceptions below

        private void CheckError(ErrorCode err)
        {
            if (err != ErrorCode.None)
            {
                throw new PTPException("could not read/write data: usb error = " + err);
            }
        }

        private void CheckErrorAndLength(ErrorCode err, int len, int target_len)
        {
            if (err != ErrorCode.None)
            {
                throw new PTPException("could not read/write data: usb error = " + err);
            }
            if (len != target_len)
            {
                throw new PTPException("could not read/write all data (" + len + " bytes instead of " + target_len);
            }
        }

        private void SendRequest()
        {
            this.reqres.Length = 12 + 4 * this.NParams; // don't send unused parameters or data
            this.reqres.Type = 1; // PTP_USB_CONTAINER_COMMAND

            this.TransactionId += 1;

            Marshal.StructureToPtr(this.reqres, this.p_reqres, true);

            int len;
            ErrorCode err;

            err = this.device.Writer.Write(this.p_reqres, 0, this.reqres.Length, 5000, out len);

            this.CheckErrorAndLength(err, len, this.reqres.Length);
        }

        private void ReceiveResponse()
        {
            int len;
            ErrorCode err;

            this.ResetParams();

            err = this.device.Reader.Read(this.p_reqres, 0, Marshal.SizeOf(this.reqres), 5000, out len);

            this.CheckError(err);

            this.reqres = (PTPReqRes)Marshal.PtrToStructure(this.p_reqres, typeof(PTPReqRes));
        }

        private void SendData(byte[] data)
        {
            var count = data.Length < 500 ? data.Length : 500;

            this.ptpdata.Length = 12 + data.Length;
            this.ptpdata.Type = 2; // PTP_USB_CONTAINER_DATA
            this.ptpdata.Code = this.Code;
            this.ptpdata.TransactionId = this.TransactionId;
            Array.Copy(data, this.ptpdata.data, count);

            Marshal.StructureToPtr(this.ptpdata, this.p_ptpdata, true);

            int len;
            ErrorCode err;

            err = this.device.Writer.Write(this.p_ptpdata, 0, 12 + count, 5000, out len);

            this.CheckErrorAndLength(err, len, 12 + count);

            // small amount of data -> we're done
            // (< instead of <= because of final if)
            if (count < 500)
            {
                return;
            }

            // more data to be sent
            if (count > 500)
            {
                err = this.device.Writer.Write(data, count, data.Length - 500, 5000, out len);

                this.CheckErrorAndLength(err, len, data.Length - 500);
            }

            // must send empty packet to signal end on multiples of 512
            // (doesn't seem to happen in libptp?)
            if ((data.Length + 12) % 512 == 0)
            {
                err = this.device.Writer.Write(null, 0, 0, 5000, out len);

                this.CheckError(err);
            }
        }

        private void ReceiveData(out byte[] data)
        {
            int len;
            ErrorCode err;

            data = null;

            err = this.device.Reader.Read(this.p_ptpdata, 0, 512, 5000, out len);

            this.CheckError(err);

            this.ptpdata = (PTPData)Marshal.PtrToStructure(this.p_ptpdata, typeof(PTPData));

            if (this.ptpdata.Length <= 512)
            {
                // must read empty end package if length is multiple of 512
                if (this.ptpdata.Length == 512)
                {
                    // if 0 length is used Read does nothing, so use 512 and supply p_ptpdata in case something is received
                    err = this.device.Reader.Read(this.p_ptpdata, 0, 512, 5000, out len);
                    this.CheckErrorAndLength(err, len, 0);
                }

                data = new byte[this.ptpdata.Length - 12];
                Array.Copy(this.ptpdata.data, data, data.Length);

                return;
            }

            // N.B.: USBEndPointReader expects multiple of MaxPacketSize but does returns actually length
            var padded_remaining_length = (this.ptpdata.Length - 1) & ~0x1ff; // ((ptpdata.Length-512)+511) % 512
            var p = Marshal.AllocHGlobal(padded_remaining_length);

            try
            {
                err = this.device.Reader.Read(p, 0, padded_remaining_length, 5000, out len);
                this.CheckErrorAndLength(err, len, this.ptpdata.Length - 512);

                // must read empty end package if length is multiple of 512
                if ((len & 0x1ff) == 0)
                {
                    // if 0 length is used Read does nothing, so use 512 and supply p_ptpdata in case something is received
                    err = this.device.Reader.Read(this.p_ptpdata, 0, 512, 5000, out len);
                    this.CheckErrorAndLength(err, len, 0);
                }

                data = new byte[this.ptpdata.Length - 12];
                Array.Copy(this.ptpdata.data, data, 500);
                Marshal.Copy(p, data, 500, this.ptpdata.Length - 512);
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        public void Send()
        {
            try
            {
                this.SendRequest();
                this.ReceiveResponse();
            }
            finally
            {
                this.TransactionId += 1;
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                this.SendRequest();
                this.SendData(data);
                this.ReceiveResponse();
            }
            finally
            {
                this.TransactionId += 1;
            }
        }

        public void Send(out byte[] data)
        {
            try
            {
                this.SendRequest();
                this.ReceiveData(out data);
                this.ReceiveResponse();
            }
            finally
            {
                this.TransactionId += 1;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PTPReqRes
        {
            public int Length;

            public ushort Type;

            public ushort Code;

            public uint TransactionId;

            public int Param1;

            public int Param2;

            public int Param3;

            public int Param4;

            public int Param5;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 484)]
            public byte[] data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PTPData
        {
            public int Length;

            public ushort Type;

            public ushort Code;

            public uint TransactionId;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 500)]
            public byte[] data;
        }
    }
}