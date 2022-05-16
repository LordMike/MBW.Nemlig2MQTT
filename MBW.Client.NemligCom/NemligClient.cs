using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using MBW.Client.NemligCom.Builder;
using MBW.Client.NemligCom.Extensions;
using MBW.Client.NemligCom.Helpers;
using MBW.Client.NemligCom.Objects.Basket;
using MBW.Client.NemligCom.Objects.Checkout;
using MBW.Client.NemligCom.Objects.Delivery;
using MBW.Client.NemligCom.Objects.General;
using MBW.Client.NemligCom.Objects.Login;
using MBW.Client.NemligCom.Objects.Menu;
using MBW.Client.NemligCom.Objects.Order;
using MBW.Client.NemligCom.Objects.Search;
using MBW.Client.NemligCom.Objects.Settings;
using Microsoft.Extensions.Logging;

namespace MBW.Client.NemligCom;

public class NemligClient
{
    private readonly ILogger<NemligClient> _logger;
    private readonly IHttpClientProducer _httpClientProducer;
    private readonly IRequestSigner _requestSigner;
    private NemligSiteSettings? _siteSettings;
    private Uri? _nemligBaseUrl;

    internal NemligClient(ILogger<NemligClient> logger, IHttpClientProducer httpClientProducer, IRequestSigner requestSigner)
    {
        _logger = logger;
        _httpClientProducer = httpClientProducer;
        _requestSigner = requestSigner;
    }

    public Uri NemligUrl
    {
        get
        {
            if (_nemligBaseUrl is null)
                _nemligBaseUrl = _httpClientProducer.CreateClient().BaseAddress;

            return _nemligBaseUrl;
        }
    }

    public async Task<WebApiLoginResponse> PerformLogin(string username, string password, CancellationToken token = default)
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

