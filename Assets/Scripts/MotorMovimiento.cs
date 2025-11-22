/*
    motormovimiento.cs
    
    intentar mover entidades en el tablero respetando reglas de STOP, PUSH, FLOAT,
    y la excepcion de OPEN/SHUT que permite entrar en celdas bloqueadas para
    provocar su destruccion en la fase de colisiones

    gestiona cadenas de empuje de objetos con propiedad PUSH

    actualiza moveDir de jugador (TU) y de los objetos empujados

    mantiene el orden de dibujo por celda (objetos debajo, textos encima, el que llega queda arriba en su grupo)

    ojo, deciones importantes:

    STOP bloquea siempre, sin importar la capa FLOAT

    la unica excepcion al bloqueo es si el jugador o la cabeza de una cadena PUSH
    forman una pareja OPEN/SHUT con algo en destino en la misma capa

    la cadena PUSH se construye recorriendo celdas consecutivas con al menos un
    objeto PUSH, y luego se empuja de cola a cabeza para no pisar posiciones

    al empujar o mover, se actualiza moveDir del objeto afectado para que el
    sistema de MOVE pueda usar esa direccion coherentemente mas adelante
    y para pintar su sprite correspondiente, si es que tiene varios (por posición)

    por decisión de diseño decido que las muertes y victorias se traten en una clase diferente (VictoriaDerrota.cs)
    y el recalculo de reglas también (se hará en Reglas.cs), por tanto aquí encapsulo el tratamiento del movimiento

*/

using System.Collections.Generic;
using UnityEngine;


public class MotorMovimiento : MonoBehaviour
{
    public Tablero tablero; // referencia al tablero para consultar celdas y mover entidades
    public Reglas reglas; //referencia al sistema de reglas para consultar propiedades

    // func de propiedades segun el tipo de la entidad
    private Propiedad propiedades_de(Entidad e)
    {
        var p = Propiedad.NINGUNA;

        if (reglas != null && reglas.propiedades != null && reglas.propiedades.TryGetValue(e.objeto, out var pr))
            p = pr;

        //todo bloque de texto es empuja por defecto
        if (e.es_texto) p |= Propiedad.EMPUJA;

        return p;
    }

    //comprobación de las propiedades de una entidad

    private bool es_para(Entidad e) => (propiedades_de(e) & Propiedad.PARA) != 0;
    private bool es_empuja(Entidad e) => (propiedades_de(e) & Propiedad.EMPUJA) != 0;
    private bool es_vuela(Entidad e) => (propiedades_de(e) & Propiedad.VUELA) != 0;

    // abre/cierra y capas
    private bool es_abre(Entidad e) => (propiedades_de(e) & Propiedad.ABRE) != 0;
    private bool es_cierra(Entidad e) => (propiedades_de(e) & Propiedad.CIERRA) != 0;

    private bool misma_capa(Entidad a, Entidad b) => es_vuela(a) == es_vuela(b);

    private bool puede_desbloquear_par(Entidad a, Entidad b)
    {
        // hay pareja valida si uno tiene abre y el otro cierra (en cualquier orden)
        return (es_abre(a) && es_cierra(b)) || (es_cierra(a) && es_abre(b));
    }

    // true si el "movedor" puede entrar en la celda destino porque hay una pareja abre/cierra en su misma capa
    private bool puede_desbloquear_entrada(Entidad movedor, List<Entidad> destino)
    {
        for (int i = 0; i < destino.Count; i++)
        {
            var o = destino[i];
            if (!misma_capa(movedor, o)) continue;
            if (puede_desbloquear_par(movedor, o)) return true;
        }
        return false;
    }

    // intento de movimiento principal
    // marca en 'movidos' al jugador y a los empujados para no moverlos dos veces en el mismo subpaso
    public bool intentar_mover(Entidad e, Vector2Int dir, HashSet<Entidad> movidos = null)
    {
        var origen = e.celda; // guardo la celda origen antes de mover
        var destino = e.celda + dir; //calculo la celda destino (sumando la direccion)

        //si la celda destino esta fuera de limites, no se mueve
        if (!tablero.en_rango(destino)) return false;

        // se obtiene la lista de entidades actuales en la celda destino
        var lista_destino = tablero.rejilla[destino.x, destino.y];

        // si hay algo empujable en destino, intentamos empujar la cadena
        // si no hay algo empujable, PARA bloquea siempre (independiente de capa) salvo pareja abre/cierra en misma capa
        bool hay_empujable = hay_alguno_empujable(lista_destino);
        if (!hay_empujable)
        {
            bool hay_para = false;
            for (int i = 0; i < lista_destino.Count; i++)
            {
                var o = lista_destino[i];
                if (es_para(o)) { hay_para = true; break; } // sin filtrar por capa
            }
            if (hay_para && !puede_desbloquear_entrada(e, lista_destino))
                return false;
        }
        else
        {
            if (!intentar_empujar_cadena(destino, dir, movidos))
                return false; // no se pudo empujar la cadena
        }

        // si llego aqui, el movimiento es valido, muevo la entidad al destino
        tablero.mover_entidad(e, destino); // actualiza grid
        e.dir_mov = dir;         // actualiza direccion al moverse
        movidos?.Add(e);         // marca como movido en este subpaso

        // refresco orden de dibujo en las dos celdas afectadas
        refrescar_orden_para_celda(origen, null);   //el que se va ya no esta (no debe contar arriba)
        refrescar_orden_para_celda(destino, e);     // el que llega queda arriba en su grupo

        Sonidos.instancia.reproducir_mover(); //sonido de movimiento

        return true; //movimiento exitoso
    }

