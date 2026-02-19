using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WolvenKit.App.Helpers;

public class EnumMember<T>
{
    public required string Desc { get; set; }
    public required T Value { get; set; }
}

public static class EnumHelpers
{


    public static IEnumerable<EnumMember<T>> EnumToItemSource<T>() where T: struct, Enum
    {
        return Enum.GetValues<T>()
            .Select(ev => new EnumMember<T>
            {
                Desc = GetDescription(ev),
                Value = ev
            });
    }

    public static string GetDescription<T>(T enumValue) where T : struct, Enum
    {
        var enumName = enumValue.ToString();
        var descAttr = typeof(T).GetField(enumName)?.GetCustomAttribute<DescriptionAttribute>();

        return descAttr?.Description ?? enumName ?? string.Empty;
    }
}
