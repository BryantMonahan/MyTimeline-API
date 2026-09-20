using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace mongoAPI.Types
{
    public record CheckUploadRequest
    (
               [Required(ErrorMessage = "ObjectKey is required")] string ObjectKey,
               [Required(ErrorMessage = "Transcribe is required")] Boolean? Transcribe,
               [Required(ErrorMessage = "Title is required")] string Title,
               string? Description
    );
}