using System;
using System.Collections.Generic;
using System.Text;

namespace EquiBorrow.Domain;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Parameterless constructor required by EF Core
    public Student() { }

    public Student(int id, string name, bool isActive = true)
    {
        Id = id;
        Name = name;
        IsActive = isActive;
    }
}
