using EquiBorrow.Domain;

namespace EquiBorrow.UI.ViewModels;

public sealed class CheckoutStudentItemViewModel
{
    public CheckoutStudentItemViewModel(Student student)
    {
        Id = student.Id;
        Number = student.StudentNumber;
        Name = student.Name;
        IsActive = student.IsActive;
    }

    public int Id { get; }
    public string Number { get; }
    public string Name { get; }
    public bool IsActive { get; }
    public string DisplayText => IsActive ? $"{Number} {Name}" : $"{Number} {Name} (Restricted)";
}
