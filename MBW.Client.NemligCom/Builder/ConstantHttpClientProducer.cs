using System;
using System.Net.Http;

namespace MBW.Client.NemligCom.Builder;

internal class ConstantHttpClientProducer : IHttpClientProducer
{
    private readonly HttpClient _httpClient;

    public ConstantHttpClientProducer(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "nemlig.com/8843 CFNetwork/1197 Darwin/20.0.0");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.BaseAddress = new Uri("https://www.nemlig.com/");
    }

    public HttpClient CreateClient()
    {
        return _httpClient;
    }
}