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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace OddsNBits.Controllers;

public class FileUploadController : ControllerBase
{
    private const int BoundaryLengthLimit = 512 * 1024;

    [HttpPost("/file-upload-streamed/")]
    public async Task<IActionResult> UploadStreamedFileAsync(CancellationToken cancellationToken)
    {
        if (Request.ContentType != null && !IsMultipartContentType(Request.ContentType))
        {
            ModelState.AddModelError("File", $"The request couldn't be processed (Error 1).");
            return BadRequest(ModelState);
        }

        var boundary = GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType), lengthLimit: BoundaryLengthLimit);
        var reader = new MultipartReader(boundary, HttpContext.Request.Body);
        var section = await reader.ReadNextSectionAsync(cancellationToken);

        while (section != null)
        {
            var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

            if (hasContentDispositionHeader)
            {
                // THIS IS WHERE YOU PASS THE FILE STREAM TO THE FACADE, the code below is not to be directly used here!

                // Don't trust the file name sent by the client. To display the file name, HTML-encode the value.
                var trustedFileNameForDisplay = WebUtility.HtmlEncode(contentDisposition?.FileName.Value);
                var trustedFileNameForFileStorage = Path.GetRandomFileName();
                using (var targetStream = System.IO.File.Create(Path.Combine(Path.GetTempPath(), trustedFileNameForFileStorage)))
                {
                	await section.Body.CopyToAsync(targetStream, cancellationToken);
                }

                // await section.Body.CopyToAsync(Stream.Null, cancellationToken);

                return Ok(trustedFileNameForFileStorage);
            }

            // Drain any remaining section body that hasn't been consumed and read the headers for the next section.
            section = await reader.ReadNextSectionAsync(cancellationToken);
        }

        return BadRequest();
    }

    // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
    // The spec at https://tools.ietf.org/html/rfc2046#section-5.1 states that 70 characters is a reasonable limit.
    public static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
    {
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidDataException("Missing content-type boundary.");
        }

        if (boundary.Length > lengthLimit)
        {
            throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");
        }

        return boundary;
    }

    public static bool IsMultipartContentType(string contentType)
    {
        return !string.IsNullOrEmpty(contentType)
               && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool HasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
    {
        // Content-Disposition: form-data; name="key";
        return contentDisposition != null
               && contentDisposition.DispositionType.Equals("form-data")
               && string.IsNullOrEmpty(contentDisposition.FileName.Value)
               && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
    }

    public static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
    {
        // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
        return contentDisposition != null
               && contentDisposition.DispositionType.Equals("form-data")
               && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                   || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
    }
}