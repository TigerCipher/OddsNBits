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

using System.ComponentModel.DataAnnotations;

namespace OddsNBits.Models;

public class BlogPostModel
{
    [Required, MaxLength(120)]
    public string Title { get; set; }

    // TODO might want slugs here in case a custom slug is desired

    [Required, MaxLength(500)]
    public string Introduction { get; set; }

    [Required]
    public string Content { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category")]
    public short CategoryId { get; set; }

    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
}