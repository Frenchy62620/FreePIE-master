﻿using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using FreePIE.Core.Contracts;

namespace FreePIE.Core.Plugins
{
    [GlobalType(Type = typeof(AhrsImuGlobal))]
    public class AhrsImuPlugin : ComDevicePlugin
    {
        private byte[] buffer;

        public override object CreateGlobal()
        {
            return new AhrsImuGlobal(this);
        }

        protected override int DefaultBaudRate
        {
            get { return 57600; }
        }

        protected override void Init(SerialPort serialPort)
        {
            Thread.Sleep(3000); //Wait for IMU to self init
            serialPort.ReadExisting();

            serialPort.Write("#ob"); // Turn on binary output
            serialPort.Write("#o1"); // Turn on continuous streaming output
            serialPort.Write("#oe0"); // Disable error message output
            serialPort.Write("#s00"); //Request sync signal

            const string sync = "#SYNCH00\r";

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (serialPort.BytesToRead < sync.Length && !serialPort.ReadLine().Contains(sync))
            {
                if (stopwatch.ElapsedMilliseconds > 100)
                    throw new Exception($"No hardware connected to port {port} with AHRS IMU protocol");
            }
            stopwatch.Stop();
            serialPort.ReadExisting(); //Sometimes there are garbage after the syncsignal
            buffer = new byte[4];
        }

        protected override void Read(SerialPort serialPort)
        {
            while (serialPort.BytesToRead >= 12)
            {
                var data = Data;
                data.Yaw = ReadFloat(serialPort, buffer);
                data.Pitch = ReadFloat(serialPort, buffer);
                data.Roll = ReadFloat(serialPort, buffer);

                Data = data;
                newData = true;
            }

            Thread.Sleep(1);
        }

        protected override string BaudRateHelpText
        {
            get { return "Baud rate, default on AHRS should be 57600"; }
        }
        
        public override string FriendlyName
        {
            get { return "AHRS IMU"; }
        }

        private float ReadFloat(SerialPort port, byte[] buffer)
        {
            port.Read(buffer, 0, buffer.Length);
            var value = BitConverter.ToSingle(buffer, 0);
            return value;
        }
    }

    [Global(Name = "ahrsImu")]
    public class AhrsImuGlobal : DofGlobal<AhrsImuPlugin>
    {
        public AhrsImuGlobal(AhrsImuPlugin plugin) : base(plugin) { }
    }
}
