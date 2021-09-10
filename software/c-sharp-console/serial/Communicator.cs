﻿// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace c_sharp_console.serial
{
    public delegate void MessageReceived(MessageType type, byte value);

    public class Communicator
    {
        private const string PORT_NAME = "COM3";
        private const int BAUD_RATE = 9600;

        private const byte MSG_START_MARK = 0x11;
        private const byte MSG_END_MARK = 0x12;

        private readonly Queue<byte> _messageQueue = new Queue<byte>();

        public event MessageReceived OnMessageReceived;
        public bool IsOpen => _serialPort?.IsOpen ?? false;

        private SerialPort _serialPort;
        private MessagePosition _currentMessagePosition = MessagePosition.None;
        private MessageType _currentMessageType;
        private byte _currentMessageValue;

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var serialPort = (SerialPort)sender;

            var data = new byte[serialPort.BytesToRead];
            serialPort.Read(data, 0, data.Length);

            lock (_messageQueue)
            {
                for (var index = 0; index < data.Length; index++)
                {
                    _messageQueue.Enqueue(data[index]);
                }
            }

            Process();
        }

        private static bool TryParseMessageType(byte value, out MessageType result)
        {
            if (!Enum.IsDefined(typeof(MessageType), value))
            {
                result = MessageType.NONE;
                return false;
            }

            result = (MessageType)value;
            return true;
        }

        public void Start()
        {
            _serialPort = new SerialPort(PORT_NAME, BAUD_RATE);
            _serialPort.DataReceived += OnDataReceived;
            _serialPort.Open();
        }

        public void Write(MessageType type)
        {
            var bytes = new[] { (byte)type };
            _serialPort.Write(bytes, 0, 1);
        }

        private void DequeueOne()
        {
            byte item;
            lock (_messageQueue)
            {
                if (_messageQueue.Count == 0)
                {
                    return;
                }

                item = _messageQueue.Dequeue();
            }

            switch (_currentMessagePosition)
            {
                case MessagePosition.None:
                    _currentMessagePosition = item == MSG_START_MARK ? MessagePosition.Start : MessagePosition.None;
                    break;
                case MessagePosition.Start:
                    _currentMessagePosition = TryParseMessageType(item, out _currentMessageType) ? MessagePosition.Type : MessagePosition.None;
                    break;
                case MessagePosition.Type:
                    _currentMessageValue = item;
                    _currentMessagePosition = MessagePosition.Value;
                    break;
                case MessagePosition.Value:
                    _currentMessagePosition = item == MSG_END_MARK ? MessagePosition.End : MessagePosition.None;
                    break;
                case MessagePosition.End:
                    throw new Exception("End of message wasn't processed.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CheckMessage()
        {
            if (_currentMessagePosition != MessagePosition.End)
            {
                return;
            }

            OnMessageReceived?.Invoke(_currentMessageType, _currentMessageValue);

            _currentMessagePosition = MessagePosition.None;
            _currentMessageType = MessageType.NONE;
            _currentMessageValue = 0x00;
        }

        public void Process()
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count > 0)
                {
                    DequeueOne();
                    CheckMessage();
                }
            }
        }

        public void Stop()
        {
            _serialPort.Close();
        }
    }
}