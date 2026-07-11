namespace SpireChess.Run
{
    public static class SeedDeriver
    {
        public static int Combine(int seed, int streamId)
        {
            unchecked
            {
                var value = seed ^ (streamId * 16777619);
                value = (value ^ (value >> 16)) * -2048144789;
                value = (value ^ (value >> 13)) * -1028477387;
                return value ^ (value >> 16);
            }
        }
    }
}
