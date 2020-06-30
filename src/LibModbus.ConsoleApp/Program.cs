using System;
using System.Linq;
using System.Threading.Tasks;

namespace LibModbus.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var address = Environment.GetEnvironmentVariable("WORKER_ADDRESS");

            await using var client = new ModbusClientBuilder()
                                    .UseSocket(address, 5020)
                                    .Build();

            await client.ConnectAsync();

            await client.WriteSingleCoil(8, true);

            Console.WriteLine();

            await ReadCoilsAndLog(client, 0, 10);

            var states = new bool[20];

            states[2] = true;
            states[3] = true;

            await client.WriteMultipleCoils(0, states);

            Console.WriteLine();
            await ReadCoilsAndLog(client, 0, 10);

            await Task.Delay(1000);

            var data = await client.ReadCoils(8, 1);
            var value = data.FirstOrDefault();

            Console.WriteLine($"Current coil value for register 8: {value}");

            var result = await client.WriteSingleCoil(8, !value);

            Console.WriteLine($"Written coil value to {result}");

            await Task.Delay(1000);

            Console.WriteLine();
            Console.WriteLine("Second Read");
            await ReadCoilsAndLog(client, 0, 20);
        }

        static async Task ReadCoilsAndLog(ModbusClient client, int start, int end)
        {
            var coils = await client.ReadCoils((ushort)start, (ushort)end);

            var index = start;
            foreach (var coil in coils)
            {
                var onOrOff = coil ? "on" : "off";
                Console.WriteLine($"Coil {index++} is {onOrOff}");
            }
        }
    }
}
