namespace Assets.Scripts.Environment.Structs
{
    public struct GeneTuple
    {
        public int X;
        public int Y;
        public int Z;

        public GeneTuple(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static bool operator != (GeneTuple lhs, GeneTuple rhs)
        {
            return lhs.X != rhs.X || lhs.Y != rhs.Y || lhs.Z != rhs.Z;
        }

        public static bool operator == (GeneTuple lhs, GeneTuple rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z;
        }
    }
}