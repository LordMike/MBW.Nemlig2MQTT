# Nemlig2MQTT
[![docker hub](https://img.shields.io/docker/pulls/lordmike/nemlig2mqtt)](https://hub.docker.com/repository/docker/lordmike/nemlig2mqtt)

![logo](Logo/Logo.png)

This is a proxy application to translate the status of a Blue Riiot pool manager, to Home Assistant using MQTT. You can run this application in docker, and it will periodically poll the Blue Riiot API for updates.

This project uses other libraries of mine, the [MBW.Client.NemligCom](https://github.com/lordMike/mbw.nemlig2mqtt) and [MBW.HassMQTT](https://github.com/LordMike/MBW.HassMQTT) ([nuget](https://www.nuget.org/packages/MBW.HassMQTT)).

# Features

* Can expose the next delivery including the estimated time of delivery when it's known
* Can show the current basket including when it is ready to order
* Can order the current basket using a predefined credit card
* Can locate good delivery times, one per day, that fit some criteria

# Setup

## Environment Variables

| Name                                              | Required | Default         | Note                                                                 |
|---------------------------------------------------|----------|-----------------|----------------------------------------------------------------------|
| MQTT__Server                                      | yes      |                 | A hostname or IP address                                             |
| MQTT__Port                                        |          | 1883            |                                                                      |
| MQTT__Username                                    |          |                 |                                                                      |
| MQTT__Password                                    |          |                 |                                                                      |
| MQTT__ClientId                                    |          | `nemlig2mqtt`   |                                                                      |
| MQTT__ReconnectInterval                           |          | `00:00:30`      | How long to wait before reconnecting to MQTT                         |
| HASS__DiscoveryPrefix                             |          | `homeassistant` | Prefix of HASS discovery topics                                      |
| HASS__TopicPrefix                                 |          | `nemlig`        | Prefix of state and attribute topics                                 |
| HASS__EnableHASSDiscovery                         |          | `true`          | Enable or disable the HASS discovery documents, disable with `false` |
| Nemlig__Username                                  | yes      |                 |                                                                      |
| Nemlig__Password                                  | yes      |                 |                                                                      |
| Nemlig__CheckInterval                             |          | 01:00:00        | Fallback update interval, default: `15 minutes`                      |
| Nemlig__DeliveryConfig__DaysToCheck               |          | `4`             | Number of days to check, range: 1-7                                  |
| Nemlig__DeliveryConfig__PrioritizeMaxHours        |          | `48`            | Max hours to prioritize, range: 4-672                                |
| Nemlig__DeliveryConfig__PrioritizeCheapHours      |          | `true`          | Prioritize cheaper hours, `true` or `false`                          |
| Nemlig__DeliveryConfig__PrioritizeShortTimespan   |          | `false`         | Prioritize shorter timespans, `true` or `false`                      |
| Nemlig__DeliveryConfig__PrioritizeFreeDelivery    |          | `true`          | Prioritize free delivery, `true` or `false`                          |
| Nemlig__DeliveryConfig__PrioritizeHours           |          |                 | Prioritize specific hours, range: 0-23, array of bytes               |
| Nemlig__DeliveryConfig__MaxDeliveryPrice          |          |                 | Maximum delivery price, optional                                     |
| Nemlig__DeliveryConfig__AllowDeliveryTypes        |          |                 | Allowed delivery types, array of `NemligDeliveryType`                |
| Nemlig__DeliveryConfig__NextDeliveryCheckInterval |          | `01:00:00`      | Interval for next delivery check, range: 00:01:00 - 15:00:00         |
| Proxy__Uri                                        |          |                 | Set this to pass Nemlig.com API calls through an HTTP proxy          |


# Docker images

## Run in Docker CLI

> docker run -d -e MQTT__Server=myqueue.local -e Nemlig__Username=myuser -e Nemlig__Password=mypassword lordmike/nemlig2mqtt:latest

## Run in Docker Compose

```yaml
# docker-compose.yml
version: '2.3'

services:
  nemlig2mqtt:
    image: lordmike/nemlig2mqtt:latest
    environment:
      MQTT__Server: myqueue.local
      Nemlig__Username: myuser
      Nemlig__Password: mypassword
```

## Available tags

You can use one of the following tags. Architectures available: `amd64`, `armv7` and `aarch64`

* `latest` (latest, multi-arch)
* `ARCH-latest` (latest, specific architecture)
* `vA.B.C` (specific version, multi-arch)
* `ARCH-vA.B.C` (specific version, specific architecture)

For all available tags, see [Docker Hub](https://hub.docker.com/repository/docker/lordmike/nemlig2mqtt/tags).

# MQTT Commands

It is possible to send certain commands to the Nemlig2MQTT application, using MQTT topics. The following commands can be sent.

## Force sync
**Topic:** (prefix)/commands/force_sync

Sending a message to this topic will force the Nemlig2MQTT app to poll Nemlig.com for new information.

# How

Officially, Nemlig.com does _not_ have any API available. They have a their site and their own app. I found these to be lacking for me, as I want to bring all my data into my domain, such as in my local HASS setup.

The API used here is reverse engineered from the Nemlig.com app and website.

# Troubleshooting

## Log level

Adjust the logging level using this environment variable:

> Logging__MinimumLevel__Default: Error | Warning | Information | Debug | Verbose

## HTTP Requests logging

Since this is a reverse engineering effort, sometimes things go wrong. To aid in troubleshooting, the requests and responses from the Nemlig.com API can be dumped to the console, by enabling trace logging.

Enable request logging with this environment variable:
> Logging__MinimumLevel__Override__nemlig: Verbose
