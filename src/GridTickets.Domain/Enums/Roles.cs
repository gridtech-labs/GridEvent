namespace GridTickets.Domain.Enums;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Organizer = "Organizer";
    public const string Customer = "Customer";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Organizer, Customer };
}
