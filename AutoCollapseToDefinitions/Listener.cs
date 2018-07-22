using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;

namespace MD5P1K3.AutoCollapseToDefinitions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType("CSharp")]
    public sealed class Listener : IWpfTextViewCreationListener
    {

        [Import]
        internal IOutliningManagerService OutliningManagerService { get; set; }

        private IWpfTextView wpfTextView;

        public void TextViewCreated(IWpfTextView textView)
        {
            if (OutliningManagerService == null || textView == null)
            {
                return;
            }

            IOutliningManager outliningManager = OutliningManagerService.GetOutliningManager(textView);

            if (outliningManager == null)
            {
                return;
            }

            outliningManager.RegionsChanged += OnRegionsChanged;
            wpfTextView = textView;
        }

        private void OnRegionsChanged(object sender, RegionsChangedEventArgs regionsChangedEventArgs)
        {
            if (sender is IOutliningManager outliningManager && outliningManager.Enabled)
            {
                // Collapses all of the regions within the span where Match() returns true.
                outliningManager.CollapseAll(regionsChangedEventArgs.AffectedSpan, Match);
            }
        }

        // Returns true when the collapsible should be collapsed.
        private bool Match(ICollapsible collapsible)
        {
            try
            {
                //Get region text
                string regionText = collapsible?.Extent?.GetText(wpfTextView.TextSnapshot);

                //Check if collapsing is wanted
                bool collapse = CheckToCollapse(regionText, collapsible.Tag.IsImplementation);

                //Collapse if wanted
                if (collapse)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("AutoCollapseToDefinitions", ex.ToString());

                return false;
            }
        }

        //Check if collapse is wanted
        private bool CheckToCollapse(string regionText, bool isImplementation)
        {
            //Null check regions
            if (string.IsNullOrEmpty(regionText))
                return false;

            //Split region text to array
            string[] lines = Regex.Split(regionText, Environment.NewLine);

            //Check if array contains "class" and if is implementation
            return lines != null && lines.Length > 1 && lines.Where(w => w.Contains("class")).Count() == 0 && isImplementation;
        }
    }
}
