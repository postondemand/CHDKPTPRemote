// Copyright Muck van Weerdenburg 2011.
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file LICENSE_1_0.txt or copy at
// http://www.boost.org/LICENSE_1_0.txt)

// XXX needed for call to CHDKPTPUtil.FindDevices

namespace CHDKPTPRemote
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;

    using CHDKPTP;

    public class Session
    {
        private readonly CHDKPTPSession _session;

        public Session(CHDKPTPDevice dev)
        {
            this._session = new CHDKPTPSession(dev);
        }

        public static List<CHDKPTPDevice> ListDevices(bool only_supported = true)
        {
            return CHDKPTPUtil.FindDevices(only_supported);
        }

        public void Connect()
        {
            this._session.device.Open();
            try
            {
                this._session.OpenSession();
            }
            catch (Exception e)
            {
                this._session.device.Close();
                throw e;
            }
        }

        public void Disconnect()
        {
            try
            {
                this._session.CloseSession();
            }
            catch (Exception)
            {
            }
            finally
            {
                this._session.device.Close();
            }
        }

        // returns return data (if any) unless get_error is true
        private object GetScriptMessage(int script_id, bool return_string_as_byte_array, bool get_error = false)
        {
            CHDK_ScriptMsgType type;
            int subtype, script_id2;
            byte[] data;
            while (true)
            {
                this._session.CHDK_ReadScriptMsg(out type, out subtype, out script_id2, out data);

                if (type == CHDK_ScriptMsgType.PTP_CHDK_S_MSGTYPE_NONE) // no more messages; no return value
                {
                    return null;
                }

                if (script_id2 != script_id) // ignore message from other scripts
                {
                    continue;
                }

                if (!get_error && type == CHDK_ScriptMsgType.PTP_CHDK_S_MSGTYPE_RET) // return info!
                {
                    switch ((CHDK_ScriptDataType)subtype)
                    {
                        case CHDK_ScriptDataType.PTP_CHDK_TYPE_BOOLEAN:
                            return (data[0] | data[1] | data[2] | data[3]) != 0;
                        case CHDK_ScriptDataType.PTP_CHDK_TYPE_INTEGER:
                            return data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24);
                        case CHDK_ScriptDataType.PTP_CHDK_TYPE_STRING:
                            if (return_string_as_byte_array)
                            {
                                return data;
                            }
                            return new ASCIIEncoding().GetString(data);
                        default:
                            throw new Exception("script returned unsupported data type: " + type);
                    }
                }

                if (type == CHDK_ScriptMsgType.PTP_CHDK_S_MSGTYPE_ERR) // hmm.. error
                {
                    if (get_error)
                    {
                        return new ASCIIEncoding().GetString(data);
                    }
                    throw new Exception("error running script: " + new ASCIIEncoding().GetString(data));
                }

                // ignore other (user) messages
            }
        }

        // TODO: should be able to distinguish "real" exceptions and script errors
        public object ExecuteScript(string script, bool return_string_as_byte_array = false)
        {
            int script_id;
            CHDK_ScriptErrorType status;
            this._session.CHDK_ExecuteScript(script, CHDK_ScriptLanguage.PTP_CHDK_SL_LUA, out script_id, out status);

            if (status == CHDK_ScriptErrorType.PTP_CHDK_S_ERRTYPE_COMPILE)
            {
                var msg = this.GetScriptMessage(script_id, false, true);
                if (msg.GetType() == typeof(string))
                {
                    throw new Exception("script compilation error: " + (string)msg);
                }
                throw new Exception("script compilation error (unknown reason)");
            }

            // wait for end
            while (true)
            {
                CHDK_ScriptStatus flags;
                this._session.CHDK_ScriptStatus(out flags);
                if (!flags.HasFlag(CHDK_ScriptStatus.PTP_CHDK_SCRIPT_STATUS_RUN))
                {
                    break;
                }

                Thread.Sleep(100);
            }

            // get result
            return this.GetScriptMessage(script_id, return_string_as_byte_array);
        }

        public byte[] DownloadFile(string filename)
        {
            byte[] buf;

            this._session.CHDK_DownloadFile(filename, out buf);

            return buf;
        }
    }
}