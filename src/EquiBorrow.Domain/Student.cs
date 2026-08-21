using System;
using System.Collections.Generic;
using System.Text;

namespace EquiBorrow.Domain;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }

    public Student(int id, string name, bool isActive = true)
    {
        Id = id;
        Name = name;
        IsActive = isActive;
    }
}
