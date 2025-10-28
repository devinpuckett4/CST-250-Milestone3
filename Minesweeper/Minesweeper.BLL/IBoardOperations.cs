using System;
using Minesweeper.Models;

namespace Minesweeper.BLL
{
    // contract the console app and tests use
    public interface IBoardOperations
    {
        void SetupBombs(BoardModel board);
        void CountBombsNearby(BoardModel board);

        // preferred entry point for clicks handles bomb, number, and flood-fill 
        void RevealCell(BoardModel board, int r, int c);

        [Obsolete("Use RevealCell(board, r, c) instead for proper flood-fill behavior.")]
        bool VisitCell(BoardModel board, int r, int c);

        void ToggleFlag(BoardModel board, int r, int c);
        string UseRewardPeek(BoardModel board, int r, int c);
        GameState DetermineGameState(BoardModel board);
    }
}