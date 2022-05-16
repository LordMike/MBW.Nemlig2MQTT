namespace MBW.Client.NemligCom.Helpers;

internal static class QueryStringCache
{
    public static QueryStringBuilder GetBuilder(string resource)
    {
        QueryStringBuilder? builder = new QueryStringBuilder();
        builder.Init(resource);

        return builder;
    }

    public static void Return(QueryStringBuilder builder)
    {
    }
}