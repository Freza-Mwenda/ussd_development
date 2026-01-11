namespace UssdDevelopmentCore.Entities;

public class BaseEntityResponse
{
    public required int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }   
}