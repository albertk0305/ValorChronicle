using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardBlockIdGeneratorTests
    {
        [Test]
        public void Next_DefaultStart_ReturnsSequentialIdsFromOne()
        {
            var generator = new BoardBlockIdGenerator();

            Assert.That(generator.Next(), Is.EqualTo(1));
            Assert.That(generator.Next(), Is.EqualTo(2));
            Assert.That(generator.Next(), Is.EqualTo(3));
        }

        [Test]
        public void Next_CustomStart_ReturnsSequentialIdsFromSpecifiedValue()
        {
            var generator = new BoardBlockIdGenerator(100);

            Assert.That(generator.Next(), Is.EqualTo(100));
            Assert.That(generator.Next(), Is.EqualTo(101));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonPositiveStart_Throws(int firstId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoardBlockIdGenerator(firstId));
        }

        [Test]
        public void Next_DoesNotReturnDuplicateIds()
        {
            var generator = new BoardBlockIdGenerator();
            var ids = new HashSet<int>();

            for (int index = 0; index < 1000; index++)
            {
                Assert.That(ids.Add(generator.Next()), Is.True);
            }
        }

        [Test]
        public void Next_AfterIntMaximum_ThrowsAndDoesNotWrap()
        {
            var generator = new BoardBlockIdGenerator(int.MaxValue);

            Assert.That(generator.Next(), Is.EqualTo(int.MaxValue));
            Assert.Throws<InvalidOperationException>(() => generator.Next());
            Assert.Throws<InvalidOperationException>(() => generator.Next());
        }
    }
}
