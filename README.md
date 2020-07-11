# LibModbus

[![Build status][ci-badge]][ci-url]
[![NuGet][nuget-package-badge]][nuget-package-url]
[![feedz.io][feedz-package-badge]][feedz-package-url]

Library to send/receive data with a device which respects the Modbus protocol for dotnet.

## Install

Install the [NuGet package][nuget-package-url] into your project.

```
PM> Install-Package LibModbus
```

```
$ dotnet add package LibModbus
```

## Supported Functions

- [X] Read Coils (FC1)
- [X] Read Discrete Inputs (FC2)
- [X] Read Holding Registers (FC3)
- [X] Read Input Registers (FC4)
- [X] Write Single Coil (FC5)
- [ ] Write Single Register (FC6)
- [X] Write Multiple Coils (FC15)
- [ ] Write Multiple Registers (FC16)

## Usage

### Client Builder

```csharp
var address = "127.0.0.1";
await using var client = new ModbusClientBuilder()
                        .UseSocket(address)
                        .Build();
```

### Connect And Read Coils
```csharp
await client.ConnectAsync();

var coils = await client.ReadCoils(2, 10);

var index = 2;
foreach (var coil in coils)
{
    var onOrOff = coil ? "on" : "off";
    Console.WriteLine($"Coil {index++} is {onOrOff}");
}
```

## Protocol-Specification

- [MODBUS Application Protocol Specification v1.1b3 (PDF)](http://modbus.org/docs/Modbus_Application_Protocol_V1_1b3.pdf)
- [MODBUS over serial line specification and implementation guide v1.02 (PDF)](http://modbus.org/docs/Modbus_over_serial_line_V1_02.pdf)
- [MODBUS Messaging on TCP/IP Implementation Guide v1.0b (PDF)](http://modbus.org/docs/Modbus_Messaging_Implementation_Guide_V1_0b.pdf)

## License

Copyright 2020 [Nick Van Dyck](https://nvd.codes)

MIT

[ci-url]: https://github.com/nickvdyck/libmodbus.cs
[ci-badge]: https://github.com/nickvdyck/libmodbus.cs/workflows/Main/badge.svg

[nuget-package-url]: https://www.nuget.org/packages/libmodbus/
[nuget-package-badge]: https://img.shields.io/nuget/v/libmodbus.svg?style=flat-square&label=nuget

[feedz-package-url]: https://f.feedz.io/nvd/libmodbus/packages/libmodbus/latest/download
[feedz-package-badge]: https://img.shields.io/badge/endpoint.svg?url=https%3A%2F%2Ff.feedz.io%2Fnvd%2Flibmodbus%2Fshield%2Flibmodbus%2Flatest&label=libmodbus
