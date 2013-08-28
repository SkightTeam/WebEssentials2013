﻿using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class CompositeUsageData : IUsageDataSource
    {
        private readonly UnusedCssExtension _extension;
        private readonly HashSet<RuleUsage> _ruleUsages = new HashSet<RuleUsage>();
        private readonly List<IUsageDataSource> _sources = new List<IUsageDataSource>();
        private readonly object _sync = new object();

        public CompositeUsageData(UnusedCssExtension extension)
        {
            _extension = extension;
        }

        public void AddUsageSource(IUsageDataSource source)
        {
            lock (_sync)
            {
                _sources.Add(source);
                _ruleUsages.UnionWith(source.GetRuleUsages());
            }
        }

        public IEnumerable<CssRule> GetAllRules()
        {
            lock (_sync)
            {
                return CssRuleRegistry.GetAllRules(_extension);
            }
        }

        public IEnumerable<RuleUsage> GetRuleUsages()
        {
            lock (_sync)
            {
                return _ruleUsages;
            }
        }

        public IEnumerable<CssRule> GetUnusedRules()
        {
            lock (_sync)
            {
                var unusedRules = new HashSet<CssRule>(GetAllRules());

                foreach (var src in _sources)
                {
                    unusedRules.IntersectWith(src.GetUnusedRules());
                }

                return unusedRules;
            }
        }

        public IEnumerable<Task> GetWarnings()
        {
            lock(_sync)
            {
                return GetUnusedRules().Select(x => x.ProduceErrorListTask(TaskErrorCategory.Warning, _extension.Connection.Project, "Unused CSS rule \"{1}\""));
            }
        }
        
        public IEnumerable<Task> GetWarnings(Uri uri)
        {
            lock(_sync)
            {
                return GetUnusedRules().Select(x => x.ProduceErrorListTask(TaskErrorCategory.Warning, _extension.Connection.Project, "Unused CSS rule \"{1}\" on page " + uri));
            }
        }
    }
}