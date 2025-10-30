// CJS_IChoiceRoller.cs
using System.Collections.Generic;

public interface CJS_IChoiceRoller
{
    // È®·üÃß°¡
    List<CJS_ChoiceData> Roll3(out Dictionary<CJS_ChoiceData, float> rollChances);
    void PushPicked(CJS_ChoiceData picked);
}
