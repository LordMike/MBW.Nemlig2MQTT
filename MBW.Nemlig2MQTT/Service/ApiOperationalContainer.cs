using System;
using System.Threading;
using System.Threading.Tasks;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.AliveAndWill;
using MBW.HassMQTT.DiscoveryModels.Enum;
using MBW.HassMQTT.DiscoveryModels.Models;
using MBW.HassMQTT.Extensions;
using MBW.HassMQTT.Interfaces;
using MBW.Nemlig2MQTT.HASS;
using Microsoft.Extensions.Hosting;

namespace MBW.Nemlig2MQTT.Service;

internal class ApiOperationalContainer : BackgroundService
{
    private readonly HassMqttManager _hassMqttManager;

    public const string OkMessage = "ok";
    public const string ProblemMessage = "problem";

    private ISensorContainer _apiOperational;

    public ApiOperationalContainer(HassMqttManager hassMqttManager)
    {
        _hassMqttManager = hassMqttManager;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _apiOperational = CreateSystemEntities();

        return Task.CompletedTask;
    }

    public void MarkOk()
    {
        if (_apiOperational == null)
            return;

        _apiOperational.SetValue(HassTopicKind.State, OkMessage);
        _apiOperational.SetAttribute("last_ok", DateTime.UtcNow.ToString("O"));
    }

    public void MarkError(string errorMessage)
    {
        if (_apiOperational == null)
            return;

        _apiOperational.SetValue(HassTopicKind.State, ProblemMessage);
        _apiOperational.SetAttribute("last_bad", DateTime.UtcNow.ToString("O"));
        _apiOperational.SetAttribute("last_bad_status", errorMessage);
    }

    private ISensorContainer CreateSystemEntities()
    {
        _hassMqttManager.ConfigureSensor<MqttBinarySensor>(HassUniqueIdBuilder.GetSystemDeviceId(), "api_operational")
            .ConfigureTopics(HassTopicKind.State, HassTopicKind.JsonAttributes)
            .ConfigureDevice(device =>
            {
                device.Name = "Nemlig2MQTT";
                device.Identifiers.Add(HassUniqueIdBuilder.GetSystemDeviceId());
                device.SwVersion = typeof(Program).Assembly.GetName().Version.ToString(3);
            })
            .ConfigureDiscovery(discovery =>
            {
                discovery.Name = "Nemlig2MQTT API Operational";
                discovery.DeviceClass = HassBinarySensorDeviceClass.Problem;

                discovery.PayloadOn = ProblemMessage;
                discovery.PayloadOff = OkMessage;
            })
            .ConfigureAliveService();

        return _hassMqttManager.GetSensor(HassUniqueIdBuilder.GetSystemDeviceId(), "api_operational");
    }

}