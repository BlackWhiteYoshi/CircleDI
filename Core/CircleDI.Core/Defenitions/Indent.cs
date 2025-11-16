namespace CircleDI.Defenitions;

/// <summary>
/// An int wrapper to handle indentation.
/// </summary>
public struct Indent {
    public const int AMOUNT = 4;
    public const char CHAR = ' ';


    public int Level { get; private set; }

    public void IncreaseLevel() => Level += AMOUNT;

    public void DecreaseLevel() => Level -= AMOUNT;
}
