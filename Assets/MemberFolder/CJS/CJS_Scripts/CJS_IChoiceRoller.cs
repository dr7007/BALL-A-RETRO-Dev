using System.Collections.Generic;

public interface CJS_IChoiceRoller
{
    List<CJS_ChoiceData> Roll3();
    void PushPicked(CJS_ChoiceData picked);
}
