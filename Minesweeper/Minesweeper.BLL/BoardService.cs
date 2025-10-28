using System;
using Minesweeper.Models;

namespace Minesweeper.BLL
{
    // game rules (bombs, counts, reward, visit/flag, and game state)
    public class BoardService : IBoardOperations
    {
        private readonly Random _rng;

        public BoardService() : this(new Random()) { }
        public BoardService(Random rng) { _rng = rng; }

        // reset all cells, place bombs using DifficultyPercentage, place one reward on a safe cell
        public void SetupBombs(BoardModel board)
        {
            board.StartTime = DateTime.UtcNow;
            board.EndTime = null;
            board.GameState = GameState.StillPlaying;
            board.RewardsRemaining = 0;

            for (int r = 0; r < board.Size; r++)
            {
                for (int c = 0; c < board.Size; c++)
                {
                    var cell = board.Cells[r, c];
                    cell.IsVisited = false;
                    cell.IsFlagged = false;
                    cell.HasSpecialReward = false;
                    cell.NumberOfBombNeighbors = 0;
                    cell.IsBomb = _rng.NextDouble() < board.DifficultyPercentage;
                }
            }

            TryPlaceSingleReward(board);

            // Important: compute neighbor counts after bombs are placed
            CountBombsNearby(board);
        }

        // pick a random safe cell to hold the reward 
        private void TryPlaceSingleReward(BoardModel board)
        {
            int n = board.Size;
            var safe = new (int r, int c)[n * n];
            int k = 0;

            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (!board.Cells[r, c].IsBomb)
                        safe[k++] = (r, c);

            if (k == 0) return;                  // no safe cells, skip
            var pick = safe[_rng.Next(k)];       // choose one safe cell
            board.Cells[pick.r, pick.c].HasSpecialReward = true;
            board.RewardsRemaining = 1;          // one peek available after you find it
        }

        // set 9 for bombs; otherwise count bombs in the neighbors
        public void CountBombsNearby(BoardModel board)
        {
            int n = board.Size;

            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    var cell = board.Cells[r, c];

                    if (cell.IsBomb)
                    {
                        cell.NumberOfBombNeighbors = 9; // sentinel for bombs
                        continue;
                    }

                    int count = 0;
                    for (int dr = -1; dr <= 1; dr++)
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            if (dr == 0 && dc == 0) continue; // skip self
                            int rr = r + dr, cc = c + dc;
                            if (rr >= 0 && rr < n && cc >= 0 && cc < n && board.Cells[rr, cc].IsBomb)
                                count++;
                        }

                    cell.NumberOfBombNeighbors = count;
                }
            }
        }

        public void RevealCell(BoardModel board, int r, int c)
        {
            if (!InBounds(board, r, c)) return;

            var cell = board.Cells[r, c];

            // ignore if already revealed or flagged
            if (cell.IsVisited || cell.IsFlagged) return;

            // pick up reward if present on first reveal
            if (cell.HasSpecialReward)
            {
                cell.HasSpecialReward = false;
                board.RewardsRemaining += 1;
            }

            if (cell.IsBomb)
            {
                cell.IsVisited = true; // reveal bomb
                return;
            }

            if (cell.NumberOfBombNeighbors > 0)
            {
                cell.IsVisited = true; // reveal just the number
                return;
            }

            // empty cell (0 neighbors) -> expand
            FloodFillOpening(board, r, c);
        }

        // Recursive flood fill for zero-neighbor regions.
        // Reveals connected zeros and the border of numbered cells.
        private void FloodFillOpening(BoardModel board, int r, int c)
        {
            if (!InBounds(board, r, c)) return;

            var cell = board.Cells[r, c];

            // stop if revealed already, flagged, or a bomb 
            if (cell.IsVisited || cell.IsFlagged || cell.IsBomb) return;

            // reveal current cell
            cell.IsVisited = true;

            // if it's a number (>0), do not expand further
            if (cell.NumberOfBombNeighbors > 0) return;

            // otherwise it's a zero; expand to all 8 neighbors
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int rr = r + dr, cc = c + dc;
                    FloodFillOpening(board, rr, cc);
                }
            }
        }

        // visit a cell; return true if you hit a bomb
        // kept for backwards compatibility for UI should prefer RevealCell now
        public bool VisitCell(BoardModel board, int r, int c)
        {
            if (!InBounds(board, r, c)) return false;

            var cell = board.Cells[r, c];
            if (cell.IsVisited || cell.IsFlagged) return false; // ignore already visited or flagged

            cell.IsVisited = true;

            // pick up the reward if this was the reward cell
            if (cell.HasSpecialReward)
            {
                cell.HasSpecialReward = false;
                board.RewardsRemaining += 1;
            }

            return cell.IsBomb;
        }

        // toggle a flag only allowed if the cell is still hidden
        public void ToggleFlag(BoardModel board, int r, int c)
        {
            if (!InBounds(board, r, c)) return;

            var cell = board.Cells[r, c];
            if (cell.IsVisited) return; // can't flag visited cells

            cell.IsFlagged = !cell.IsFlagged;
        }

        // use the one-time reward to peek a cell; consumes one use if available
        public string UseRewardPeek(BoardModel board, int r, int c)
        {
            if (board.RewardsRemaining <= 0) return "No reward available.";
            if (!InBounds(board, r, c)) return "That position is out of bounds.";

            board.RewardsRemaining -= 1;

            var cell = board.Cells[r, c];
            return cell.IsBomb
                ? "Peek result: This cell IS a bomb."
                : "Peek result: This cell is safe.";
        }

        // figure out if the player won, lost, or is still playing
        public GameState DetermineGameState(BoardModel board)
        {
            int n = board.Size;

            // any visited bomb -> lost
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (board.Cells[r, c].IsBomb && board.Cells[r, c].IsVisited)
                    {
                        board.GameState = GameState.Lost;
                        board.EndTime = DateTime.UtcNow;
                        return board.GameState;
                    }

            // any safe cell not visited -> still playing
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (!board.Cells[r, c].IsBomb && !board.Cells[r, c].IsVisited)
                    {
                        board.GameState = GameState.StillPlaying;
                        return board.GameState;
                    }

            // all safe cells visited -> won
            board.GameState = GameState.Won;
            board.EndTime = DateTime.UtcNow;
            return board.GameState;
        }

        // quick bounds helper
        private bool InBounds(BoardModel board, int r, int c)
            => r >= 0 && r < board.Size && c >= 0 && c < board.Size;
    }
}