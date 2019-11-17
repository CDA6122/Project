/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
using Project.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Project.Extensions
{
    internal static class EnumExtensions
    {
        internal static string GetDescription<TEnum>(this TEnum value)
            where TEnum : Enum
        {
            string? enumName = Enum.GetName(typeof(TEnum), value);
            if (enumName == null)
            {
                return "";
            }

            var enumField = typeof(FieldError).GetField(enumName, BindingFlags.Static | BindingFlags.Public);
            var descriptionAttributes = enumField?.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (descriptionAttributes != null)
            {
                foreach (DescriptionAttribute enumDescription in descriptionAttributes)
                {
                    return enumDescription.Description;
                }
            }
            return enumName;
        }
    }
}
