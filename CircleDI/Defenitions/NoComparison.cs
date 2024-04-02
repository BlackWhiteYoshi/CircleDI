namespace CircleDI.Defenitions;

public readonly struct NoComparison<T> : IEqualityComparer<T> {
    public static NoComparison<T> Instance { get; } = default;

    public bool Equals(T x, T y) => false;

    public int GetHashCode(T obj) => 0;
}
