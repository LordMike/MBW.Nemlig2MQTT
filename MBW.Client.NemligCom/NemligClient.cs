using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom.Builder;
using MBW.Client.NemligCom.Extensions;
using MBW.Client.NemligCom.Helpers;
using MBW.Client.NemligCom.Objects.Login;
using MBW.Client.NemligCom.Objects.Order;

namespace MBW.Client.NemligCom;

public class NemligClient
{
    private readonly IHttpClientProducer _httpClientProducer;
    private readonly IRequestSigner _requestSigner;

    internal NemligClient(IHttpClientProducer httpClientProducer, IRequestSigner requestSigner)
    {
        _httpClientProducer = httpClientProducer;
        _requestSigner = requestSigner;
    }

    public async Task<WebApiLoginResponse> PerformLogin(string username, string password,
        CancellationToken token = default)
    {
        var req = new
        {
            Username = username,
            Password = password,
            DoMerge = true,
            CheckForExistingProducts = true,
            AutoLogin = true
        };

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage res = await httpClient.PostJson("/webapi/login", req, token: token);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Unable to login as {username}, response: {res.StatusCode}");

        return await res.Content.ReadAsJson<WebApiLoginResponse>(token);
    }

    public async Task<BasicOrderHistory> GetBasicOrderHistory(int skip, int take, CancellationToken token = default)
    {
        // https://www.nemlig.com/webapi/order/GetBasicOrderHistory?skip=10&take=10
        await _requestSigner.LoginIfNeeded(this, token);

        QueryStringBuilder builder = QueryStringCache.GetBuilder("/webapi/order/GetBasicOrderHistory");
        builder.Add("skip", skip.ToString());
        builder.Add("take", take.ToString());

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp =
            await httpClient.GetAsync(builder, HttpCompletionOption.ResponseContentRead, token);

        QueryStringCache.Return(builder);

        BasicOrderHistory response = await resp.Content.ReadAsJson<BasicOrderHistory>(token);

        return response;
    }

    public async Task<LatestOrderHistory> GetLatestOrderHistory(CancellationToken token = default)
    {
        // https://www.nemlig.com/webapi/order/GetBasicOrderHistory?skip=10&take=10
        await _requestSigner.LoginIfNeeded(this, token);

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync("/webapi/order/GetLatestOrderHistory",
            HttpCompletionOption.ResponseContentRead, token);

        LatestOrderHistory response = await resp.Content.ReadAsJson<LatestOrderHistory>(token);

        return response;
    }

    public async Task<OrderHistory> GetOrderHistory(int orderId, CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        QueryStringBuilder builder = QueryStringCache.GetBuilder("/webapi/v2/order/GetOrderHistory/" + orderId);

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp =
            await httpClient.GetAsync(builder, HttpCompletionOption.ResponseContentRead, token);

        QueryStringCache.Return(builder);

        OrderHistory response = await resp.Content.ReadAsJson<OrderHistory>(token);

        return response;
    }

    public async Task<DeliverySpot> GetDeliverySpot(CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync("/webapi/Order/DeliverySpot",
            HttpCompletionOption.ResponseContentRead, token);

        DeliverySpot response = await resp.Content.ReadAsJson<DeliverySpot>(token);

        return response;
    }
}