using System.Windows.Input;

namespace Soundboard.Models;

public class HotkeyBinding
{
    public Key Key { get; set; }
    public ModifierKeys Modifiers { get; set; }

    public HotkeyBinding()
    {
    }

    public HotkeyBinding(Key key, ModifierKeys modifiers = ModifierKeys.None)
    {
        Key = key;
        Modifiers = modifiers;
    }

    public string GetDisplayString()
    {
        var parts = new List<string>();

        if (Modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Windows))
            parts.Add("Win");

        parts.Add(Key.ToString());

        return string.Join("+", parts);
    }

    public override bool Equals(object? obj)
    {
        if (obj is HotkeyBinding other)
        {
            return Key == other.Key && Modifiers == other.Modifiers;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Modifiers);
    }
}
