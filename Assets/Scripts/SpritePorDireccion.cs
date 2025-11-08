/*
    sprite_por_direccion.cs
    
    cambia el sprite mostrado en funcion de ent.dir_mov: izquierda (-1,0), derecha (1,0), arriba (0,1), abajo (0,-1)

    si dir_mov es (0,0) usa el sprite de abajo por defecto

    no toca color ni materiales (compatible con parpadeo color)
*/


using UnityEngine;

[RequireComponent(typeof(Entidad))]
public class SpritePorDireccion : MonoBehaviour
{
    public SpriteRenderer sr;         //si esta vacio lo busco automaticamente
    public Sprite sprite_abajo;
    public Sprite sprite_derecha;
    public Sprite sprite_izquierda;
    public Sprite sprite_arriba;

    Entidad ent;
    Vector2Int ultima_dir;

    void Awake()
    {
        ent = GetComponent<Entidad>();
        if (!sr) sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

        refrescar_sprite();   //el inicial
    }

    void LateUpdate()
    {
        if (!ent || !sr) return;

        // si es texto no toco el sprite
        if (ent.es_texto || Entidad.es_texto_objeto(ent.objeto))
        {
            sr.flipX = false;
            return;
        }

        refrescar_sprite();
    }

    void refrescar_sprite()
    {
        ultima_dir = ent.dir_mov;
        var d = ultima_dir;

        // prioridad horizontal, si x es 0 usa vertical
        if (d.x < 0 && sprite_izquierda)
        {
            if (sr.sprite != sprite_izquierda) sr.sprite = sprite_izquierda;
        }
        else if (d.x > 0 && sprite_derecha)
        {
            if (sr.sprite != sprite_derecha) sr.sprite = sprite_derecha;
        }
        else if (d.y > 0 && sprite_arriba)
        {
            if (sr.sprite != sprite_arriba) sr.sprite = sprite_arriba;
        }
        else
        {
            if (sprite_abajo && sr.sprite != sprite_abajo) sr.sprite = sprite_abajo; // d.y < 0 o (0,0)
        }
    }



    //incidencia sprite movimiento
    public void ForzarRefrescoInmediato()
    {
        sr.sprite = sprite_abajo;
    }
    //


}
