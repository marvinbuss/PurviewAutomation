using System.Collections.Generic;

namespace PurviewAutomation.Models.Purview;

internal class DecisionRule
{
    public string Kind { get; set; }
    public string Effect { get; set; }
    public List<List<Rule>> DnfCondition { get; set; }
}
