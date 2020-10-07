using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateWin10
{

    public class FirewallPreset: PresetItem
    {
        public Guid ProgSetId;
        public ProgramSet.Config.AccessLevels OnState;
        public ProgramSet.Config.AccessLevels OffState;

        public struct SingleRule
        {
            public string RuleId;
            public bool? OnState;
            public bool? OffState;
        }

        public Dictionary<string, SingleRule> Rules = new Dictionary<string, SingleRule>();

        public override PresetItem Clone()
        {
            FirewallPreset item = new FirewallPreset();

            Clone(item);
            item.ProgSetId = this.ProgSetId;
            item.OnState = this.OnState;
            item.OffState = this.OffState;

            item.Rules = this.Rules.ToDictionary(entry => entry.Key, entry => entry.Value);

            return item;
        }

        public override bool SetState(bool State)
        {
            List<ProgramSet> progs = App.client.GetPrograms(new List<Guid>() { ProgSetId });
            if(progs.Count == 0)
                return false;
            ProgramSet progSet = progs[0];

            progSet.config.NetAccess = State ? this.OnState : this.OffState;

            if (!App.client.UpdateProgram(ProgSetId, progSet.config))
                return false;

            if (progSet.config.NetAccess == ProgramSet.Config.AccessLevels.CustomConfig)
            {
                var progRules = App.client.GetRules(progs.Select(x => x.guid).ToList());

                foreach (var ruleList in progRules)
                {
                    foreach (FirewallRuleEx ruleEntry in ruleList.Value)
                    {
                        SingleRule rule;
                        if (Rules.TryGetValue(ruleEntry.guid, out rule))
                        {
                            bool RuleState = ruleEntry.Enabled;
                            if (State && rule.OnState != null)
                                RuleState = rule.OnState.Value;
                            else if (State == false && rule.OffState != null)
                                RuleState = rule.OffState.Value;
                            if (ruleEntry.Enabled != RuleState)
                            {
                                ruleEntry.Enabled = RuleState;
                                App.client.UpdateRule(ruleEntry);
                            }
                        }
                    }
                }
            }

            return true;
        }

        protected override void StoreNodes(XmlWriter writer)
        {
            writer.WriteElementString("ProgSetId", ProgSetId.ToString());
            writer.WriteElementString("OnState", OnState.ToString());
            writer.WriteElementString("OffState", OffState.ToString());

            foreach (SingleRule item in Rules.Values)
            {
                writer.WriteStartElement("Rule");
                
                writer.WriteElementString("RuleId", item.RuleId);
                if(item.OnState != null) writer.WriteElementString("OnState", item.OnState.Value.ToString());
                if(item.OffState != null) writer.WriteElementString("OffState", item.OffState.Value.ToString());
                
                writer.WriteEndElement();
            }
        }

        protected override bool LoadNode(XmlNode node)
        {
            if (node.Name == "ProgSetId")
                ProgSetId = Guid.Parse(node.InnerText);
            else if (node.Name == "OnState")
                Enum.TryParse(node.InnerText, out OnState);
            else if (node.Name == "OffState")
                Enum.TryParse(node.InnerText, out OffState);
            else if (node.Name == "Rule")
            {
                SingleRule Rule = new SingleRule();
                foreach (XmlNode subNode in node.ChildNodes)
                {
                    if (subNode.Name == "RuleId")
                        Rule.RuleId = subNode.InnerText;
                    else if (subNode.Name == "OnState")
                        Rule.OnState = bool.Parse(subNode.InnerText);
                    else if (subNode.Name == "OffState")
                        Rule.OffState = bool.Parse(subNode.InnerText);
                }
                if(Rule.RuleId != null)
                    Rules.Add(Rule.RuleId, Rule);
            }
            else if (!base.LoadNode(node))
                return false;
            return true;
        }

        public override bool Sync(bool CleanUp = false)
        {
            var rules = App.client.GetRules(new List<Guid>() { ProgSetId });
            if (rules == null)
                return false;

            Dictionary<string, SingleRule> oldRules = new Dictionary<string, SingleRule>(Rules);

            foreach (var rule in rules[ProgSetId])
            {
                if (Rules.ContainsKey(rule.guid))
                    oldRules.Remove(rule.guid);
                else
                    Rules.Add(rule.guid, new SingleRule() { RuleId = rule.guid });
            }

            if (CleanUp)
            {
                foreach (string key in oldRules.Keys)
                    Rules.Remove(key);
            }

            List<ProgramSet> progs = App.client.GetPrograms(new List<Guid>() { ProgSetId });
            if(progs.Count == 0)
                return false;
            ProgramSet progSet = progs[0];

            Name = progSet.config.Name;

            return true;
        }

        public override string GetIcon()
        {
            if (Icon != null && Icon.Length > 0)
                return Icon;
            return Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\FirewallControlPanel.dll");
        }
    }
}
