using System.Collections.Generic;

namespace PurviewAutomation.Models;

internal class DecisionRule
{
    public string Kind { get; set; }
    public string Effect { get; set; }
    public List<List<Rule>> DnfCondition { get; set; }
}
