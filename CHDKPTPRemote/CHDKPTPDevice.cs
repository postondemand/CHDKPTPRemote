// Copyright Muck van Weerdenburg 2011.
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)

namespace CHDKPTP
{
    using LibUsbDotNet;

    using PTP;

    public class CHDKPTPDevice : PTPDevice
    {
        public bool CHDKSupported;

        public int CHDKVersionMajor;

        public int CHDKVersionMinor;

        public CHDKPTPDevice(UsbDevice dev)
            : base(dev)
        {
            this.CHDKVersionMajor = -1;
            this.CHDKVersionMinor = -1;
            this.CHDKSupported = false;
        }

        public override string ToString()
        {
            if (this.CHDKVersionMajor != -1 && this.CHDKVersionMinor != -1)
            {
                return base.ToString() + " (CHDK PTP v" + this.CHDKVersionMajor + "." + this.CHDKVersionMinor + ")";
            }
            return base.ToString();
        }
    }
}