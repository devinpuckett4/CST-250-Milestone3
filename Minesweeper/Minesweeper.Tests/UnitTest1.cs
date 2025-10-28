using Minesweeper.BLL;
using Minesweeper.Models;
using Xunit;

namespace Minesweeper.Tests
{
    // Basic unit tests for the BLL neighbor counting logic
    public class BoardServiceTests
    {
        [Fact]
        public void CountBombsNearby_counts_neighbors_around_a_manual_bomb()
        {
            // 3x3 board, no random bombs
            var board = new BoardModel(3) { DifficultyPercentage = 0f };
            var svc = new BoardService();

            // place a bomb manually in the center
            board.Cells[1, 1].IsBomb = true;

            // run the neighbor counting
            svc.CountBombsNearby(board);

            // bomb cells should have sentinel 9
            Assert.Equal(9, board.Cells[1, 1].NumberOfBombNeighbors);

            // a corner next to the center bomb should see exactly 1 neighbor bomb
            Assert.Equal(1, board.Cells[0, 0].NumberOfBombNeighbors);
        }
    }
}