using System;
using System.Threading;
using c_sharp_console.serial;

namespace c_sharp_console
{
    internal class Program
    {
        private const int UpdateMs = 1000;

        private static readonly Timer Timer;
        private static readonly Communicator Communicator;

        static Program()
        {
            Timer = new Timer(Update, null, UpdateMs, UpdateMs);
            Communicator = new Communicator();
            Communicator.OnMessageReceived += OnMessageReceived;
        }

        private static void Main(string[] args)
        {
            Communicator.Start();
            ReadCommand();
            Communicator.Stop();
        }

        private static void OnMessageReceived(MessageType type, byte value)
        {
            switch (type)
            {
                case MessageType.STATUS_RESPONSE:
                    var state = (DeskState)value;
                    Console.WriteLine($"Desk state is: {state}");
                    break;
                case MessageType.DEBUG_RESPONSE:
                    //Console.WriteLine($"############  DESK DEBUG MSG: {value}");
                    break;
                default:
                    Console.WriteLine($"Message received: {type}, value: {value}");
                    break;
            }
        }

        private static void ReadCommand()
        {
            while (true)
            {
                var input = Console.ReadKey();

                switch (input.Key)
                {
                    case ConsoleKey.End:
                        Communicator.Write(MessageType.STOP_REQUEST);
                        break;
                    case ConsoleKey.UpArrow:
                        Communicator.Write(MessageType.UP_REQUEST);
                        break;
                    case ConsoleKey.DownArrow:
                        Communicator.Write(MessageType.DOWN_REQUEST);
                        break;
                    case ConsoleKey.Escape:
                        return;
                    default:
                        Console.WriteLine($"No matching command for key: {input.Key}");
                        break;
                }
            }
        }

        private static void Update(object o)
        {
            Communicator.Write(MessageType.STATUS_REQUEST);
        }
    }
}
