using System.Threading;
using System.Threading.Tasks;
using MBW.HassMQTT.CommonServices.Commands;
using MBW.Nemlig2MQTT.Service;
using MQTTnet;

namespace MBW.Nemlig2MQTT.Commands
{
    internal class BasketSyncCommand : IMqttCommandHandler
    {
        private readonly NemligBasketMqttService _service;

        public BasketSyncCommand(NemligBasketMqttService service)
        {
            _service = service;
        }

        public string[] GetFilter()
        {
            return new[] { "basket", "sync" };
        }

        public Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = new CancellationToken())
        {
            _service.ForceSync();

            return Task.CompletedTask;
        }
    }
}