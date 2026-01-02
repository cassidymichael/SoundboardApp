namespace Soundboard.Models;

public class AudioDevice
{
    public string Id { get; }
    public string Name { get; }
    public bool IsDefault { get; }

    public AudioDevice(string id, string name, bool isDefault = false)
    {
        Id = id;
        Name = name;
        IsDefault = isDefault;
    }

    public static AudioDevice Default => new("default", "System Default", true);
    public static AudioDevice None => new("none", "None", false);

    public override string ToString() => Name;

    public override bool Equals(object? obj)
    {
        if (obj is AudioDevice other)
        {
            return Id == other.Id;
        }
        return false;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
