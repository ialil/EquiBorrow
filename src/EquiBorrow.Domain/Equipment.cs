namespace EquiBorrow.Domain;

public class Equipment
{
    public int Id { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string IdentifierKey { get; private set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsAvailable { get; set; } = true;

    public Equipment() { }

    public Equipment(int id, string name, bool isAvailable = true, string identifier = "")
    {
        Id = id;
        Name = name;
        IsAvailable = isAvailable;
        Identifier = identifier;
    }
}