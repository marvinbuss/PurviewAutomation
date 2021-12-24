using System.Collections.Generic;

namespace PurviewAutomation.Models.Purview;

internal class Properties
{
    public string Description { get; set; }
    public List<DecisionRule> DecisionRules { get; set; }
    public List<AttributeRules> AttributeRules { get; set; }
    public Collection Collection { get; set; }
}