        return await res.Content.ReadAsJson<WebApiLoginResponse>(token);
    }

    public async Task<NemligSiteSettings> GetSiteSettings(bool forceRefresh = false, CancellationToken token = default)
    {
        if (_siteSettings != null && !forceRefresh)
            return _siteSettings;

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync("/webapi/AppSettings/Website", HttpCompletionOption.ResponseContentRead, token);

        _siteSettings = await resp.Content.ReadAsJson<NemligSiteSettings>(token);

        return _siteSettings;
    }

    public async Task<NemligAntiForgeryResponse> GetAntiForgery(CancellationToken token = default)
    {
        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync("/webapi/AntiForgery", HttpCompletionOption.ResponseContentRead, token);

        NemligAntiForgeryResponse basket = await resp.Content.ReadAsJson<NemligAntiForgeryResponse>(token);

        return basket;
    }

    public async Task<NemligBasket> GetBasket(CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync("/webapi/basket/GetBasket", HttpCompletionOption.ResponseContentRead, token);

        NemligBasket basket = await resp.Content.ReadAsJson<NemligBasket>(token);

        return basket;
    }

    public async Task<NemligSearchResponse> Search(string query, Action<SearchParameters>? configure = null, CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        SearchParameters searchParams = new SearchParameters
        {
            Query = query
        };

        configure?.Invoke(searchParams);

        NemligSiteSettings settings = await GetSiteSettings(token: token);

        QueryStringBuilder builder = QueryStringCache.GetBuilder($"/webapi/{settings.CombinedProductsAndSitecoreTimestamp}/{settings.TimeslotUtc}/{settings.DeliveryZoneId}/{settings.UserId}/Search/Search");

        builder.Add("query", searchParams.Query);
        builder.Add("take", searchParams.Take);
        builder.Add("skip", searchParams.Skip);

        if (searchParams.SortOrder != SortOrder.None)
            builder.Add("sortorder", searchParams.SortOrder.AsString(EnumFormat.EnumMemberValue));

        foreach (SearchFilters flag in searchParams.Filters.GetFlags())
            builder.Add(flag.AsString(EnumFormat.EnumMemberValue)!, "1");

        string resource = builder;
        QueryStringCache.Return(builder);

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync(resource, HttpCompletionOption.ResponseContentRead, token);

        NemligSearchResponse searchResponse = await resp.Content.ReadAsJson<NemligSearchResponse>(token);

        return searchResponse;
    }

    public async Task<NemligBasket> AddToBasket(int productId, int quantity = 1, bool addToExisting = false, CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        var req = new
        {
            productId,
            quantity,
            addToExisting
        };

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.PostJson("/webapi/basket/AddToBasket", req, token: token);

        NemligBasket basket = await resp.Content.ReadAsJson<NemligBasket>(token);

        return basket;
    }

    public async Task<NemligCheckPostalCodeResponse> CheckPostCode(int postalCode, CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.PostJson("/webapi/Delivery/CheckPostCode", postalCode, token: token);

        NemligCheckPostalCodeResponse checkResponse = await resp.Content.ReadAsJson<NemligCheckPostalCodeResponse>(token);

        return checkResponse;
    }

    public async Task<IList<string>> GetStreetNames(int postalCode, CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        QueryStringBuilder builder = QueryStringCache.GetBuilder("/webapi/Delivery/GetStreetNames");

        builder.Add("postalcode", postalCode.ToString());

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync(builder, HttpCompletionOption.ResponseContentRead, token);

        List<string> streetNames = await resp.Content.ReadAsJson<List<string>>(token);

        QueryStringCache.Return(builder);

        return streetNames;
    }

    public async Task<NemligDeliveryDaysResponse> GetDeliveryDays(int days, CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        QueryStringBuilder builder = QueryStringCache.GetBuilder("/webapi/v2/Delivery/GetDeliveryDays");

        builder.Add("days", days.ToString());

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync(builder, HttpCompletionOption.ResponseContentRead, token);

        NemligDeliveryDaysResponse deliveryDays = await resp.Content.ReadAsJson<NemligDeliveryDaysResponse>(token);

        QueryStringCache.Return(builder);

        return deliveryDays;
    }

    public async Task<TryUpdateDeliveryTimeResponse> TryUpdateDeliveryTime(int timeSlotId, CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        QueryStringBuilder builder = QueryStringCache.GetBuilder("/webapi/Delivery/TryUpdateDeliveryTime");

        builder.Add("timeslotId", timeSlotId.ToString());

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync(builder, HttpCompletionOption.ResponseContentRead, token);

        TryUpdateDeliveryTimeResponse updateResponse = await resp.Content.ReadAsJson<TryUpdateDeliveryTimeResponse>(token);

        QueryStringCache.Return(builder);

        return updateResponse;
    }

    public async Task<NemligMenuNode[]> GetProductsMenu(int navigationDepth = 15, CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        NemligSiteSettings settings = await GetSiteSettings(token: token);

        QueryStringBuilder builder = QueryStringCache.GetBuilder($"/webapi/{settings.CombinedProductsAndSitecoreTimestamp}/{settings.TimeslotUtc}/{settings.DeliveryZoneId}/{settings.UserId}/Menu/main");

        builder.Add("navigationDepth", navigationDepth);
        builder.Add("platform", "App Menu");

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync(builder, HttpCompletionOption.ResponseContentRead, token);

        NemligMenuNode[] menuNodes = await resp.Content.ReadAsJson<NemligMenuNode[]>(token);

        QueryStringCache.Return(builder);

        return menuNodes;
    }

    public async Task<NemligCreditCard[]> GetCreditCards(bool isRecurringCard = false, CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        QueryStringBuilder builder = QueryStringCache.GetBuilder("/webapi/Checkout/GetCreditCards");
        builder.Add("isRecurringCard", isRecurringCard.ToString().ToLowerInvariant());

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync(builder, HttpCompletionOption.ResponseContentRead, token);

        NemligCreditCard[] response = await resp.Content.ReadAsJson<NemligCreditCard[]>(token);

        QueryStringCache.Return(builder);

        return response;
    }

    public async Task OrderBasketWithCreditCard(int creditCardId, string userPassword, string placementLocation = "Frontdoor", CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        NemligOrderLoggedInRequest request = new NemligOrderLoggedInRequest
        {
            Password = userPassword,
            PaymentCard = creditCardId,
            TermsAndConditionsAccepted = true,
            PlacementMessage = placementLocation,
            EmailSubscriptions = new Emailsubscriptions
            {
                SmsNotificationsAllowed = true
            },
            DoorCode = "",
            UnattendedNotes = "",
            AcceptMinimumAge = false,
        };

        QueryStringBuilder builder = QueryStringCache.GetBuilder("/webapi/Order/PlaceOrderLoggedIn");

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.PostJson(builder, request, HttpCompletionOption.ResponseContentRead, token);

        string respString = await resp.Content.ReadAsStringAsync();

        QueryStringCache.Return(builder);

        // Complete payment
        await RegisterNewPaymentTransaction(false);
    }

    public async Task RegisterNewPaymentTransaction(bool useMobilePay, CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        QueryStringBuilder builder = QueryStringCache.GetBuilder("/webapi/Checkout/RegisterNewPaymentTransaction");
        builder.Add("useMobilePay", useMobilePay.ToString().ToLowerInvariant());

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync(builder, HttpCompletionOption.ResponseContentRead, token);

        QueryStringCache.Return(builder);
    }

    public async Task<BasicOrderHistory> GetBasicOrderHistory(int skip, int take, CancellationToken token = default)
    {
        // https://www.nemlig.com/webapi/order/GetBasicOrderHistory?skip=10&take=10
        await _requestSigner.LoginIfNeeded(this, token);

        QueryStringBuilder builder = QueryStringCache.GetBuilder("/webapi/order/GetBasicOrderHistory");
        builder.Add("skip", skip.ToString());
        builder.Add("take", take.ToString());

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync(builder, HttpCompletionOption.ResponseContentRead, token);

        QueryStringCache.Return(builder);

        BasicOrderHistory response = await resp.Content.ReadAsJson<BasicOrderHistory>(token);

        return response;
    }

    public async Task<OrderHistory> GetOrderHistory(int orderId, CancellationToken token = default)
    {
        await _requestSigner.LoginIfNeeded(this, token);

        QueryStringBuilder builder = QueryStringCache.GetBuilder("/webapi/v2/order/GetOrderHistory/" + orderId);

        using HttpClient httpClient = _httpClientProducer.CreateClient();
        using HttpResponseMessage resp = await httpClient.GetAsync(builder, HttpCompletionOption.ResponseContentRead, token);

        QueryStringCache.Return(builder);

        OrderHistory response = await resp.Content.ReadAsJson<OrderHistory>(token);

        return response;
    }
}