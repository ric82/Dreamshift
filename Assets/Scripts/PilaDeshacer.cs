/*
    piladeshacer.cs
    
    guarda instantaneas completas (snapshots) del tablero (ancho, alto y todas las entidades con su tipo, texto y direccion de movimiento)

    permite restaurar exactamente el estado anterior (pop), reinstanciando los prefabs correctos, es decir,
    al hacer Pop() se destruye el estado actual y se reconstruye exactamente el
    que había en el snapshot superior de la pila utilizando los prefabs

    los campos de prefabs deben estar completos para todos los objetos y textos
*/

using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;



public class PilaDeshacer : MonoBehaviour
{
    [Header("refs")]
    public Tablero tablero;

    // AÑADIR cuando se cree un objeto
    // usa los mismos prefabs que el cargador de niveles
    [Header("prefabs para reconstruir - objetos")]
    public Entidad prefab_lu, prefab_TRONCO, prefab_cohete, prefab_VAMPI, prefab_caja,
                   prefab_BICHO, prefab_OVNI, prefab_lodo, prefab_ESPEJO, prefab_ASTRO,
                   prefab_AGUA, prefab_ARU, prefab_PEZ, prefab_GATO;

    // AÑADIR cuando se cree un objeto
    [Header("prefabs para reconstruir - texto")]
    public Entidad texto_lu, texto_TRONCO, texto_cohete, texto_VAMPI, texto_igual, texto_tu, texto_empuja,
                   texto_meta, texto_vence, texto_para, texto_caja, texto_y, texto_no, texto_vuela, texto_hunde,
                   texto_quema, texto_funde, texto_rojo, texto_azul, texto_mueve, texto_guarda, texto_abre,
                   texto_cierra, texto_tunel, texto_texto, texto_BICHO, texto_OVNI, texto_lodo, texto_ESPEJO,
                   texto_ASTRO, texto_AGUA, texto_ARU, texto_PEZ, texto_GATO;

    //estado serializado
    [System.Serializable]
    private class Snap
    {
        public int w, h;            // dimensiones del tablero
        public List<S> ents = new List<S>(); // lista de entidades serializadas
    }

    [System.Serializable]
    private class S
    {
        public Objeto objeto;   // tipo de entidad
        public bool es_texto;   // true si es bloque de texto
        public int x, y;        //posición en celdas dentro del tablero

        // mueve, orientacion guardada
        public int mdx, mdy;    // moveDir en el momento del snapshot
    }

    private Stack<Snap> pila = new Stack<Snap>();

    // 
    public void Clear()
    {
        pila.Clear(); // vacia la pila de snapshots
    }

    public void Push()
    {
        if (tablero == null || tablero.rejilla == null) return;

        var snap = new Snap { w = tablero.ancho, h = tablero.alto };

        for (int x = 0; x < tablero.ancho; x++)
            for (int y = 0; y < tablero.alto; y++)
                foreach (var e in tablero.rejilla[x, y])
                    snap.ents.Add(new S
                    {
                        objeto = e.objeto, // guarda el objeto exacto
                        es_texto = e.es_texto, // guarda si es texto
                        x = x, // guarda la celda x
                        y = y, // y la celda y
                        // mueve, guarda la direccion actual
                        mdx = e.dir_mov.x,
                        mdy = e.dir_mov.y
                    });

        pila.Push(snap); // empujo el snapshot a la pila
    }

    public void Pop()
    {
        if (pila.Count == 0 || tablero == null) return;
        var s = pila.Pop();

        //1-destruir todo lo actual del tablero
        for (int x = 0; x < tablero.ancho; x++)
            for (int y = 0; y < tablero.alto; y++)
            {
                var copia = new List<Entidad>(tablero.rejilla[x, y]);
                foreach (var e in copia)
                    if (e) Destroy(e.gameObject);
                tablero.rejilla[x, y].Clear();
            }

        // 2-reinicializar rejilla al tamaño de la instantanea
        tablero.iniciar(s.w, s.h);

        // 3-reinstanciar todas las entidades (prefabs correspondientes)
        foreach (var st in s.ents)
        {
            var prefab = obtener_prefab(st.objeto, st.es_texto);
            if (prefab == null)
            {
                Debug.LogWarning($"[pila deshacer] falta prefab para {st.objeto} (es_texto={st.es_texto}).");
                continue; // si no hay prefab, no se puede restaurar esta entidad, doy mensaje
            }
            // instancia la entidad como hijo del tablero (para mantener el orden jerárquico esperado)
            var e = Instantiate(prefab, tablero.transform);
            e.objeto = st.objeto; // tipo
            e.es_texto = st.es_texto; // y si es texto

            // mueve: restaura orientacion y evita que start la sobreescriba 
            e.dir_mov = new Vector2Int(st.mdx, st.mdy);
            e.dir_mov_desde_rotacion = false;

            // coloca la entidad en la celda adecuada y actualiza su transform.localPosition
            tablero.agregar(e, new Vector2Int(st.x, st.y));
        }
    }

