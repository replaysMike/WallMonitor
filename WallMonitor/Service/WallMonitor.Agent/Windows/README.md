# Windows Specific Instrumentation

Through extensive testing I had to mostly rule out Performance Counters and WMI as they don't work as intended in Windows containers.

## Resources

* [CPU Stress Testing tool](https://pylonos.com/sp/download)
* [Win32 GetProcessTimes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocesstimes)
* [Win32 GetSystemTimes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getsystemtimes)
* [PInvoke GetProcessTimes](https://www.pinvoke.net/default.aspx/kernel32.getprocesstimes)
* [Docker container detection](https://www.hanselman.com/blog/detecting-that-a-net-core-app-is-running-in-a-docker-container-and-skippablefacts-in-xunit)

## Cpu Usage

Implementation is based on [CPU usage via Win32](https://www.philosophicalgeek.com/2009/01/03/determine-cpu-usage-of-current-process-c-and-c/) and 
[using Win32 GetSystemTimes](https://www.codeproject.com/Articles/9113/Get-CPU-Usage-with-GetSystemTimes) adapted from there. 
Performance counters and WMI were not an option as they don't function properly in containers. Needed a lower level O/S way of accurately calculating CPU usage, 
this was a difficult project to get right as there were bugs in the example code that were hard to identify and fix.
