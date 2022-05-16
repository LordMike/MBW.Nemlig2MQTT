using System;

namespace MBW.Client.NemligCom.Objects.Search;

[Flags]
public enum SearchFilters
{
    None = 0,

    /// <summary>
    /// Favorit
    /// </summary>
    Favorite = 1,

    /// <summary>
    /// Økologisk
    /// </summary>
    Ecological = 2,

    /// <summary>
    /// Tilbud
    /// </summary>
    Offer = 4,

    /// <summary>
    /// Discount / prismatch
    /// </summary>
    Discount = 8
}