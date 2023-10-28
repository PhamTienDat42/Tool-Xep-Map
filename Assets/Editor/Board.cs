using System;
using System.Text;
using System.Collections.Generic;

namespace Tool
{
	public class Board
	{
		private readonly int size;

		private ulong hMask = 0ul;
		private ulong vMask = 0ul;

		private readonly List<Piece> pieces = new();
		private bool isAbleToSolve;
		private readonly bool isValidMap = true;

		public Board(int size, string data)
		{
			this.size = size;

			var positions = new Dictionary<char, List<int>>();
			var labels = new SortedSet<char>();
			var barriers = new List<int>();
			for (var i = 0; i < data.Length; ++i)
			{
				var label = data[i];
				if (label == '.')
				{
					continue;
				}
				if (label == 'x')
				{
					barriers.Add(i);
					continue;
				}
				if (positions.ContainsKey(label) == false)
				{
					positions.Add(label, new List<int>());
				}
				positions[label].Add(i);
				labels.Add(label);
			}

			if(labels.Contains('A') == false)
			{
				isValidMap = false;
				return;
			}
			
			foreach (var label in labels)
			{
				var position = positions[label];
				if (label == 'A' && position.Count != 2)
				{
					isValidMap = false;
					return;
				}
				if (position.Count > 3 || position.Count == 1)
				{
					isValidMap = false;
					return;
				}
				if (position.Count == 2)
				{
					if (position[1] - position[0] != 1 & position[1] - position[0] != 6)
					{
						isValidMap = false;
						return;
					}
				}
				if (position.Count == 3)
				{
					if (position[1] - position[0] != 1 & position[1] - position[0] != 6 & position[2] - position[1] != 1 & position[2] - position[1] != 6)
					{
						isValidMap = false;
						return;
					}
				}
				isValidMap = true;
				AddPiece(new Piece(position[0], position.Count, position[1] - position[0]));
			}

			foreach (var barrier in barriers)
			{
				AddPiece(new Piece(barrier, 1, 1));
			}
		}

		private Board(Board board)
		{
			this.size = board.size;
			this.hMask = board.hMask;
			this.vMask = board.vMask;
			foreach (var piece in board.pieces)
			{
				this.pieces.Add(new Piece(piece));
			}
		}

		public List<Move> Solve2(int target)
		{
			if (pieces[0].Position == target)
			{
				return new List<Move>();
			}

			var leftColumn = 0ul;
			for (var i = 0; i < size; ++i)
			{
				var ii = i * size + 0;
				leftColumn |= 1ul << ii;
			}
			var rightColumn = 0ul;
			for (var i = 0; i < size; ++i)
			{
				var ii = i * size + (size - 1);
				rightColumn |= 1ul << ii;
			}
			var topRow = 0ul;
			for (var i = 0; i < size; ++i)
			{
				var ii = 0 * size + i;
				topRow |= 1ul << ii;
			}
			var bottomRow = 0ul;
			for (var i = 0; i < size; ++i)
			{
				var ii = (size - 1) * size + i;
				bottomRow |= 1ul << ii;
			}

			var scores = new Dictionary<Tuple<ulong, ulong>, int>();
			var movesBuffer = new List<Move>();
			for (int i = 1; i < 100 ; ++i)
			{
				var moves = new List<Move>(new Move[i + 1]);
				if (Search2(target, leftColumn, rightColumn, topRow, bottomRow, i, moves, movesBuffer, scores))
				{
					isAbleToSolve = true;
					return moves;
				}
			}
			isAbleToSolve = false;
			return new List<Move>();
		}

		private void AddPiece(Piece piece)
		{
			pieces.Add(piece);
			if (piece.Stride == 1)
			{
				hMask |= piece.Mask;
			}
			else
			{
				vMask |= piece.Mask;
			}
		}

		private void DoMove(int pieceIndex, int steps)
		{
			var piece = pieces[pieceIndex];
			if (piece.Stride == 1)
			{
				hMask &= ~piece.Mask;
				piece.Move(steps);
				hMask |= piece.Mask;
			}
			else
			{
				vMask &= ~piece.Mask;
				piece.Move(steps);
				vMask |= piece.Mask;
			}
		}

		public void DoMove(Move move)
		{
			DoMove(move.PieceIndex, move.Steps);
		}

		public void UndoMove(Move move)
		{
			DoMove(move.PieceIndex, -move.Steps);
		}

