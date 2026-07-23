using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
#if TOOLS
using Godot;
using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;

namespace NURFG
{
    [Tool]
    public partial class TestRunnerDock : Control
    {
        private FrameworkController _nunit;

        private readonly Dictionary<ITest, TreeItem> _testTreeItems = new Dictionary<ITest, TreeItem>();
        private readonly Dictionary<ITest, ITestResult> _testResults = new Dictionary<ITest, ITestResult>();

        private Button _btn_refresh;
        private Button _btn_run;
        private Button _btn_runFailed;
        private Button _btn_clearResults;
        private Tree _tre_results;
        private RichTextLabel _lbl_testOutput;

        public override void _Ready()
        {
            base._Ready();
            
            InitializeNUnitIfNeeded();

            _btn_refresh = (Button)GetNode("HBoxContainer/RefreshButton");
            _btn_run = (Button)GetNode("HBoxContainer/RunButton");
            _btn_runFailed = (Button)GetNode("HBoxContainer/RunFailedButton");
            _btn_clearResults = (Button)GetNode("HBoxContainer/ClearResultsButton");
            _tre_results = (Tree)GetNode("VSplitContainer/ResultTree");
            _lbl_testOutput = (RichTextLabel)GetNode("VSplitContainer/TestOutputLabel");

            RefreshButton_Click();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            InitializeNUnitIfNeeded();
            EnableButtons(!_nunit.Runner.IsTestRunning);
        }

        private void InitializeNUnitIfNeeded()
        {
            if (_nunit != null)
                return;

            GD.Print("Initializing NUnit");

            _nunit = new FrameworkController(
                Assembly.GetExecutingAssembly(),
                "gnur",
                new Dictionary<string, object>()
            );
            _nunit.LoadTests();
        }


        private void RefreshButton_Click()
        {
            _testTreeItems.Clear();
            _tre_results.Clear();

            CreateTreeItemForTest(_nunit.Runner.LoadedTest);

            foreach (var test in _testTreeItems.Keys)
                UpdateTestTreeItem(test);
        }

        private void ClearResultsButton_Click()
        {
            _testResults.Clear();

            foreach (var test in _testTreeItems.Keys)
                UpdateTestTreeItem(test);
        }

        private void RunButton_Click()
        {
            RefreshButton_Click();
            StartTestRun(new MatchEverythingTestFilter());
        }

        private void RunFailedButton_Click()
        {
            StartTestRun(new MatchFailedTestFilter(_testResults));
        }

        private void TestResultTree_ItemSelected()
        {
            var selectedItem = _tre_results.GetSelected();
            ITest selectedTest = GetTestFromTreeItem(selectedItem);
            DisplayTestOutput(selectedTest);
        }

        private void TestResultTree_ItemActivated()
        {
            // Run the selected test
            var selectedItem = _tre_results.GetSelected();
            ITest selectedTest = GetTestFromTreeItem(selectedItem);
            StartTestRun(new MatchDescendantsOfFilter(selectedTest));
        }


        private void StartTestRun(ITestFilter filter)
        {
            var testListener = new LambdaListener
            {
                TestStartedCallback = (test) =>
                {
                    CreateTreeItemForTest(test);
                    _testResults[test] = null;
                    UpdateTestTreeItem(test);
                },

                TestFinishedCallback = (result) =>
                {
                    _testResults[result.Test] = result;
                    UpdateTestTreeItem(result.Test);
                }
            };

            // Start running the tests in the background.
            _nunit.Runner.RunAsync(testListener, filter);
        }

        private void EnableButtons(bool enabled)
        {
            bool disabled = !enabled;

            _btn_refresh.Disabled = disabled;
            _btn_run.Disabled = disabled;
            _btn_runFailed.Disabled = disabled;
            _btn_clearResults.Disabled = disabled;
        }


        private void UpdateTestTreeItem(ITest test)
        {
            var treeItem = _testTreeItems[test];
            // Need to be deferred, tests can run in worker threads, not
            // main (aka UI) threads.
            treeItem.CallDeferred(TreeItem.MethodName.SetText, 0, GetTestLabel(test));
            DisplayTestOutput(test);

            // Recursively update all ancestor items
            if (test.Parent != null)
                UpdateTestTreeItem(test.Parent);
        }

