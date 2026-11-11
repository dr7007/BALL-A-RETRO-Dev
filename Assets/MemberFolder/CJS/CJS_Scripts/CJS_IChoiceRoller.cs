using System.Collections.Generic;

public interface CJS_IChoiceRoller
{
    List<CJS_ChoiceData> Roll3(out Dictionary<CJS_ChoiceData, float> rollChances);
    void PushPicked(CJS_ChoiceData picked);
}

public interface CJS_IChoiceReroller : CJS_IChoiceRoller
{
    List<CJS_ChoiceData> Reroll3(IReadOnlyCollection<int> previousIds,
        out Dictionary<CJS_ChoiceData, float> rollChances);
}