using System.Collections.Generic;

namespace PurviewAutomation.Models;

internal class AttributeRules
{
    public string Kind { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public List<List<Rule>> DnfCondition { get; set; }
}
