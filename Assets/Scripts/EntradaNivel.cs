using UnityEngine;

public class EntradaNivel : MonoBehaviour
{
    [Header("escena objetivo del build")]
    public string escena_objetivo;

    // si este portal es una entidad del tablero se detecta por celda
    public Entidad entidad;

    void Awake()
    {
        if (!entidad) entidad = GetComponent<Entidad>();
    }
}
