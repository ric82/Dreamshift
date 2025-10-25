/*
    baseapariencias.cs
    
     mantiene un mapa en memoria de objeto -> sprite para consultas

     se rellena un array serializado de objeto -> sprite
     la idea es que se pueda asociar facilmente desde el inspector

     lo usaré, por ejemplo, cuando Reglas haga transformaciones y se tenga
     que actualizar el sprite visual de la entidad

     ojo, si hay entradas duplicadas en el array la última gana,
     ya que el diccionario se sobrescribe con la misma clave

*/

using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public struct ObjetoSprite
{
    public Objeto objeto;   //clave logica
    public Sprite sprite;   // sprite asociado
}

public class BaseApariencias : MonoBehaviour
{
    public ObjetoSprite[] entradas;          // pares configurables en el inspector
    Dictionary<Objeto, Sprite> mapa;         //diccionario de consulta

    void Awake()
    {
        // construye el diccionario si aun no existe
        if (mapa == null)
        {
            mapa = new Dictionary<Objeto, Sprite>(); // creo el contenedor de mapeos

            //recorro todas las entradas configuradas en el Inspector
            foreach (var e in entradas)
                mapa[e.objeto] = e.sprite;   //la ultima entrada con la misma clave prevalece
        }
    }

    // devuelve el sprite asociado al objeto o null si no hay entrada
    public Sprite obtener(Objeto o)
    {
        if (mapa == null) Awake();           //inicializacion por si alguien llama antes de awake
        mapa.TryGetValue(o, out var s);
        return s;
    }
}
