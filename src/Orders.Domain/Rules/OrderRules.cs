namespace Orders.Domain.Rules;

public static class OrderRules
{
    public const int CustomerNameMinLength = 2;
    public const int CustomerNameMaxLength = 150;
    public const int DescriptionMinLength = 3;
    public const int DescriptionMaxLength = 500;
    public const int TotalAmountMaxDecimals = 2;
    public const decimal TotalAmountMax = 999_999_999.99m;
}
