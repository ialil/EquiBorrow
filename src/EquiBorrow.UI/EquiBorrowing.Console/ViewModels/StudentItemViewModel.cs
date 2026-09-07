using ReactiveUI;

namespace EquiBorrow.UI.ViewModels;

public class StudentItemViewModel : ReactiveObject
{
    public int Id { get; }
    public string Name { get; }
    public bool IsActive { get; }

    public StudentItemViewModel(int id, string name, bool isActive)
    {
        Id = id;
        Name = name;
        IsActive = isActive;
    }
}
