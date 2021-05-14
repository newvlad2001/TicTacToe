using System;

namespace Game
{
    public class TicTacToe
    {
        private bool?[,] _field;
        
        public enum Players
        {
            X,
            O
        }

        public TicTacToe()
        {
            _field = new bool?[3, 3];
        }

        public void Move(int x, int y, bool isX)
        {
            if (x >= 0 && x < 3 && y >= 0 && y < 3)
            {
                if (_field[y, x] == null)
                {
                    _field[y, x] = isX;
                }
                else
                {
                    throw new ArgumentException("This cell is already filled");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(x), "x and y must be in range [0;3)");
            }
        }

        public bool IsWinner(bool isX)
        {
            return (
                // Rows
                (_field[0, 0] == isX && _field[0, 1] == isX && _field[0, 2] == isX) ||
                (_field[1, 0] == isX && _field[1, 1] == isX && _field[1, 2] == isX) ||
                (_field[2, 0] == isX && _field[2, 1] == isX && _field[2, 2] == isX) ||
                // Columns
                (_field[0, 0] == isX && _field[1, 0] == isX && _field[2, 0] == isX) ||
                (_field[0, 1] == isX && _field[1, 1] == isX && _field[2, 1] == isX) ||
                (_field[0, 2] == isX && _field[1, 2] == isX && _field[2, 2] == isX) ||
                // Diagonals
                (_field[0, 0] == isX && _field[1, 1] == isX && _field[2, 2] == isX) ||
                (_field[0, 2] == isX && _field[1, 1] == isX && _field[2, 0] == isX)
            );
        }
        
        public bool IsDraw()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (_field[i, j] == null)
                        return false;
                }
            }

            return true;
        }
    }
}