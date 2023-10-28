namespace Tool
{
	public class Piece
	{
		private int position;
		private readonly int size;
		private readonly int stride;
		private ulong mask;

		public Piece(int position, int size, int stride)
		{
			this.position = position;
			this.size     = size;
			this.stride   = stride;

			for (var i = 0; i < size; ++i)
			{
				mask |= 1ul<<position;
				position += stride;
			}
		}

		public Piece(Piece piece)
		{
			this.position = piece.position;
			this.size     = piece.size;
			this.stride   = piece.stride;
			this.mask     = piece.mask;
		}

		public void Move(int steps)
		{
			var d = stride*steps;
			position += d;
			if (steps > 0)
			{
				mask <<= d;
			}
			else
			{
				mask >>= -d;
			}
		}

		public int Position => position;
		public int Size => size;
		public int Stride => stride;
		public ulong Mask => mask;
	}
}
