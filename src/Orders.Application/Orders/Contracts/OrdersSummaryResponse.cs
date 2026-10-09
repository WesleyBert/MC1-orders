namespace Orders.Application.Orders.Contracts;

public sealed record OrdersSummaryResponse(int Total, int Open, int Paid, int Cancelled);
