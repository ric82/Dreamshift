using UnityEngine;

public static class EstadoRetornoHub
{
    public static string escena_hub = null;
    public static Vector2Int celda = new Vector2Int(-999, -999);
    public static Vector2Int dir = Vector2Int.down;

    public static bool hay_estado => !string.IsNullOrEmpty(escena_hub) && celda.x > -900;

    public static void guardar(string nombre_escena_hub, Vector2Int celda_hub, Vector2Int dir_hub)
    {
        escena_hub = nombre_escena_hub;
        celda = celda_hub;
        dir = dir_hub;
    }

    public static void limpiar()
    {
        escena_hub = null;
        celda = new Vector2Int(-999, -999);
        dir = Vector2Int.down;
    }
}
