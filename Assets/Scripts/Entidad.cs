/*
    entidad.cs
    
    enum objeto: todos los objetos del juego (y sus bloques de texto)

    enum propiedad: banderas combinables para las reglas

    clase entidad: componente base con tipo, celda, flag de texto y config de movimiento

    importante, el orden de los objetos no debe modificarse para no romper indices, es decir, de agregar tendrá que ser al final
*/

using UnityEngine;


public enum Objeto
{
    //AÑADIR cuando se cree un objeto
    // objetos base
    LU, TRONCO, COHETE, VAMPI, CAJA, BICHO, OVNI, LODO, ESPEJO, ASTRO, AGUA, ARU, PEZ, GATO,

    // AÑADIR cuando se cree un objeto
    // bloques de texto
    TEXTO_LU, TEXTO_TRONCO, TEXTO_COHETE, TEXTO_VAMPI, TEXTO_IGUAL, TEXTO_TU, TEXTO_EMPUJA, TEXTO_META, TEXTO_VENCE, TEXTO_PARA, TEXTO_CAJA, TEXTO_Y,
    TEXTO_NO, TEXTO_VUELA, TEXTO_HUNDE, TEXTO_QUEMA, TEXTO_FUNDE, TEXTO_ROJO, TEXTO_AZUL, TEXTO_MUEVE, TEXTO_GUARDA, TEXTO_ABRE, TEXTO_CIERRA, TEXTO_TUNEL,
    TEXTO_TEXTO, TEXTO_BICHO, TEXTO_OVNI, TEXTO_LODO, TEXTO_ESPEJO, TEXTO_ASTRO, TEXTO_AGUA, TEXTO_ARU, TEXTO_PEZ, TEXTO_GATO
    
}

[System.Flags]
public enum Propiedad
{
    // AÑADIR cuando se cree una propiedad
    NINGUNA = 0,
    TU = 1, //controlado por el jugador
    EMPUJA = 2, //puede ser empujado (y empuja en cadena cuando aplique)
    META = 4, //gana al coincidir con TU en la misma celda/capa
    VENCE = 8, //mata a TU al compartir celda/capa
    PARA = 16, //bloquea el movimiento (incluye VUELA)
    VUELA = 32, //capa superior (se interactua en misma capa, salvo alguna excepción como PARA)
    HUNDE = 64, //se hunde junto al otro objeto en la misma capa (ambos destruidos)
    QUEMA = 128, //destruye a FUNDE en la misma capa
    FUNDE = 256, //se destruye con QUEMA en la misma capa
    ROJO = 512, //pintura roja (permite parpadeo)
    AZUL = 1024, //pintura azul (permite parpadeo)
    MUEVE = 2048, //se mueve automaticamente
    ABRE = 4096, //abre (se destruye con CIERRA en la misma celda/capa)
    CIERRA = 8192, //bloquea (se destruye con ABRE en la misma celda/capa)
    TUNEL = 16384 // teletransporte al mismo objeto (en misma capa, aleatorio si hay mas de uno)
}

public class Entidad : MonoBehaviour
{
    // tipo logico de la entidad (objeto o bloque de texto)
    public Objeto objeto;

    // coordenada de celda en el tablero
    public Vector2Int celda;

    //true si representa un bloque de texto (prefijo TEXTO_)
    public bool es_texto;

    [Header("mueve (config)")]
    // direccion de movimiento por defecto para piezas con propiedad mueve
    public Vector2Int dir_mov = Vector2Int.down;

    // si es true, la direccion se toma de la rotacion z al iniciar
    public bool dir_mov_desde_rotacion = false;

    // cache del spriterenderer para cambios visuales
    SpriteRenderer _sr;

    void Awake()
    {
        // busca un spriterenderer en este objeto o en hijos
        _sr = GetComponent<SpriteRenderer>();
        if (!_sr) _sr = GetComponentInChildren<SpriteRenderer>();
    }

    //cambia el tipo logico y actualiza el sprite si se proporciona
    public void establecer_objeto(Objeto nuevo_objeto, Sprite sprite_si_hay)
    {
        objeto = nuevo_objeto;
        es_texto = es_texto_objeto(nuevo_objeto); //mantiene coherencia si se transforma a texto
        if (_sr && sprite_si_hay) _sr.sprite = sprite_si_hay;
    }

    // true si el objeto es un bloque de texto
    public static bool es_texto_objeto(Objeto o)
    {
        // AÑADIR cuando se cree un objeto
        return
            o == Objeto.TEXTO_LU ||
            o == Objeto.TEXTO_TRONCO ||
            o == Objeto.TEXTO_COHETE ||
            o == Objeto.TEXTO_VAMPI ||
            o == Objeto.TEXTO_IGUAL ||
            o == Objeto.TEXTO_TU ||
            o == Objeto.TEXTO_EMPUJA ||
            o == Objeto.TEXTO_META ||
            o == Objeto.TEXTO_VENCE ||
            o == Objeto.TEXTO_PARA ||
            o == Objeto.TEXTO_CAJA ||
            o == Objeto.TEXTO_Y ||
            o == Objeto.TEXTO_NO ||
            o == Objeto.TEXTO_VUELA ||
            o == Objeto.TEXTO_HUNDE ||
            o == Objeto.TEXTO_QUEMA ||
            o == Objeto.TEXTO_FUNDE ||
            o == Objeto.TEXTO_ROJO ||
            o == Objeto.TEXTO_AZUL ||
            o == Objeto.TEXTO_MUEVE ||
            o == Objeto.TEXTO_GUARDA ||
            o == Objeto.TEXTO_ABRE ||
            o == Objeto.TEXTO_CIERRA ||
            o == Objeto.TEXTO_TUNEL ||
            o == Objeto.TEXTO_TEXTO ||
            o == Objeto.TEXTO_BICHO ||
            o == Objeto.TEXTO_OVNI ||
            o == Objeto.TEXTO_LODO ||
            o == Objeto.TEXTO_ESPEJO ||
            o == Objeto.TEXTO_ASTRO ||
            o == Objeto.TEXTO_AGUA ||
            o == Objeto.TEXTO_ARU ||
            o == Objeto.TEXTO_PEZ ||
            o == Objeto.TEXTO_GATO;
    }
}
