#ifndef BALL_A_RETRO_EFFECT_TIME_INCLUDED
#define BALL_A_RETRO_EFFECT_TIME_INCLUDED
float _BallARetroReduceEffects;

void BallARetroEffectTime_float(out float Time, out float SineTime, out float CosineTime,
    out float DeltaTime, out float SmoothDelta)
{
    float running = 1.0 - step(0.5, _BallARetroReduceEffects);
    Time = _TimeParameters.x * running;
    SineTime = sin(Time);
    CosineTime = cos(Time);
    DeltaTime = unity_DeltaTime.x * running;
    SmoothDelta = unity_DeltaTime.z * running;
}

void BallARetroEffectTime_half(out half Time, out half SineTime, out half CosineTime,
    out half DeltaTime, out half SmoothDelta)
{
    float t, s, c, d, sd;
    BallARetroEffectTime_float(t, s, c, d, sd);
    Time = t; SineTime = s; CosineTime = c; DeltaTime = d; SmoothDelta = sd;
}
#endif
