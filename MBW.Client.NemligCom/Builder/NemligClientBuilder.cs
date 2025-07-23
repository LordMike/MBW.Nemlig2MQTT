using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace MBW.Client.NemligCom.Builder;

public class NemligClientBuilder
{
    private IHttpClientProducer _clientProducer;
    private IRequestSigner _requestSigner;

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
        return this;
    }

    public NemligClient Build()
    {
        return new NemligClient(_clientProducer, _requestSigner);
    }
}