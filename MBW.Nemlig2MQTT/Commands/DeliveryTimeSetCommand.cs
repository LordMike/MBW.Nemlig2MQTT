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

        public DeliveryTimeSetCommand(NemligDeliveryOptionsMqttService service)
        {
            _service = service;
        }

        public string[] GetFilter()
        {
            return new[] { "basket", "delivery_select", "command" };
        }

        public Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = new CancellationToken())
        {
            string value = message.ConvertPayloadToString();

            return _service.SetValue(value);
        }
    }
}