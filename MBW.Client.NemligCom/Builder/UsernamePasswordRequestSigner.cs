using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MBW.Client.NemligCom.Objects.Login;

namespace MBW.Client.NemligCom.Builder;

internal class UsernamePasswordRequestSigner : IRequestSigner
{
    private readonly string _username;
    private readonly string _password;
    private WebApiLoginResponse? _credentials;

    public UsernamePasswordRequestSigner(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public async Task LoginIfNeeded(NemligClient client, CancellationToken token = default)
    {
        if (_credentials != null)
            return;

        // Re-login
        _credentials = await client.PerformLogin(_username, _password, token);
    }

    public Task Sign(HttpClient httpClient, HttpRequestMessage request, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }
}