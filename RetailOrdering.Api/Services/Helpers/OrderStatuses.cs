namespace RetailOrdering.Api.Services.Helpers;

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Preparing = "Preparing";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";

    public static readonly HashSet<string> Allowed =
    [
        Pending,
        Confirmed,
        Preparing,
        Delivered,
        Cancelled
    ];
}

