using System.Linq;
using ConnectFour.Core;
using NUnit.Framework;

namespace ConnectFour.Core.Tests
{
    public sealed class BoardModelTests
    {
        [Test]
        public void DropPieceFallsToLowestCell()
        {
            BoardModel board = new BoardModel();

            TurnResult result = board.DropPiece(3);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Movements.Single().To, Is.EqualTo(new GridPosition(3, 0)));
            board.CompleteAnimation();
            Assert.That(board.GetPiece(new GridPosition(3, 0)).OwnerId, Is.EqualTo(1));
        }

        [Test]
        public void DropAndRotateEnterAnimatingAndBlockInput()
        {
            BoardModel board = new BoardModel();
            board.DropPiece(0);

            Assert.That(board.State, Is.EqualTo(BoardState.Animating));
            Assert.That(board.DropPiece(1).Success, Is.False);
            Assert.That(board.RotateGravity(GravityDirection.Left).Success, Is.False);

            board.CompleteAnimation();
            TurnResult rotation = board.RotateGravity(GravityDirection.Left);
            Assert.That(rotation.Success, Is.True);
            Assert.That(board.State, Is.EqualTo(BoardState.Animating));
        }

        [Test]
        public void GravityCompactsPiecesInAllDirections()
        {
            BoardModel board = new BoardModel();
            board.DropPiece(2);
            board.CompleteAnimation();
            board.DropPiece(2);
            board.CompleteAnimation();

            TurnResult up = board.RotateGravity(GravityDirection.Up);
            Assert.That(up.Movements.Any(m => m.To == new GridPosition(2, 5)), Is.True);
            board.CompleteAnimation();

            TurnResult left = board.RotateGravity(GravityDirection.Left);
            Assert.That(left.Movements.Any(m => m.To.Column == 0), Is.True);
            board.CompleteAnimation();

            TurnResult right = board.RotateGravity(GravityDirection.Right);
            Assert.That(right.Movements.Any(m => m.To.Column == BoardModel.Columns - 1), Is.True);
            board.CompleteAnimation();

            TurnResult down = board.RotateGravity(GravityDirection.Down);
            Assert.That(down.Movements.Any(m => m.To.Row == 0), Is.True);
        }

        [Test]
        public void RotatingGravityDoesNotChangeActivePlayer()
        {
            BoardModel board = new BoardModel();

            DropAndComplete(board, 0);
            Assert.That(board.ActivePlayerId, Is.EqualTo(2));

            TurnResult rotation = board.RotateGravity(GravityDirection.Right);
            Assert.That(rotation.Success, Is.True);
            board.CompleteAnimation();

            Assert.That(board.ActivePlayerId, Is.EqualTo(2));
            DropAndComplete(board, 0);
            Assert.That(board.GetPiece(new GridPosition(5, 0)).OwnerId, Is.EqualTo(2));
        }

        [Test]
        public void RepeatedDropsAndRotationsRemainPlayable()
        {
            BoardModel board = new BoardModel();

            for (int turn = 0; turn < 7; turn++)
            {
                DropAndComplete(board, turn % BoardModel.Columns);
                TurnResult rotation = board.RotateGravity(GravityDirection.Down);
                Assert.That(rotation.Success, Is.True, rotation.FailureReason);
                TurnResult completed = board.CompleteAnimation();
                Assert.That(completed.State, Is.EqualTo(BoardState.WaitingForInput));
            }
        }

        [Test]
        public void HorizontalWinIsReportedOnlyAfterCompletion()
        {
            BoardModel board = new BoardModel();
            DropAndComplete(board, 0); DropAndComplete(board, 0);
            DropAndComplete(board, 1); DropAndComplete(board, 1);
            DropAndComplete(board, 2); DropAndComplete(board, 2);
            TurnResult finalDrop = board.DropPiece(3);

            Assert.That(finalDrop.Success, Is.True);
            Assert.That(board.WinnerId, Is.Null);
            TurnResult completed = board.CompleteAnimation();
            Assert.That(completed.Outcome, Is.EqualTo(TurnOutcome.Win));
            Assert.That(completed.WinnerId, Is.EqualTo(1));
        }

        [Test]
        public void VerticalAndDiagonalWinsAreDetected()
        {
            BoardModel vertical = new BoardModel();
            DropAndComplete(vertical, 0); DropAndComplete(vertical, 1);
            DropAndComplete(vertical, 0); DropAndComplete(vertical, 1);
            DropAndComplete(vertical, 0); DropAndComplete(vertical, 1);
            DropAndComplete(vertical, 0);
            Assert.That(vertical.CompleteAnimation().WinnerId, Is.EqualTo(1));

            BoardModel diagonal = new BoardModel();
            DropAndComplete(diagonal, 0); DropAndComplete(diagonal, 1);
            DropAndComplete(diagonal, 1); DropAndComplete(diagonal, 2);
            DropAndComplete(diagonal, 2); DropAndComplete(diagonal, 3);
            DropAndComplete(diagonal, 2); DropAndComplete(diagonal, 3);
            DropAndComplete(diagonal, 3); DropAndComplete(diagonal, 4);
            DropAndComplete(diagonal, 3);
            Assert.That(diagonal.CompleteAnimation().WinnerId, Is.EqualTo(1));
        }

        [Test]
        public void RotationWinActivatesOnlyAfterCompletion()
        {
            BoardModel board = new BoardModel();
            DropAndComplete(board, 0); DropAndComplete(board, 0);
            DropAndComplete(board, 2); DropAndComplete(board, 2);
            DropAndComplete(board, 4); DropAndComplete(board, 4);
            DropAndComplete(board, 6);

            TurnResult rotation = board.RotateGravity(GravityDirection.Left);

            Assert.That(rotation.Success, Is.True);
            Assert.That(board.WinnerId, Is.Null);
            TurnResult completed = board.CompleteAnimation();
            Assert.That(completed.Outcome, Is.EqualTo(TurnOutcome.Win));
            Assert.That(completed.WinnerId, Is.EqualTo(1));
        }

        private static void DropAndComplete(BoardModel board, int line)
        {
            TurnResult result = board.DropPiece(line);
            Assert.That(result.Success, Is.True, result.FailureReason);
            board.CompleteAnimation();
        }
    }
}