		private void Moves2(ulong leftColumn, ulong rightColumn, ulong topRow, ulong bottomRow, List<Move> movesBuffer, int previousPieceIndex)
		{
			movesBuffer.Clear();
			var mask = hMask | vMask;
			for (var i = 0; i < pieces.Count; ++i)
			{
				if (i == previousPieceIndex)
				{
					continue;
				}
				var piece = pieces[i];
				if (piece.Size == 1)
				{
					continue;
				}

				if (piece.Stride == 1)
				{
					if ((piece.Mask & leftColumn) == 0)
					{
						var m = (piece.Mask >> 1) & ~piece.Mask;
						var steps = -1;
						while ((mask & m) == 0)
						{
							movesBuffer.Add(new Move { PieceIndex = i, Steps = steps });
							if ((m & leftColumn) != 0)
							{
								break;
							}
							m >>= 1;
							--steps;
						}
					}
					if ((piece.Mask & rightColumn) == 0)
					{
						var m = (piece.Mask << 1) & ~piece.Mask;
						var steps = 1;
						while ((mask & m) == 0)
						{
							movesBuffer.Add(new Move { PieceIndex = i, Steps = steps });
							if ((m & rightColumn) != 0)
							{
								break;
							}
							m <<= 1;
							++steps;
						}
					}
				}
				else
				{
					if ((piece.Mask & topRow) == 0)
					{
						var m = (piece.Mask >> size) & ~piece.Mask;
						var steps = -1;
						while ((mask & m) == 0)
						{
							movesBuffer.Add(new Move { PieceIndex = i, Steps = steps });
							if ((m & topRow) != 0)
							{
								break;
							}
							m >>= size;
							--steps;
						}
					}
					if ((piece.Mask & bottomRow) == 0)
					{
						var m = (piece.Mask << size) & ~piece.Mask;
						var steps = 1;
						while ((mask & m) == 0)
						{
							movesBuffer.Add(new Move { PieceIndex = i, Steps = steps });
							if ((m & bottomRow) != 0)
							{
								break;
							}
							m <<= size;
							++steps;
						}
					}
				}
			}
		}


		private bool Search2(int target, ulong leftColumn, ulong rightColumn, ulong topRow, ulong bottomRow, int maxDepth, List<Move> moves, List<Move> movesBuffer, Dictionary<Tuple<ulong, ulong>, int> scores)
		{
			var open = new Stack<Node>();
			open.Push(new Node
			{
				Move = new Move { PieceIndex = 0, Steps = 0 },
				Depth = 0,
				PreviousPieceIndex = -1
			});

			var board = new Board(this);
			moves[0] = new Move { PieceIndex = 0, Steps = 0 };
			var currentDepth = 0;

			while (open.Count > 0)
			{
				var node = open.Pop();

				for (var i = currentDepth; i >= node.Depth; --i)
				{
					board.UndoMove(moves[i]);
				}

				board.DoMove(node.Move);
				currentDepth = node.Depth;
				moves[currentDepth] = node.Move;

				var score = maxDepth - node.Depth;
				if (score <= 0)
				{
					if (board.pieces[0].Position == target)
					{
						return true;
					}
					continue;
				}

				var key = new Tuple<ulong, ulong>(board.hMask, board.vMask);
				if (scores.TryGetValue(key, out int value) == true && value >= score)
				{
					continue;
				}
				scores[key] = score;

				var mask = board.hMask | board.vMask;
				var primaryPiece = board.pieces[0];
				var i0 = primaryPiece.Position + primaryPiece.Size;
				var i1 = target + primaryPiece.Size - 1;
				var minMoves = 0;
				for (var i = i0; i <= i1; ++i)
				{
					if (((1ul << i) & mask) != 0)
					{
						++minMoves;
					}
				}
				if (minMoves > score)
				{
					continue;
				}

				board.Moves2(leftColumn, rightColumn, topRow, bottomRow, movesBuffer, node.PreviousPieceIndex);
				foreach (var move in movesBuffer)
				{
					open.Push(new Node
					{
						Move = move,
						Depth = node.Depth + 1,
						PreviousPieceIndex = move.PieceIndex
					});
				}
			}

			return false;
		}

		public override string ToString()
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.Append('-', size * size);

			for (var i = 0; i < pieces.Count; ++i)
			{
				var piece = pieces[i];
				var c = piece.Size == 1 ? 'X' : (char)('A' + i);
				var position = piece.Position;
				for (var j = 0; j < piece.Size; ++j)
				{
					stringBuilder[position] = c;
					position += piece.Stride;
				}
			}

			return stringBuilder.ToString();
		}

		public List<Piece> Pieces => pieces;
		public bool IsAbleToSolve => isAbleToSolve;
		public bool IsValidMap => isValidMap;
	}
}
