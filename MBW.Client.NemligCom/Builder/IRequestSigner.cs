using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MBW.Client.NemligCom.Builder;

internal interface IRequestSigner
{
    Task LoginIfNeeded(NemligClient client, CancellationToken token = default);

    Task Sign(HttpClient httpClient, HttpRequestMessage request, CancellationToken token = default);
}