using System.Collections.Generic;

namespace PurviewAutomation.Models;

internal class Rule
{
    public string AttributeName { get; set; }
    public string? AttributeValueIncludes { get; set; }
    public string? FromRule { get; set; }
    public List<string>? AttributeValueIncludedIn { get; set; }
}
