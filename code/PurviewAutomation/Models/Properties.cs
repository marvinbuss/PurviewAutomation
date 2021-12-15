using System.Collections.Generic;

namespace PurviewAutomation.Models;

internal class Properties
{
    public string Description { get; set; }
    public List<DecisionRule> DecisionRules { get; set; }
    public List<PurviewClient> AttributeRules { get; set; }
    public Collection Collection { get; set; }
}
