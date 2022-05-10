using System.Threading;
using System.Threading.Tasks;
using MBW.HassMQTT.CommonServices.Commands;
using MBW.Nemlig2MQTT.Service;
using MQTTnet;

namespace MBW.Nemlig2MQTT.Commands
{
    internal class DeliveryTimeSetCommand : IMqttCommandHandler
    {
        private readonly NemligDeliveryOptionsMqttService _service;
        private readonly NemligBasketMqttService _basket;

        public DeliveryTimeSetCommand(NemligDeliveryOptionsMqttService service, NemligBasketMqttService basket)
        {
            _service = service;
            _basket = basket;
        }

        public string[] GetFilter()
        {
            return new[] { "basket", "delivery_select", "command" };
        }

        public async Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = new CancellationToken())
        {
            string value = message.ConvertPayloadToString();

            await _service.SetValue(value);

            _basket.ForceSync();
        }
    }
}