namespace Zepheus.Util
{
    public sealed class Pair<T1, T2>
    {
        public T1 First { get; private set; }
        public T2 Second { get; private set; }

        public Pair(T1 pFirst, T2 pSecond)
        {
            this.First = pFirst;
            this.Second = pSecond;
        }
    }
}
