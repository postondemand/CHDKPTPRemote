// Copyright Muck van Weerdenburg 2011.
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)

namespace PTP
{
    public class PTPSession
    {
        protected PTPCommunication ptp;

        public PTPSession(PTPDevice dev)
        {
            this.device = dev;
            this.ptp = new PTPCommunication(this.device);
            this.IsOpen = false;
        }

        public PTPDevice device { get; private set; }

        public bool IsOpen { get; private set; }

        public void SendCommand(
            PTP_Operation op,
            int num_params,
            int param1 = 0,
            int param2 = 0,
            int param3 = 0,
            int param4 = 0,
            int param5 = 0)
        {
            //ptp.ResetParams(); //not needed as all params are set anyway
            this.ptp.Code = (ushort)op;
            this.ptp.NParams = num_params; // perhaps check for value value
            this.ptp.Param1 = param1;
            this.ptp.Param2 = param2;
            this.ptp.Param3 = param3;
            this.ptp.Param4 = param4;
            this.ptp.Param5 = param5;

            this.ptp.Send();
        }

        public void SendCommand(
            PTP_Operation op,
            byte[] data,
            int num_params,
            int param1 = 0,
            int param2 = 0,
            int param3 = 0,
            int param4 = 0,
            int param5 = 0)
        {
            //ptp.ResetParams(); //not needed as all params are set anyway
            this.ptp.Code = (ushort)op;
            this.ptp.NParams = num_params; // perhaps check for value value
            this.ptp.Param1 = param1;
            this.ptp.Param2 = param2;
            this.ptp.Param3 = param3;
            this.ptp.Param4 = param4;
            this.ptp.Param5 = param5;

            this.ptp.Send(data);
        }

        public void SendCommand(
            PTP_Operation op,
            out byte[] data,
            int num_params,
            int param1 = 0,
            int param2 = 0,
            int param3 = 0,
            int param4 = 0,
            int param5 = 0)
        {
            //ptp.ResetParams(); //not needed as all params are set anyway
            this.ptp.Code = (ushort)op;
            this.ptp.NParams = num_params; // perhaps check for value value
            this.ptp.Param1 = param1;
            this.ptp.Param2 = param2;
            this.ptp.Param3 = param3;
            this.ptp.Param4 = param4;
            this.ptp.Param5 = param5;

            this.ptp.Send(out data);
        }

        public void Ensure_PTP_RC_OK()
        {
            if (this.ptp.Code != (ushort)PTP_Response.PTP_RC_OK)
            {
                throw new PTPException(
                    "could not get perform PTP operation (unexpected return code 0x" + this.ptp.Code.ToString("X4")
                    + ")");
            }
        }

        public void OpenSession()
        {
            this.SendCommand(PTP_Operation.PTP_OC_OpenSession, 1, 1);
            this.Ensure_PTP_RC_OK();

            this.IsOpen = true;
        }

        public void CloseSession()
        {
            this.SendCommand(PTP_Operation.PTP_OC_CloseSession, 0);
            this.Ensure_PTP_RC_OK();

            this.IsOpen = false;
        }
    }
}