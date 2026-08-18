using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace mongoAPI.Types
{
    public record UploadedDaysRequest
    // offset is in minutes
    ([Required(ErrorMessage = "MinutesPastMidnight is required")] int MinutesPastMidnight);
}