    // devuelve true si en la lista de la celda hay al menos una entidad PUSH
    private bool hay_alguno_empujable(List<Entidad> lista)
    {
        for (int i = 0; i < lista.Count; i++)
            if (es_empuja(lista[i])) return true;
        return false;
    }

    // empuja una cadena contigua de objetos empujables hacia 'dir'
    //no filtramos por capas dentro de la cadena, si un objeto es empuja y para, sigue empujandose
    private bool intentar_empujar_cadena(Vector2Int inicio, Vector2Int dir, HashSet<Entidad> movidos)
    {
        var celdas_cadena = new List<Vector2Int>(); //lista de celdas consecutivas que forman la cadena
        var cur = inicio;

        //construye la cadena: celdas consecutivas que contienen al menos un empujable
        while (true)
        {
            if (!tablero.en_rango(cur)) return false;

            var celda = tablero.rejilla[cur.x, cur.y];

            bool hay_empujable = false;
            for (int i = 0; i < celda.Count; i++)
                if (es_empuja(celda[i])) { hay_empujable = true; break; }

            if (!hay_empujable) break;

            celdas_cadena.Add(cur);
            cur += dir;
        }

        // la celda siguiente a la ultima de la cadena debe poder aceptar el empuje
        if (!tablero.en_rango(cur)) return false;

        // prepara la lista de objetos PUSH que hay en la cabeza de la cadena
        var cabeza_empujables = new List<Entidad>();
        if (celdas_cadena.Count > 0)
        {
            var celda_cabeza = celdas_cadena[celdas_cadena.Count - 1];
            var lista_cabeza = tablero.rejilla[celda_cabeza.x, celda_cabeza.y];
            for (int i = 0; i < lista_cabeza.Count; i++)
                if (es_empuja(lista_cabeza[i])) cabeza_empujables.Add(lista_cabeza[i]);
        }

        // evaluo bloqueo por para en el destino y posible desbloqueo por abre/cierra en la misma capa
        bool para_en_destino = false, puede_desbloquear = false;
        var lista_destino = tablero.rejilla[cur.x, cur.y];

        for (int i = 0; i < lista_destino.Count; i++)
        {
            var o = lista_destino[i];

            if (es_para(o))
            {
                // para bloquea aunque este en otra capa
                para_en_destino = true;
            }

            // compruebo si alguna cabeza forma pareja abre/cierra con 'o' en la misma capa
            for (int h = 0; h < cabeza_empujables.Count; h++)
                if (misma_capa(cabeza_empujables[h], o) && puede_desbloquear_par(cabeza_empujables[h], o))
                { puede_desbloquear = true; break; }
        }

        if (para_en_destino && !puede_desbloquear) return false;

        // empuja de cola a cabeza para no pisar posiciones durante el desplazamiento
        for (int i = celdas_cadena.Count - 1; i >= 0; i--)
        {
            var desde = celdas_cadena[i];
            var hasta = desde + dir;

            var copia = new List<Entidad>(tablero.rejilla[desde.x, desde.y]);
            for (int j = 0; j < copia.Count; j++)
            {
                var o = copia[j];
                if (es_empuja(o))
                {
                    tablero.mover_entidad(o, hasta);
                    o.dir_mov = dir;     // al ser empujado, adopta la direccion del empuje
                    movidos?.Add(o);   //marco el objeto como movido en este subpaso
                }
            }

            // refresca orden de dibujo de las dos celdas
            refrescar_orden_para_celda(desde, null);
            refrescar_orden_para_celda(hasta, null);
        }

        return true; //cadena empujada con exito
    }

    // reorden de spriterenderer en una celda:
    // objetos (no texto) debajo
    // textos encima
    //si e_movida != null y esta en la celda, queda el ultimo de su grupo
    void refrescar_orden_para_celda(Vector2Int celda, Entidad e_movida)
    {
        //si la celda no existe, nada que ordenar
        if (!tablero.en_rango(celda)) return;
        // obtengo lista de entidades en la celda
        var lista = tablero.rejilla[celda.x, celda.y];
        // si no hay entidades, nada que hacer
        if (lista == null || lista.Count == 0) return;

        // corrijo error para evitar overflow de sortingOrder
        int max_seguro = 32000;
        int h = Mathf.Max(1, tablero.alto);
        int por_fila = Mathf.Max(8, max_seguro / h);

        int base_y = celda.y * por_fila;
        int offset_texto = por_fila / 2; // los textos por encima de los objetos

        int orden = base_y;

        // primera pasada, objetos que NO son texto
        for (int i = 0; i < lista.Count; i++)
        {
            var o = lista[i];
            if (o.es_texto) continue;
            if (e_movida != null && o == e_movida) continue; // pospone el que llega
            asignar_orden(o, orden++);
        }
        if (e_movida != null && !e_movida.es_texto && lista.Contains(e_movida))
            asignar_orden(e_movida, orden++); // el recien llegado va arriba dentro del grupo de objetos

        //segunda pasada, textos por encima
        orden = base_y + offset_texto;
        for (int i = 0; i < lista.Count; i++)
        {
            var o = lista[i];
            if (!o.es_texto) continue;
            if (e_movida != null && o == e_movida) continue;
            asignar_orden(o, orden++);
        }
        if (e_movida != null && e_movida.es_texto && lista.Contains(e_movida))
            asignar_orden(e_movida, orden++); //el texto recien llegado queda arriba dentro del grupo de textos
    }

    // asigna el sortingOrder al SpriteRenderer principal o al de un hijo
    void asignar_orden(Entidad e, int sorting_order)
    {
        // busco un SpriteRenderer en el propio GameObject o en sus hijos
        var sr = e.GetComponent<SpriteRenderer>() ?? e.GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.sortingOrder = sorting_order; //si hay SpriteRenderer aplica el sortingOrder calculado
    }
}
