using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;
using System.Management;
using System.IO;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Threading;
using hid;


namespace SMeter
{
    /*
     *  The Model 
     */
    public class Communicator
    {
        // Instance variables
        public HidUtility HidUtil { get; set; }
        private ushort _Vid;
        private ushort _Pid;
        public List<byte> PacketsToRequest { get; set; }
        private List<UsbCommand> PendingCommands;
        public uint TxCount { get; private set; }
        public uint TxFailedCount { get; private set; }
        public uint RxCount { get; private set; }
        public uint RxFailedCount { get; private set; }
        public bool WaitingForDevice { get; private set; }
        //Information obtained from the device
        public Int16 CurrentMeasurementAdc { get; private set; }
        public Int16 CurrentMeasurement { get; private set; }
        public Int16[] CalibrationValues { get; private set; } = new Int16[14];
        public string DebugString { get; private set; }

        public class UsbCommand
        {
            public byte command { get; set; }
            public List<byte> data { get; set; }

            public UsbCommand(byte command, byte index, Int16 value)
            {
                this.command = command;
                this.data = new List<byte>();
                this.data.Add(index);
                foreach (byte b in BitConverter.GetBytes(value))
                {
                    this.data.Add(b);
                }
            }

            public List<byte> GetByteList()
            {
                List<byte> ByteList = new List<byte>();
                ByteList.Add(command);
                foreach (byte b in data)
                {
                    ByteList.Add(b);
                }
                return ByteList;
            }
        } // End of UsbCommand

        //Others
        private bool _NewDataAvailable;

        public Communicator()
        {
            // Initialize variables
            TxCount = 0;
            TxFailedCount = 0;
            RxCount = 0;
            RxFailedCount = 0;
            PendingCommands = new List<UsbCommand>();
            PacketsToRequest = new List<byte>();
            PacketsToRequest.Add(0x10);
            WaitingForDevice = false;
            _NewDataAvailable = false;

            // Obtain and initialize an instance of HidUtility
            HidUtil = new HidUtility();

            // Subscribe to HidUtility events
            HidUtil.RaiseConnectionStatusChangedEvent += ConnectionStatusChangedHandler;
            HidUtil.RaiseSendPacketEvent += SendPacketHandler;
            HidUtil.RaisePacketSentEvent += PacketSentHandler;
            HidUtil.RaiseReceivePacketEvent += ReceivePacketHandler;
            HidUtil.RaisePacketReceivedEvent += PacketReceivedHandler;
        }

        //Convert binary coded decimal byte to integer
        private uint BcdToUint(byte bcd)
        {
            uint lower = (uint)(bcd & 0x0F);
            uint upper = (uint)(bcd >> 4);
            return (10 * upper) + lower;
        }

        //Convert integer to binary encoded decimal byte
        private byte UintToBcd(uint val)
        {
            uint lower = val % 10;
            uint upper = val / 10;
            byte retval = (byte)upper;
            retval <<= 4;
            retval |= (byte)lower;
            return retval;
        }

