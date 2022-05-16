using System;
using System.Net.Http;

namespace MBW.Client.NemligCom.Builder;

internal class HttpClientFactoryProducer : IHttpClientProducer
{
    private readonly IHttpClientFactory _factory;
    private readonly string _clientName;

    public HttpClientFactoryProducer(IHttpClientFactory factory, string clientName)
    {
        _factory = factory;
        _clientName = clientName;
    }

    public HttpClient CreateClient()
    {
        HttpClient httpClient = _factory.CreateClient(_clientName);
            
        httpClient.DefaultRequestHeaders.Add("User-Agent", "nemlig.com/8843 CFNetwork/1197 Darwin/20.0.0");
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.BaseAddress = new Uri("https://www.nemlig.com/");

        return httpClient;
    }
}