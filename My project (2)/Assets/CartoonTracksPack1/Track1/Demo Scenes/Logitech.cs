using UnityEngine;
using static LogitechGSDK;

public class Logitech
{
    public static bool Ligar()
    {
        return LogitechGSDK.LogiSteeringInitialize(false);
    }
    public static void Desligar()
    {
        LogitechGSDK.LogiSteeringShutdown();
    }
    public static bool IsLigado()
    {
        return LogitechGSDK.LogiUpdate() &&
            LogitechGSDK.LogiIsConnected(0);
    }

    public static float RotacaoVolante(float anguloMaximo)
    {
        DIJOYSTATE2ENGINES estado = ObterEstado();

        // lX varia de -32768 a 32767
        return -(estado.lX * anguloMaximo / 32767f);
    }

    private static DIJOYSTATE2ENGINES ObterEstado() 
    {
        return LogitechGSDK.LogiGetStateCSharp(0);


    }

    public static float Acelerador()
    {
        DIJOYSTATE2ENGINES estado = ObterEstado();
      
        // eixo varia de -32768 a 32767
        return (32767f - estado.lY) / 65535f;
    }

    public static float Embreagem()
    {
        DIJOYSTATE2ENGINES estado = ObterEstado();

        return (32767f - estado.rglSlider[0]) / 65535f;
    }
}