        //Accessors for the UI to call
        public bool NewDataAvailable
        {
            get
            {
                if (_NewDataAvailable)
                {
                    _NewDataAvailable = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        //Function to parse packet received over USB
        /*
    ToSendDataBuffer[1] = (uint8_t) os.adc_values[os.timeSlot&0b00001111]; //LSB
    ToSendDataBuffer[2] = (uint8_t) (os.adc_values[os.timeSlot&0b00001111] >> 8); //MSB
    ToSendDataBuffer[3] = (uint8_t) os.adc_sum; //LSB
    ToSendDataBuffer[4] = (uint8_t) (os.adc_sum >> 8);
    ToSendDataBuffer[5] = (uint8_t) (os.adc_sum >> 16);
    ToSendDataBuffer[6] = (uint8_t) (os.adc_sum >> 24); //MSB
    ToSendDataBuffer[7] = (uint8_t) os.db_value; //LSB
    ToSendDataBuffer[8] = (uint8_t) (os.db_value >> 8);
    ToSendDataBuffer[9] = os.s_value;
    ToSendDataBuffer[10] = os.s_fraction;
        */
        private void ParseData(ref UsbBuffer InBuffer)
        {
            //Input values are encoded as Int16

            //CurrentMeasurementAdc = (Int16)((InBuffer.buffer[3] << 8) + InBuffer.buffer[2]);
            CurrentMeasurement = (Int16)((InBuffer.buffer[9] << 8) + InBuffer.buffer[8]);
            /*
            for(int i=0; i<CalibrationValues.Length; ++i)
            {
                CalibrationValues[i] = (Int16)((InBuffer.buffer[2*i+7] << 8) + InBuffer.buffer[2*i+6]);
            }
            */
            //New status data is now available
            _NewDataAvailable = true;
        }

        // Accessor for _Vid
        // Only update selected device if the value has actually changed
        public ushort Vid
        {
            get
            {
                return _Vid;
            }
            set
            {
                if (value != _Vid)
                {
                    _Vid = value;
                    HidUtil.SelectDevice(new Device(_Vid, _Pid));
                }
            }
        }

        // Accessor for _Pid
        // Only update selected device if the value has actually changed
        public ushort Pid
        {
            get
            {
                return _Pid;
            }
            set
            {
                if (value != _Pid)
                {
                    _Pid = value;
                    HidUtil.SelectDevice(new Device(_Vid, _Pid));
                }
            }
        }

        /*
         * HidUtility callback functions
         */

        public void ConnectionStatusChangedHandler(object sender, HidUtility.ConnectionStatusEventArgs e)
        {
            if (e.ConnectionStatus != HidUtility.UsbConnectionStatus.Connected)
            {
                // Reset variables
                _NewDataAvailable = false;
                TxCount = 0;
                TxFailedCount = 0;
                RxCount = 0;
                RxFailedCount = 0;
                PendingCommands = new List<UsbCommand>();
                PacketsToRequest = new List<byte>();
                PacketsToRequest.Add(0x10);
                WaitingForDevice = false;
            }
        }

        // HidUtility asks if a packet should be sent to the device
        // Prepare the buffer and request a transfer
        public void SendPacketHandler(object sender, UsbBuffer OutBuffer)
        {
            DebugString = "Start SendPacketHandler";
            // Fill entire buffer with 0xFF
            OutBuffer.clear();

            // The first byte is the "Report ID" and does not get sent over the USB bus.  Always set = 0.
            OutBuffer.buffer[0] = 0x00;

            //Prepare data to send
            byte NextPacket;
            if (PacketsToRequest.Count >= 1)
            {
                NextPacket = PacketsToRequest[0];
                PacketsToRequest.RemoveAt(0);
            }
            else
            {
                NextPacket = 0x10;
            }
            OutBuffer.buffer[1] = NextPacket;
            PacketsToRequest.Add(NextPacket);

            int position = 2;
            while ((position <= 64) && (PendingCommands.Count > 0))
            {
                List<byte> CommandBytes = PendingCommands[0].GetByteList();

                //Check if entire command fits into current buffer
                if ((64 - position) >= CommandBytes.Count)
                {
                    foreach (byte b in CommandBytes)
                    {
                        OutBuffer.buffer[position] = b;
                        ++position;
                    }
                    PendingCommands.RemoveAt(0);
                }
                else
                {
                    position += CommandBytes.Count;
                    break;
                }
            }

            //Request the packet to be sent over the bus
            OutBuffer.RequestTransfer = true;
            DebugString = "End SendPacketHandler";
        }

        // HidUtility informs us if the requested transfer was successful
        // Schedule to request a packet if the transfer was successful
        public void PacketSentHandler(object sender, UsbBuffer OutBuffer)
        {
            DebugString = "Start PacketSentHandler";
            WaitingForDevice = OutBuffer.TransferSuccessful;
            if (OutBuffer.TransferSuccessful)
            {
                ++TxCount;
            }
            else
            {
                ++TxFailedCount;
            }
            DebugString = "End PacketSentHandler";
        }

        // HidUtility asks if a packet should be requested from the device
        // Request a packet if a packet has been successfully sent to the device before
        public void ReceivePacketHandler(object sender, UsbBuffer InBuffer)
        {
            DebugString = "Start ReceivePacketHandler";
            WaitingForDevice = true;
            InBuffer.RequestTransfer = WaitingForDevice;
            DebugString = "End ReceivePacketHandler";
        }

        // HidUtility informs us if the requested transfer was successful and provides us with the received packet
        public void PacketReceivedHandler(object sender, UsbBuffer InBuffer)
        {
            DebugString = "Start PacketReceivedHandler";
            WaitingForDevice = false;

            //Parse received data
            switch (InBuffer.buffer[1])
            {
                case 0x10:
                    ParseData(ref InBuffer);
                    break;
            };

            //Some statistics
            if (InBuffer.TransferSuccessful)
            {
                ++RxCount;
            }
            else
            {
                ++RxFailedCount;
            }
            DebugString = "End PacketReceivedHandler";
        }

        public bool RequestValid()
        {
            return true;
        }

        public void ScheduleCommand(UsbCommand cmd)
        {
            PendingCommands.Add(cmd);
        }

    } // Communicator

}
