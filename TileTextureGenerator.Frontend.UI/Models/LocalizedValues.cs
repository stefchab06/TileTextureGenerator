using System;
using System.Collections.Generic;
using System.Text;

namespace TileTextureGenerator.Frontend.UI.Models;

public sealed class LocalizedValue<T>
{
    public T Value { get; }
    public string Localized { get; }

    public LocalizedValue(T value, string localized)
    {
        Value = value;
        Localized = localized;
    }

    public override string ToString() => Localized;
}