        private string GetTestLabel(ITest test)
        {
            var state = GetTestState(test);
            string icon = TestStateToIcon(state);

            if (!test.IsSuite)
                return $"{icon} {test.Name}";

            if (!_testResults.ContainsKey(test) || _testResults[test] == null)
                return $"{icon} {test.Name} ({test.TestCaseCount} found)";

            var result = _testResults[test];
            return $"{icon} {test.Name} ({result.PassCount} / {test.TestCaseCount} passing)";
        }

        /// <summary>
        /// Gets a value corresponding to the "icon" that should be displayed
        /// next to a test's name
        /// 
        /// If there are children, it examines the results of all of them and
        /// returns the "worst" of them.
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        private TestState GetTestState(ITest test)
        {
            // Recursive case: find the worst of the children.
            if (test.HasChildren)
            {
                var worstState = TestState.Passed;

                foreach (var child in test.Tests)
                {
                    var childState = GetTestState(child);
                    if (childState < worstState)
                        worstState = childState;
                }

                return worstState;
            }

            // Tests that haven't been run do not have an entry in _testResults.
            if (!_testResults.ContainsKey(test))
                return TestState.NotRun;

            // Tests that are in progress have a null entry in _testResults
            else if (_testResults[test] == null)
                return TestState.InProgress;

            // All others are self-explanatory.
            switch (_testResults[test].ResultState.Status)
            {
                case TestStatus.Failed: return TestState.Failed;
                case TestStatus.Inconclusive: return TestState.Inconclusive;
                case TestStatus.Passed: return TestState.Passed;
                case TestStatus.Skipped: return TestState.Skipped;
                case TestStatus.Warning: return TestState.Warning;
            }

            throw new Exception("Unexpected TestStatus " + _testResults[test].ResultState.Status);
        }

        private enum TestState
        {
            InProgress = 0,
            Failed = 1,
            Warning = 2,
            Inconclusive = 3,
            Skipped = 4,
            NotRun = 5,
            Passed = 6
        }

        private string TestStateToIcon(TestState state)
        {
            switch (state)
            {
                case TestState.NotRun: return "?";
                case TestState.InProgress: return "⏳";
                case TestState.Passed: return "✔";
                case TestState.Failed: return "❌";
                case TestState.Inconclusive: return "?";
                case TestState.Warning: return "⚠";

                default: return $"[{state.ToString().ToUpper()}]";
            }
        }

        private void DisplayTestOutput(ITest test)
        {
            if (test != GetTestFromTreeItem(_tre_results.GetSelected())) return;

            _lbl_testOutput.SetDeferred(RichTextLabel.PropertyName.Text, GetTestOutput(test));
        }

        private string GetTestOutput(ITest test)
        {
            var builder = new System.Text.StringBuilder();

            void PrintIfNotEmpty(string msg)
            {
                if (!string.IsNullOrWhiteSpace(msg))
                    builder.AppendLine(msg);
            }

            if (test == null)
                return string.Empty;

            if (!_testResults.ContainsKey(test))
                return $"{test.Name} (not run)";

            if (_testResults[test] == null)
                return $"{test.Name} (in progress...)";

            var testResult = _testResults[test];

            builder.AppendLine(testResult.Name);
            PrintIfNotEmpty(testResult.Message);
            PrintIfNotEmpty(testResult.Output);

            if (testResult.ResultState.Status != TestStatus.Passed)
                PrintIfNotEmpty(testResult.StackTrace);

            return builder.ToString();
        }

        private void CreateTreeItemForTest(ITest test)
        {
            if (_testTreeItems.ContainsKey(test))
                return;

            // Create a tree item for this test
            var parentTreeItem = test.Parent == null
                ? null
                : _testTreeItems[test.Parent];

            var treeItem = _tre_results.CreateItem(parentTreeItem);
            _testTreeItems[test] = treeItem;

            // Create tree items for all child tests
            foreach (var child in test.Tests)
                CreateTreeItemForTest(child);
        }

        private ITest GetTestFromTreeItem(TreeItem treeItem)
        {
            return _testTreeItems
                .Where(kvp => kvp.Value == treeItem)
                .Select(kvp => kvp.Key)
                .FirstOrDefault();
        }
    }
}
#endif