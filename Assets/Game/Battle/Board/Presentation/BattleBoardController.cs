using System;
using UnityEngine;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BattleBoardController : MonoBehaviour
    {
        [SerializeField]
        private BattleBoardView boardView = null;

        private bool initialized;

        public BoardState CurrentBoard { get; private set; }
        public bool IsInitialized => initialized;

        private void Start()
        {
            try
            {
                Initialize();
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Initial board setup failed.",
                    this);
                enabled = false;
            }
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (boardView == null)
            {
                throw new InvalidOperationException(
                    "A BattleBoardView must be assigned.");
            }

            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null)
            {
                throw new InvalidOperationException(
                    "GameBootstrapper is not available.");
            }

            if (bootstrapper.RandomSource == null)
            {
                throw new InvalidOperationException(
                    "GameBootstrapper.RandomSource is not initialized.");
            }

            var idGenerator = new BoardBlockIdGenerator();
            var moveAnalyzer = new BoardMoveAnalyzer();
            var generator = new BoardGenerator(
                bootstrapper.RandomSource,
                BoardMatchFinder.FindMatches,
                moveAnalyzer,
                idGenerator);

            BoardState generatedBoard = generator.Generate();
            boardView.Render(generatedBoard);
            CurrentBoard = generatedBoard;
            initialized = true;
        }
    }
}
