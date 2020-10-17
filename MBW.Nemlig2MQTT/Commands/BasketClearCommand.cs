using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.Commands;
using MBW.Nemlig2MQTT.Service;
using MQTTnet;

namespace MBW.Nemlig2MQTT.Commands
{
    internal class BasketClearCommand : IMqttCommandHandler
    {
        private readonly HassMqttManager _hassMqttManager;
        private readonly NemligBasketMqttService _service;
        private readonly NemligClient _client;

        public BasketClearCommand(HassMqttManager hassMqttManager, NemligBasketMqttService service, NemligClient client)
        {
            _hassMqttManager = hassMqttManager;
            _service = service;
            _client = client;
        }

        public string[] GetFilter()
        {
            return new[] { "basket", "clear" };
        }

        public async Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = new CancellationToken())
        {
            var basket = await _client.GetBasket(token);

            foreach (var line in basket.Lines)
                await _client.AddToBasket(line.Id, 0, token: token);

            basket = await _client.GetBasket(token);

            _service.Update(basket);

            await _hassMqttManager.FlushAll(token);
        }
    }
}