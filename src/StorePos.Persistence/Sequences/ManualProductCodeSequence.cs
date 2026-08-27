namespace StorePos.Persistence.Sequences;

public sealed class ManualProductCodeSequence
{
    public const int SingletonId = 1;
    public const long InitialCode = 1000;

    private ManualProductCodeSequence()
    {
    }

    public int Id { get; private set; }

    public long NextCode { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static ManualProductCodeSequence Initialize(long nextCode)
    {
        if (nextCode < InitialCode)
        {
            throw new ArgumentOutOfRangeException(nameof(nextCode));
        }

        return new ManualProductCodeSequence
        {
            Id = SingletonId,
            NextCode = nextCode
        };
    }

    public void AdvanceTo(long nextCode)
    {
        if (nextCode <= NextCode)
        {
            throw new ArgumentOutOfRangeException(nameof(nextCode));
        }

        NextCode = nextCode;
    }
}
