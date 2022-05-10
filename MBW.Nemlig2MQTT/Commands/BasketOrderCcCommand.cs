using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom;
using MBW.HassMQTT.CommonServices.Commands;
using MBW.Nemlig2MQTT.Configuration;
using MBW.Nemlig2MQTT.Service;
using Microsoft.Extensions.Options;
using MQTTnet;
using Newtonsoft.Json;

namespace MBW.Nemlig2MQTT.Commands
{
    internal class BasketOrderCcCommand : IMqttCommandHandler
    {
        private readonly NemligBasketMqttService _service;
        private readonly string _nemligPassword;
        private readonly NemligClient _nemligClient;

        public BasketOrderCcCommand(NemligBasketMqttService service, NemligClient nemligClient, IOptions<NemligConfiguration> configuration)
        {
            _service = service;
            _nemligPassword = configuration.Value.Password;
            _nemligClient = nemligClient;
        }

        public string[] GetFilter()
        {
            return new[] { "basket", "order-cc", null };
        }

        public async Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = new CancellationToken())
        {
            int ccId = int.Parse(topicLevels[2]);
            string request = message.ConvertPayloadToString();

            RequestDetails requestDetails;
            if (!string.IsNullOrEmpty(request) && request != "PRESS")
                requestDetails = JsonConvert.DeserializeObject<RequestDetails>(request);
            else
                requestDetails = new RequestDetails();

            requestDetails.PlacementMessage ??= "Frontdoor";

            await _nemligClient.OrderBasketWithCreditCard(ccId, _nemligPassword, requestDetails.PlacementMessage, token);

            _service.ForceSync();
        }

        class RequestDetails
        {
            public string PlacementMessage { get; set; }
        }
    }
}