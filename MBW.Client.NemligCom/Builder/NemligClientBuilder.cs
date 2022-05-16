using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MBW.Client.NemligCom.Builder;

public class NemligClientBuilder
{
    private IHttpClientProducer _clientProducer;
    private IRequestSigner _requestSigner;
    private ILogger<NemligClient> _logger = NullLogger<NemligClient>.Instance;

    public NemligClientBuilder()
    {
        _clientProducer = new ConstantHttpClientProducer(new HttpClient());
    }

    public NemligClientBuilder UseHttpClientFactory(IHttpClientFactory factory, string clientName)
    {
        _clientProducer = new HttpClientFactoryProducer(factory, clientName);
        return this;
    }

    public NemligClientBuilder UseHttpClient(HttpClient client)
    {
        _clientProducer = new ConstantHttpClientProducer(client);
        return this;
    }

    public NemligClientBuilder UseUsernamePassword(string username, string password)
    {
        _requestSigner = new UsernamePasswordRequestSigner(username, password);
        return this;
    }

    public NemligClientBuilder UseLogger(ILogger<NemligClient> logger)
    {
        _logger = logger;
        return this;
    }

    public NemligClient Build()
    {
        return new NemligClient(_logger, _clientProducer, _requestSigner);
    }
}