/*
    iniciadorescena.cs
    
    recoge las entidades colocadas en la escena y las registra en el tablero

    asegura que la rejilla del tablero esta inicializada

    recalcula reglas al inicio

    ajusta la camara y el fondo (si existen sus componentes)

    flujo:
    
    1-valida referencias y crea el grid
    2-comp entidades activas sobre el tablero y las añade en su celda
    3-recalcular reglas
    4-ajusta la camara y el fondo

*/



using UnityEngine;



public class IniciadorEscena : MonoBehaviour
{
    public Tablero tablero;
    public Reglas reglas;

    void Start()
    {
        // comprobaciones
        if (tablero == null || reglas == null)
        {
            Debug.LogError("[iniciador] falta referencia a tablero o reglas"); //sin las referencias obligatorias doy mensaje
            return;
        }

        // me aseguro que la rejilla esta creada
        if (tablero.rejilla == null)
            tablero.iniciar(tablero.ancho, tablero.alto);

        // recolecto todas las entidades que esten en la jerarquia (hijas de tablero)
        var entidades = tablero.GetComponentsInChildren<Entidad>(includeInactive: false);

        // recorro cada entidad encontrada para registrarla en la rejilla del tablero
        foreach (var e in entidades)
        {
            // uso de posicion local para mapear celdas del tablero
            var p = e.transform.localPosition;

            // importante, redondeo a enteros para obtener coordenadas de celda válidas
            var celda = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));

            //si esta fuera de los límites del tablero, ignoro la entidad y aviso
            if (!tablero.en_rango(celda))
            {
                Debug.LogWarning($"[iniciador] {e.name} esta fuera del tablero en {celda}. se ignora.");
                continue;
            }

            // registrar en la rejilla y fijar su posicion exacta
            tablero.agregar(e, celda);
        }

        // primer recalculo de reglas
        reglas.recalcular_propiedades();

        // ajustar la camara si hay componente de ajuste
        var camara = Camera.main;
        if (camara != null)
        {
            var ajuste = camara.GetComponent<CamaraAjusteTablero>();
            if (ajuste != null) ajuste.AjustarAhora(); // recalculo
        }

        // refrescar el fondo si existe
        var fondo = FindFirstObjectByType<FondoTablero>();
        if (fondo != null) fondo.Refrescar();
    }
}
