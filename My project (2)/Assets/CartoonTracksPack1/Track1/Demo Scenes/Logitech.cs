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
    public static bool Ligado()
    {
        return LogitechGSDK.LogiUpdate() &&
            LogitechGSDK.LogiIsConnected(0);
    }

    public static float RotacaoVolante()
    {
        DIJOYSTATE2ENGINES estado = ObterEstado();

        // lX varia de -32768 a 32767
        return estado.lX / 32767f;
    }

    private static DIJOYSTATE2ENGINES ObterEstado() 
    {
        return LogitechGSDK.LogiGetStateCSharp(0);
    }

}