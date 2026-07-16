namespace ValorChronicle.Data.Validation
{
    public sealed class ValidationIssue
    {
        public ValidationIssue(
            ValidationSeverity severity,
            string message,
            string definitionId,
            UnityEngine.Object context)
        {
            Severity = severity;
            Message = message;
            DefinitionId = definitionId;
            Context = context;
        }

        public ValidationSeverity Severity { get; }
        public string Message { get; }
        public string DefinitionId { get; }
        public UnityEngine.Object Context { get; }
    }
}
