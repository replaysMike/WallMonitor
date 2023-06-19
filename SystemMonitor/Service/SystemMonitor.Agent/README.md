# SystemMonitor Agent

This application serves hardware information about the host it is installed on.
It runs as a service on both Windows and Unix environments.

## Instrumentation Info

This agent collects the following information: Cpu usage, Memory free and total memory, Disk free and disk size.
It also collects specific hardware information such as processor type, motherboard information.

The agent exposes the following performance counters on Windows (when not in a Docker container): `SystemMonitor` => `Memory Available` and `CPU Processor Time %`
