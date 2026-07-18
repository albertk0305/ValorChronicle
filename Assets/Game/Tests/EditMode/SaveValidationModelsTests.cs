using System.Linq;
using NUnit.Framework;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveValidationModelsTests
    {
        [Test]
        public void Report_PreservesIssuesAndAggregatesState()
        {
            var report = new SaveValidationReport();
            report.Add(new SaveValidationIssue(SaveValidationCode.NegativeCurrency, SaveValidationSeverity.RecoverableError, "Currencies.GachaCurrency", "negative", true, true));
            report.Add(new SaveValidationIssue(SaveValidationCode.ReferenceCatalogUnavailable, SaveValidationSeverity.Warning, "Catalog.Gacha", "unavailable", false));

            Assert.That(report.Issues, Has.Count.EqualTo(2));
            Assert.That(report.HasWarnings, Is.True);
            Assert.That(report.HasRecoverableErrors, Is.True);
            Assert.That(report.HasFatalErrors, Is.False);
            Assert.That(report.WasModified, Is.True);
            Assert.That(report.Contains(SaveValidationCode.NegativeCurrency), Is.True);
            Assert.That(report.Find(SaveValidationCode.NegativeCurrency, "Currencies.GachaCurrency").Single().Message, Is.EqualTo("negative"));
        }
    }
}
