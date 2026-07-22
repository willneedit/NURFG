#if TOOLS
using System;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;

namespace NURFG
{
    public class MatchEverythingTestFilter : ITestFilter
    {
        public TNode AddToXml(TNode parentNode, bool recursive) => null;
        public TNode ToXml(bool recursive) => null;

        public bool IsExplicitMatch(ITest test) => true;
        public bool Pass(ITest test) => true;
    }

    public class MatchFailedTestFilter : ITestFilter
    {
        private readonly Dictionary<ITest, ITestResult> _possibleResults;

        public MatchFailedTestFilter(Dictionary<ITest, ITestResult> testResults)
        {
            _possibleResults = testResults;
        }

        public TNode AddToXml(TNode parentNode, bool recursive) => null;
        public TNode ToXml(bool recursive) => null;

        public bool IsExplicitMatch(ITest test) => true;

        public bool Pass(ITest test)
        {
            // Is a suite, check its children
            if (test.HasChildren) return false;

            // Not run, take it.
            if (!_possibleResults.ContainsKey(test))
                return true;
 
            // In progress, skip it.
            if (_possibleResults[test] == null) return false;

            TestStatus result = _possibleResults[test].ResultState.Status;

            if (result == TestStatus.Failed || result == TestStatus.Inconclusive || result == TestStatus.Skipped)
                return true;
 
            // Which remains Passed or Warned.
            return false;
        }
    }

    public class MatchDescendantsOfFilter : ITestFilter
    {
        private readonly ITest _possibleParent;

        public MatchDescendantsOfFilter(ITest test)
        {
            _possibleParent = test;
        }

        public TNode AddToXml(TNode parentNode, bool recursive) => null;
        public TNode ToXml(bool recursive) => null;

        public bool IsExplicitMatch(ITest test) => IsDescendantOf(_possibleParent, test);
        public bool Pass(ITest test) => IsDescendantOf(_possibleParent, test);

        private bool IsDescendantOf(ITest possibleParent, ITest possibleChild)
        {
            if (possibleChild == possibleParent)
                return true;
            
            if (possibleChild.Parent == null)
                return false;

            return IsDescendantOf(possibleParent, possibleChild.Parent);
        }
    }
}
#endif