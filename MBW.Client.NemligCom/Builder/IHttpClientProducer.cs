using System.Net.Http;

namespace MBW.Client.NemligCom.Builder;

internal interface IHttpClientProducer
{
    HttpClient CreateClient();
}