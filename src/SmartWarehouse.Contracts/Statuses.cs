namespace SmartWarehouse.Contracts;

public static class OrderStatuses
{
    public const string Assigned = "Assigned";
    public const string InProgress = "InProgress";
    public const string AwaitingHandoff = "AwaitingHandoff";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

public static class LineStatuses
{
    public const string Pending = "Pending";
    public const string Picked = "Picked";
}

public static class InboundStatuses
{
    public const string Received = "Received";
    public const string ShelfAssigned = "ShelfAssigned";
    public const string Placed = "Placed";
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Worker = "Worker";
}
