﻿namespace MBW.Client.NemligCom.Objects.Search;

public class Availability1
{
    public bool IsDeliveryAvailable { get; set; }
    public bool IsAvailableInStock { get; set; }
    public object[] ReasonMessageKeys { get; set; }
}