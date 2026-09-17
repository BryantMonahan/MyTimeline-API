using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace mongoAPI.Types
{
    public record AudioObjectKey(
        [Required(ErrorMessage = "ObjectKey is required")] string ObjectKey
    );
}