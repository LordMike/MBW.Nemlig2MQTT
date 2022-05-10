using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom;
using MBW.Client.NemligCom.Objects.Basket;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.Commands;
using MBW.Nemlig2MQTT.Service;
using MQTTnet;
using Newtonsoft.Json;

namespace MBW.Nemlig2MQTT.Commands;

internal class BasketAddCommand : IMqttCommandHandler
{
    private readonly HassMqttManager _hassMqttManager;
    private readonly NemligBasketMqttService _service;
    private readonly NemligClient _client;

    public BasketAddCommand(HassMqttManager hassMqttManager, NemligBasketMqttService service, NemligClient client)
    {
        _hassMqttManager = hassMqttManager;
        _service = service;
        _client = client;
    }

    public string[] GetFilter()
    {
        return new[] { "basket", "add" };
    }

    public async Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = new CancellationToken())
    {
        Request request = JsonConvert.DeserializeObject<Request>(message.ConvertPayloadToString());
        NemligBasket basket = await _client.AddToBasket(request.Id, request.Quantity, token: token);

        _service.Update(basket);

        await _hassMqttManager.FlushAll(token);
    }

    class Request
    {
        public int Id { get; set; }

        public int Quantity { get; set; } = 1;
    }
}