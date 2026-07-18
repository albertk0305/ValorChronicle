using System;
using NUnit.Framework;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class ProfileIdProviderTests
    {
        [Test]
        public void CreateProfileId_ReturnsNFormatGuid()
        {
            string id = new GuidProfileIdProvider().CreateProfileId();

            Assert.That(id, Is.Not.Null.And.Not.Empty);
            Assert.That(string.IsNullOrWhiteSpace(id), Is.False);
            Assert.That(id, Has.Length.EqualTo(32));
            Assert.That(Guid.TryParseExact(id, "N", out _), Is.True);
        }

        [Test]
        public void CreateProfileId_ConsecutiveCallsReturnDifferentIds()
        {
            var provider = new GuidProfileIdProvider();

            Assert.That(provider.CreateProfileId(), Is.Not.EqualTo(provider.CreateProfileId()));
        }
    }
}
