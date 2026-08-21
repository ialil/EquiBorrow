using System;
using System.Collections.Generic;
using System.Text;

namespace EquiBorrow.Domain;

public class Equipment
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsAvailable { get; set; }

    public Equipment(int id, string name, bool isAvailable = true)
    {
        Id = id;
        Name = name;
        IsAvailable = isAvailable;
    }
}
