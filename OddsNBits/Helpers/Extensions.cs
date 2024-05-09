// -------------------------------------------------------------------------------
//     OddsNBits - A Blazor Web App serving as my dev log / blog site
//     Copyright (C) 2024  Matt Rogers
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// -------------------------------------------------------------------------------

using System.Security.Claims;
using System.Text.RegularExpressions;

namespace OddsNBits.Helpers;

public static class Extensions
{
    public static string DisplayName(this ClaimsPrincipal principal) => principal.FindFirstValue(Globals.ClaimNames.DisplayName)!;
    public static string UserId(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public static string ProfileImage(this ClaimsPrincipal principal) => principal.FindFirstValue(Globals.ClaimNames.Image)!;

    public static string Slugify(this string name) =>
        Regex.Replace(name.ToLower(), @"[^a-z0-9_]+", "-", RegexOptions.Compiled, TimeSpan.FromSeconds(1)).Replace("--", "-").Trim('-');

    public static string ShortDisplay(this DateTime? dateTime) => dateTime?.ToString("MMM dd") ?? string.Empty;
}