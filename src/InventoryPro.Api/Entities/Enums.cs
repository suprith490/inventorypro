namespace InventoryPro.Api.Entities;

public enum UserRole
{
    Admin = 1,
    Staff = 2
}

public enum TransactionType
{
    StockIn = 1,
    StockOut = 2,
    Adjustment = 3
}

public enum ReferenceType
{
    Purchase = 1,
    Sale = 2,
    ManualAdjustment = 3
}

public enum PurchaseStatus
{
    Pending = 0,
    Completed = 1,
    Cancelled = 2
}

public enum SaleStatus
{
    Completed = 1,
    Cancelled = 2
}
