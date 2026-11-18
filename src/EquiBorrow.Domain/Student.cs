namespace EquiBorrow.Domain;

public class Student
{
    public int Id { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    // Database-generated normalized key (lower(btrim(StudentNumber))); carries the unique index.
    public string StudentNumberKey { get; private set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public bool IsActive { get; set; } = true;
    public uint Version { get; set; }

    public Student() { }

    public Student(int id, string name, bool isActive = true, string studentNumber = "")
    {
        Id = id;
        Name = name;
        IsActive = isActive;
        StudentNumber = studentNumber;
    }
}