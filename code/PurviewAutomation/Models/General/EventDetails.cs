using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurviewAutomation.Models.General
{
    internal record struct EventDetails
    {
        internal string Status { get; init; }
        internal string Action { get; init; }
        internal string Operation { get; init; }
        internal string Scope { get; init; }
    }
}
