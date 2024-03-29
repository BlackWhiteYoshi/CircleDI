namespace CircleDI.Generation;

/// <summary>
/// An object to handle indentation.
/// </summary>
public struct Indent {
    public const string SP0 = "";
    public const string SP4 = "    ";
    public const string SP8 = "        ";
    public const string SP12 = "            ";
    public const string SP16 = "                ";
    public const string SP20 = "                    ";
    public const string SP24 = "                        ";
    public const string SP28 = "                            ";
    public const string SP32 = "                                ";
    public const string SP36 = "                                    ";
    public const string SP40 = "                                        ";
    public const string SP44 = "                                            ";
    public const string SP48 = "                                                ";

    public string Sp0 { get; private set; } = SP0;
    public string Sp4 { get; private set; } = SP4;
    public string Sp8 { get; private set; } = SP8;
    public string Sp12 { get; private set; } = SP12;
    public string Sp16 { get; private set; } = SP16;
    public string Sp20 { get; private set; } = SP20;
    public string Sp24 { get; private set; } = SP24;


    private int indentLevel = 0;

    public Indent() { }


    private void InitNormalIndent() {
        indentLevel = 0;
        Sp0 = SP0;
        Sp4 = SP4;
        Sp8 = SP8;
        Sp12 = SP12;
        Sp16 = SP16;
        Sp20 = SP20;
        Sp24 = SP24;
    }

    private void InitPlusOneIndent() {
        indentLevel = 1;
        Sp0 = SP4;
        Sp4 = SP8;
        Sp8 = SP12;
        Sp12 = SP16;
        Sp16 = SP20;
        Sp20 = SP24;
        Sp24 = SP28;
    }

    private void InitPlusTwoIndent() {
        indentLevel = 2;
        Sp0 = SP8;
        Sp4 = SP12;
        Sp8 = SP16;
        Sp12 = SP20;
        Sp16 = SP24;
        Sp20 = SP28;
        Sp24 = SP32;
    }

    private void InitPlusThreeIndent() {
        indentLevel = 3;
        Sp0 = SP12;
        Sp4 = SP16;
        Sp8 = SP20;
        Sp12 = SP24;
        Sp16 = SP28;
        Sp20 = SP32;
        Sp24 = SP36;
    }

    private void InitPlusFourIndent() {
        indentLevel = 4;
        Sp0 = SP16;
        Sp4 = SP20;
        Sp8 = SP24;
        Sp12 = SP28;
        Sp16 = SP32;
        Sp20 = SP36;
        Sp24 = SP40;
    }

    private void InitPlusFiveIndent() {
        indentLevel = 5;
        Sp0 = SP20;
        Sp4 = SP24;
        Sp8 = SP28;
        Sp12 = SP32;
        Sp16 = SP36;
        Sp20 = SP40;
        Sp24 = SP44;
    }

    private void InitPlusSixIndent() {
        indentLevel = 6;
        Sp0 = SP24;
        Sp4 = SP28;
        Sp8 = SP32;
        Sp12 = SP36;
        Sp16 = SP40;
        Sp20 = SP44;
        Sp24 = SP48;
    }

    private void InitIndent() {
        int indentBase = indentLevel * 4;

        Sp0 = new string(' ', indentBase);
        Sp4 = new string(' ', indentBase + 4);
        Sp8 = new string(' ', indentBase + 8);
        Sp12 = new string(' ', indentBase + 12);
        Sp16 = new string(' ', indentBase + 16);
        Sp20 = new string(' ', indentBase + 32);
        Sp24 = new string(' ', indentBase + 36);
    }


    public void IncreaseLevel() {
        switch (indentLevel) {
            case 0:
                InitPlusOneIndent();
                break;
            case 1:
                InitPlusTwoIndent();
                break;
            case 2:
                InitPlusThreeIndent();
                break;
            case 3:
                InitPlusFourIndent();
                break;
            case 4:
                InitPlusFiveIndent();
                break;
            case 5:
                InitPlusSixIndent();
                break;
            default:
                indentLevel++;
                InitIndent();
                break;
        }
    }

    public void DecreaseLevel() {
        switch (indentLevel) {
            case 0:
                throw new InvalidOperationException("IndentLevel not supported: IndentLevel was decreased to level -1.");
            case 1:
                InitNormalIndent();
                break;
            case 2:
                InitPlusOneIndent();
                break;
            case 3:
                InitPlusTwoIndent();
                break;
            case 4:
                InitPlusThreeIndent();
                break;
            case 5:
                InitPlusFourIndent();
                break;
            case 6:
                InitPlusFiveIndent();
                break;
            case 7:
                InitPlusSixIndent();
                break;
            default:
                indentLevel--;
                InitIndent();
                break;
        }
    }
}
