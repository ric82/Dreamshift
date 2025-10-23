/*
    tablero.cs
    
    mantiene una rejilla 2d de listas de entidades

    operaciones basicas: iniciar el tablero, comprobar limites, agregar y mover entidades

    todas las posiciones son enteras en coordenadas locales (x,y) respecto al propio tablero

    cada celda contiene una lista de entidades (List<Entidad>) porque varias entidades pueden
    estar en la misma posición
*/


using System.Collections.Generic;
using UnityEngine;



public class Tablero : MonoBehaviour
{
    // dimensiones del tablero en celdas, se fijará de forma definitiva en edición y dependerá del nivel
    public int ancho = 28, alto = 16;

    // rejilla de celdas, cada celda contiene una lista de entidades apiladas
    public List<Entidad>[,] rejilla;

    // inicializa la rejilla con el tamaño indicado y listas vacias por celda
    public void iniciar(int w, int h)
    {
        ancho = w;
        alto = h;

        rejilla = new List<Entidad>[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                rejilla[x, y] = new List<Entidad>();
    }

    // true si la celda esta dentro de los limites del tablero
    public bool en_rango(Vector2Int celda) =>
        celda.x >= 0 && celda.x < ancho && celda.y >= 0 && celda.y < alto;

    // agrega una entidad a la celda dada y actualiza su posicion local
    public void agregar(Entidad e, Vector2Int celda)
    {
        e.celda = celda;
        rejilla[celda.x, celda.y].Add(e);
        e.transform.localPosition = new Vector3(celda.x, celda.y, 0f); //posición lógica en el mundo (z=0 por defecto)
    }

    // mueve una entidad a la celda destino si esta en rango, actualiza rejilla y transform
    public void mover_entidad(Entidad e, Vector2Int destino)
    {
        if (!en_rango(destino)) return; // seguridad (no mueve si el destino es invalido)

        rejilla[e.celda.x, e.celda.y].Remove(e); // quita la entidad de la lista de su celda actual
        e.celda = destino; // actualiza su posición a la celda de destino
        rejilla[destino.x, destino.y].Add(e); // inserta la entidad en la lista de la nueva celda
        e.transform.localPosition = new Vector3(destino.x, destino.y, 0f); //actualizo la posición visual
    }
}