    // funciones auxiliares
    private Entidad obtener_prefab(Objeto k, bool es_texto)
    {
        // selecciono el prefab según si es un bloque de texto o un objeto
        if (es_texto)
        {
            // mapeo para TEXT_*
            switch (k)
            {
                // AÑADIR cuando se cree un objeto
                case Objeto.TEXTO_LU: return texto_lu;
                case Objeto.TEXTO_TRONCO: return texto_TRONCO;
                case Objeto.TEXTO_COHETE: return texto_cohete;
                case Objeto.TEXTO_VAMPI: return texto_VAMPI;
                case Objeto.TEXTO_IGUAL: return texto_igual;
                case Objeto.TEXTO_TU: return texto_tu;
                case Objeto.TEXTO_EMPUJA: return texto_empuja;
                case Objeto.TEXTO_META: return texto_meta;
                case Objeto.TEXTO_VENCE: return texto_vence;
                case Objeto.TEXTO_PARA: return texto_para;
                case Objeto.TEXTO_CAJA: return texto_caja;
                case Objeto.TEXTO_Y: return texto_y;
                case Objeto.TEXTO_NO: return texto_no;

                case Objeto.TEXTO_VUELA: return texto_vuela;
                case Objeto.TEXTO_HUNDE: return texto_hunde;
                case Objeto.TEXTO_QUEMA: return texto_quema;
                case Objeto.TEXTO_FUNDE: return texto_funde;

                case Objeto.TEXTO_ROJO: return texto_rojo;
                case Objeto.TEXTO_AZUL: return texto_azul;
                case Objeto.TEXTO_MUEVE: return texto_mueve;

                case Objeto.TEXTO_GUARDA: return texto_guarda;
                case Objeto.TEXTO_ABRE: return texto_abre;
                case Objeto.TEXTO_CIERRA: return texto_cierra;
                case Objeto.TEXTO_TUNEL: return texto_tunel;
                case Objeto.TEXTO_TEXTO: return texto_texto;

                case Objeto.TEXTO_BICHO: return texto_BICHO;
                case Objeto.TEXTO_OVNI: return texto_OVNI;
                case Objeto.TEXTO_LODO: return texto_lodo;
                case Objeto.TEXTO_ESPEJO: return texto_ESPEJO;
                case Objeto.TEXTO_ASTRO: return texto_ASTRO;
                case Objeto.TEXTO_AGUA: return texto_AGUA;
                case Objeto.TEXTO_ARU: return texto_ARU;
                case Objeto.TEXTO_PEZ: return texto_PEZ;
                case Objeto.TEXTO_GATO: return texto_GATO;

                default: return null; // si el tipo no está mapeado no hay prefab asignado
            }
        }
        else
        {
            // mapeo para objetos
            switch (k)
            {
                // AÑADIR cuando se cree un objeto
                case Objeto.LU: return prefab_lu;
                case Objeto.TRONCO: return prefab_TRONCO;
                case Objeto.COHETE: return prefab_cohete;
                case Objeto.VAMPI: return prefab_VAMPI;
                case Objeto.CAJA: return prefab_caja;

                case Objeto.BICHO: return prefab_BICHO;
                case Objeto.OVNI: return prefab_OVNI;
                case Objeto.LODO: return prefab_lodo;
                case Objeto.ESPEJO: return prefab_ESPEJO;
                case Objeto.ASTRO: return prefab_ASTRO;
                case Objeto.AGUA: return prefab_AGUA;
                case Objeto.ARU: return prefab_ARU;

                case Objeto.PEZ: return prefab_PEZ;
                case Objeto.GATO: return prefab_GATO;

                default: return null; // sin mapeo no hay prefab
            }
        }
    }
}
