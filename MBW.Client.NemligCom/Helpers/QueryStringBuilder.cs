using System.Text;
using System.Web;

namespace MBW.Client.NemligCom.Helpers;

internal class QueryStringBuilder
{
    private readonly StringBuilder _stringBuilder = new StringBuilder();
    private bool _hasQueryParam;

    public void Init(string resource)
    {
        _stringBuilder.Clear();
        _stringBuilder.Append(resource);
        _hasQueryParam = false;
    }

    public void Add(string key, int? value, bool forceDefault = false)
    {
        if (!value.HasValue && !forceDefault)
            return;

        Add(key, value.ToString(), forceDefault);
    }

    public void Add(string key, string? value, bool forceDefault = false)
    {
        if (string.IsNullOrEmpty(value) && !forceDefault)
            return;

        if (!_hasQueryParam)
            _stringBuilder.Append('?');
        else
            _stringBuilder.Append('&');

        _hasQueryParam = true;
        _stringBuilder.Append(HttpUtility.UrlEncode(key))
            .Append('=');

        if (!string.IsNullOrEmpty(value))
            _stringBuilder.Append(HttpUtility.UrlEncode(value));
    }

    public static implicit operator string(QueryStringBuilder builder)
    {
        return builder.ToString();
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }
}