# Wall Monitor
[![GitHub release](https://img.shields.io/github/release/replaysMike/WallMonitor.svg)](https://GitHub.com/replaysMike/WallMonitor/releases/)
[![GitHub commits](https://img.shields.io/github/commits-since/replaysMike/WallMonitor/v1.0.2.svg)](https://GitHub.com/replaysMike/WallMonitor/commit/)
[![Github all releases](https://img.shields.io/github/downloads/replaysMike/WallMonitor/total.svg)](https://GitHub.com/replaysMike/WallMonitor/releases/)
[![GitHub contributors](https://img.shields.io/github/contributors/replaysMike/WallMonitor.svg)](https://GitHub.com/replaysMike/WallMonitor/graphs/contributors/)
[![GitHub license](https://img.shields.io/github/license/replaysMike/WallMonitor.svg)](https://github.com/replaysMike/WallMonitor/blob/master/LICENSE)

Wall Monitor is a graphical open-source network monitoring application that is cross-platform. It was designed to accommodate wall based or multiple displays used for centralized monitoring of many servers & services.

## Screenshots

![image](https://github.com/replaysMike/WallMonitor/assets/2531058/746a06c8-1abd-49fb-85ff-ea8a86987e6f)

Figure 1. Multi-server and service state monitoring graphically displayed


![image](https://github.com/replaysMike/WallMonitor/assets/2531058/52ddc59d-978f-42e6-8ea4-c09ae368e089)

Figure 2. Servers and services state are clearly indicated

## Features at a glance

* Animated UI displays many device types, multiple pages and notifications designed for full screen or windowed use (cross-platform!)
* Graphing of all services
* Notifications via email (SMTP & AWS SES), SMS, SNMP Traps
* Monitor common protocols such as TCP, UDP and ICMP services
* Application level monitoring (HTTPS, DNS, SMTP, POP3, IMAP, Databases, Processes, IP Cameras, Plex, NZBGet, DLNA etc)
* Hardware level monitoring (CPU, Memory, Disk, Queue lengths)
* Fully encrypted protocol support
* Display and monitoring services are separated to allow for many remote displays
* Distribute multiple monitoring services
* Audio alerts
* Support for both Windows & Unix operating systems
* SDK to write your own customized monitors

## Getting Started

See the [Getting Started Wiki](https://github.com/replaysMike/WallMonitor/wiki/Getting-Started) which explains the architecture and how to configure each application.

* There are 3 applications: Desktop, Monitoring Service, and Agent.
* [Download the latest release](https://github.com/replaysMike/WallMonitor/releases) for your platform.
* Configure the `appsettings.json` for the [Desktop](https://github.com/replaysMike/WallMonitor/wiki/Desktop-Configuration), [Monitoring Service](https://github.com/replaysMike/WallMonitor/wiki/Monitoring-Service-Configuration), and optionally the [Agent](https://github.com/replaysMike/WallMonitor/wiki/Agent-Configuration)

## Monitors currently supported

There are many built-in monitors that come with Wall Monitor. If you feel an important monitor is missing, create a [Feature Request](https://github.com/replaysMike/WallMonitor/discussions/categories/feature-requests) or contribute by submitting a pull-request.

* TcpPort
* Udp Multicast & Unicast message monitoring
* Http & Https
* Icmp
* Dns
* Smtp
* Imap
* Pop3
* IpCamera (RTSP, SIP, MQTT)
* Process monitoring (Windows & Unix)
* Plex
* DLNA
* NzbGet
* SqlServer
* MySql
* Postgresql
* Oracle
* Redis
* Windows WMI Queries
* Windows Services
* Windows Performance Counters
* CPU
* Memory
* Disk

## Remaining Upcoming Features for 2023

- [ ] Philips Hue Integration
- [ ] PRTG Integration
- [ ] Custom sprite loading for your own look
- [ ] SNMP Trap Receiving
- [ ] Enhanced full screen animations
- [ ] Failover monitoring

For more information about getting started and configuring, see the [Wiki](https://github.com/replaysMike/WallMonitor/wiki)
