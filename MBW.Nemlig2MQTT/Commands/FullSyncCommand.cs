using System.Threading;
using System.Threading.Tasks;
using MBW.HassMQTT.CommonServices.Commands;
using MBW.Nemlig2MQTT.HASS;
using MBW.Nemlig2MQTT.Service;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace MBW.Nemlig2MQTT.Commands;

internal class FullSyncCommand : IMqttCommandHandler
{
    private readonly ILogger<FullSyncCommand> _logger;
    private readonly NemligMqttService _mqttService;

    public FullSyncCommand(ILogger<FullSyncCommand> logger, NemligMqttService mqttService)
    {
        _logger = logger;
        _mqttService = mqttService;
    }

    public string[] GetFilter()
    {
        return new[] { HassUniqueIdBuilder.GetSystemDeviceId(), "force_sync", "command" };
    }

    public Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = default)
    {
        _logger.LogInformation("Force syncing all");

        _mqttService.ForceSync();
        return Task.CompletedTask;
    }
}