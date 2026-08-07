using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.State
{
    public sealed class MarkStateTests
    {
        [Test]
        public void AddConsumeAndConsumeAllRespectBounds()
        {
            var mark = new MarkState("mark", 3);

            Assert.That(mark.Add(5), Is.EqualTo(3));
            Assert.That(mark.CurrentStacks, Is.EqualTo(3));
            Assert.That(mark.HasAny, Is.True);
            Assert.That(mark.Consume(2), Is.EqualTo(2));
            Assert.That(mark.Consume(5), Is.EqualTo(1));
            Assert.That(mark.HasAny, Is.False);
            mark.Add(2);
            Assert.That(mark.ConsumeAll(), Is.EqualTo(2));
            Assert.That(mark.CurrentStacks, Is.Zero);
        }

        [Test]
        public void CollectionKeepsMarksSeparateFromResources()
        {
            var marks = new MarkCollection();
            marks.Register("mark", 4);

            Assert.That(marks.Add("mark", 2), Is.EqualTo(2));
            Assert.That(marks.GetStacks("mark"), Is.EqualTo(2));
            Assert.That(marks.ConsumeAll("mark"), Is.EqualTo(2));
            Assert.Throws<ArgumentException>(() =>
                marks.Register("mark", 4));
        }
    }
}
