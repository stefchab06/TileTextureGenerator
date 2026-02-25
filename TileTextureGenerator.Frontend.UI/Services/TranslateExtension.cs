using System;
using System.Collections.Generic;
using System.Text;

namespace TileTextureGenerator.Frontend.UI.Services;

[ContentProperty(nameof(Text))]
public class TranslateExtension : IMarkupExtension
{
    public string Text { get; set; } = string.Empty;

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Text)) return string.Empty;
        try
        {
            return LocalizationService.Instance.GetString(Text);
        }
        catch
        {
            return Text;
        }
    }
}