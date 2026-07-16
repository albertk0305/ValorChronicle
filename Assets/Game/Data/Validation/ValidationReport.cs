using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Data.Validation
{
    public sealed class ValidationReport
    {
        private readonly List<ValidationIssue> issues = new List<ValidationIssue>();
        private readonly ReadOnlyCollection<ValidationIssue> readOnlyIssues;

        public ValidationReport()
        {
            readOnlyIssues = issues.AsReadOnly();
        }

        public IReadOnlyList<ValidationIssue> Issues => readOnlyIssues;
        public bool HasErrors { get; private set; }

        internal void Add(
            ValidationSeverity severity,
            string message,
            string definitionId = null,
            UnityEngine.Object context = null)
        {
            issues.Add(new ValidationIssue(severity, message, definitionId, context));

            if (severity == ValidationSeverity.Error)
            {
                HasErrors = true;
            }
        }
    }
}
