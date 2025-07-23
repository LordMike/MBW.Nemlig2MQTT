# Repository Notes

This project is a .NET solution that bridges the Danish grocery store Nemlig.com to Home Assistant using MQTT. The solution file `MBW.Nemlig2MQTT.sln` contains two projects:

* **MBW.Client.NemligCom** – a client library that implements the (reverse engineered) Nemlig.com HTTP API.
* **MBW.Nemlig2MQTT** – the main service that periodically polls Nemlig.com, transforms the responses and publishes them as Home Assistant MQTT entities.

There are currently **no unit tests** in this repository. Validation is usually done by simply running `dotnet build` or executing the application.

## Scraper based architecture

The main service collects data using several **scrapers** located in `MBW.Nemlig2MQTT/Service/Scrapers`. Each class implements `IResponseScraper` and focuses on a single aspect of the Nemlig API (basket contents, next delivery, delivery options, credit card list, order history, etc.). `ScraperManager` invokes all registered scrapers for each received response.

MQTT topics and discovery documents are built using the helper library **MBW.HassMQTT**. `NemligMqttService` schedules scrapes according to the configuration and pushes updates via `HassMqttManager`. Sensors and other entities are emitted so that Home Assistant can automatically discover them.

### Commands

The service also listens for MQTT commands (located in the `Commands` folder) such as forcing a sync, ordering the current basket or setting the desired delivery time.

