/*
    victoriaderrota.cs
    
    aplica tuneles (teletransporte), colisiones con muerte y condiciones de victoria/derrota

    gestiona los spawns por "guarda" al destruir entidades

    ojo, no decide movimiento, solo evalua el estado del tablero tras cada paso

    decisiones de diseño importantes:

    las interacciones se resuelven por celda y por capa

    orden de resolución: abre/cierra, hunde, quema/funde, vence, y luego se comprueba la victoria/derrota

    TUNEL es estático (los portales no viajan entre ellos mismos)

*/

using System.Collections.Generic;
using UnityEngine;


public class VictoriaDerrota : MonoBehaviour
{
    public Tablero tablero;
    public Reglas reglas;

    // AÑADIR cuando se cree un objeto
    [Header("prefabs para guarda (spawn) — objetos")]
    public Entidad prefab_lu, prefab_TRONCO, prefab_cohete, prefab_VAMPI, prefab_caja,
                   prefab_BICHO, prefab_OVNI, prefab_lodo, prefab_ESPEJO, prefab_ASTRO, prefab_AGUA,
                   prefab_ARU, prefab_PEZ, prefab_GATO; // Prefabs objeto para spawnear cuando GUARDA lo indique

    // AÑADIR cuando se cree un objeto
    [Header("prefabs para guarda (spawn) — texto")]
    public Entidad texto_lu, texto_TRONCO, texto_cohete, texto_VAMPI, texto_igual, texto_tu, texto_empuja,
                   texto_meta, texto_vence, texto_para, texto_caja, texto_y, texto_no, texto_vuela, texto_hunde,
                   texto_quema, texto_funde, texto_rojo, texto_azul, texto_mueve, texto_guarda, texto_abre,
                   texto_cierra, texto_tunel, texto_texto, texto_BICHO, texto_OVNI, texto_lodo, texto_ESPEJO,
                   texto_ASTRO, texto_AGUA, texto_ARU, texto_PEZ, texto_GATO; // Prefabs texto para spawnear cuando GUARDA lo indique

    // func no se excluyen textos, si un texto tiene propiedades, se aplican igual
    private bool tiene(Entidad e, Propiedad p) => reglas.tiene_propiedad(e.objeto, p);
    private bool es_vuela(Entidad e) => reglas.tiene_propiedad(e.objeto, Propiedad.VUELA);
    private bool misma_capa(Entidad a, Entidad b) => es_vuela(a) == es_vuela(b);

    //tuneles (tele)
    // los ESPEJOS con propiedad tunel son estaticos, no viajan
    // cualquier entidad (incluidos textos) que comparta celda con al menos un tunel viaja a otra celda con tunel (no a la misma),
    //respetando la capa (float/no float). correccion: una vez por entidad y turno.
    public void aplicar_tuneles()
    {
        int W = tablero.ancho, H = tablero.alto;

        //recopila celdas que contienen al menos un tunel
        var celdas_tunel = new List<Vector2Int>();
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                var celda = tablero.rejilla[x, y];
                bool hay_tunel = false;
                for (int i = 0; i < celda.Count; i++)
                {
                    var t = celda[i];
                    if (!t.gameObject.activeSelf) continue;
                    if (reglas.tiene_propiedad(t.objeto, Propiedad.TUNEL)) { hay_tunel = true; break; }
                }
                if (hay_tunel) celdas_tunel.Add(new Vector2Int(x, y));
            }

        if (celdas_tunel.Count <= 1) return; // sin otro ESPEJO no hay viaje

        var teletransportadas = new HashSet<Entidad>();

        // por cada celda con tunel, teletransporta a quienes esten dentro (una vez por entidad/turno)
        for (int idx = 0; idx < celdas_tunel.Count; idx++)
        {
            var pos = celdas_tunel[idx];
            var celda = tablero.rejilla[pos.x, pos.y];

            // hago copia porque voy a mover entidades durante la iteracion
            var copia = new List<Entidad>(celda);

            for (int i = 0; i < copia.Count; i++)
            {
                var e = copia[i];
                if (!e.gameObject.activeSelf) continue;
                if (teletransportadas.Contains(e)) continue;

                //los propios ESPEJOS no viajan
                if (reglas.tiene_propiedad(e.objeto, Propiedad.TUNEL)) continue;

                // exigir tunel en la misma capa
                bool hay_tunel_misma_capa = false;
                bool e_vuela = reglas.tiene_propiedad(e.objeto, Propiedad.VUELA);
                for (int j = 0; j < celda.Count; j++)
                {
                    var t = celda[j];
                    if (!t.gameObject.activeSelf) continue;
                    if (!reglas.tiene_propiedad(t.objeto, Propiedad.TUNEL)) continue;
                    bool t_vuela = reglas.tiene_propiedad(t.objeto, Propiedad.VUELA);
                    if (t_vuela == e_vuela) { hay_tunel_misma_capa = true; break; }
                }
                if (!hay_tunel_misma_capa) continue; //si el portal está en otra capa, no teletransporta

                //elegir destino distinto a la celda actual
                int count = celdas_tunel.Count;
                int pick = Random.Range(0, count - 1); // [0, count-2]
                if (pick >= idx) pick++;               // saltar la actual
                var destino = celdas_tunel[pick];

                tablero.mover_entidad(e, destino); // muevo la entidad al destino elegido
                teletransportadas.Add(e); //marco como ya teletransportada este turno
            }
        }
    }

    // proceso abre/cierra, hunde, quema/funde y vence. devuelve cuantas entidades se desactivaron
    public int aplicar_derrota()
    {
        int eliminadas = 0;
        int W = tablero.ancho, H = tablero.alto;

        // abre/cierra (misma capa), mueren en parejas validas
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                var celda = tablero.rejilla[x, y];

                for (int layer = 0; layer < 2; layer++)
                {
                    bool capa_vuela = (layer == 1);

                    // snapshot por capa (incluye textos)
                    var lista = new List<Entidad>();
                    for (int i = 0; i < celda.Count; i++)
                    {
                        var e = celda[i];
                        if (!e.gameObject.activeSelf) continue;
                        if (es_vuela(e) != capa_vuela) continue;
                        lista.Add(e);
                    }

                    // emparejar abre con cierra
                    while (true)
                    {
                        Entidad abre = null, cierra = null;
                        for (int i = 0; i < lista.Count && abre == null; i++)
                            if (tiene(lista[i], Propiedad.ABRE)) abre = lista[i];
                        if (abre == null) break;

                        for (int i = 0; i < lista.Count && cierra == null; i++)
                            if (lista[i] != abre && tiene(lista[i], Propiedad.CIERRA)) cierra = lista[i];
                        if (cierra == null) break;

                        //destruir ambos
                        var c0 = abre.celda;
                        tablero.rejilla[c0.x, c0.y].Remove(abre);
                        abre.gameObject.SetActive(false);
                        abre.celda = new Vector2Int(-999, -999);
                        generar_productos_guarda(abre, c0);
                        eliminadas++;

                        var c1 = cierra.celda;
                        tablero.rejilla[c1.x, c1.y].Remove(cierra);
                        cierra.gameObject.SetActive(false);
                        cierra.celda = new Vector2Int(-999, -999);
                        generar_productos_guarda(cierra, c1);
                        eliminadas++;

                        lista.Remove(abre);
                        lista.Remove(cierra);
                    }
                }
            }

        // hunde (misma capa), si hay un hunde y cualquier otro, ambos desaparecen
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                var celda = tablero.rejilla[x, y];

                for (int layer = 0; layer < 2; layer++)
                {
                    bool capa_vuela = (layer == 1);

                    var lista = new List<Entidad>();
                    for (int i = 0; i < celda.Count; i++)
                    {
                        var e = celda[i];
                        if (!e.gameObject.activeSelf) continue;
                        if (es_vuela(e) != capa_vuela) continue;
                        lista.Add(e);
                    }

                    bool progreso = true;
                    while (progreso)
                    {
                        progreso = false;
                        Entidad hundidor = null, otro = null;

                        for (int i = 0; i < lista.Count; i++)
                            if (tiene(lista[i], Propiedad.HUNDE)) { hundidor = lista[i]; break; }

                        if (hundidor == null) break;

                        for (int i = 0; i < lista.Count; i++)
                            if (lista[i] != hundidor) { otro = lista[i]; break; }

                        if (otro == null) break;

                        var cS = hundidor.celda;
                        tablero.rejilla[cS.x, cS.y].Remove(hundidor);
                        hundidor.gameObject.SetActive(false);
                        hundidor.celda = new Vector2Int(-999, -999);
                        generar_productos_guarda(hundidor, cS);

                        var cO = otro.celda;
                        tablero.rejilla[cO.x, cO.y].Remove(otro);
                        otro.gameObject.SetActive(false);
                        otro.celda = new Vector2Int(-999, -999);
                        generar_productos_guarda(otro, cO);

                        lista.Remove(hundidor);
                        lista.Remove(otro);
                        eliminadas += 2;
                        progreso = true;
                    }
                }
            }

        //quema/funde (misma capa), el que tiene funde desaparece, el que tiene quema permanece
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                var celda = tablero.rejilla[x, y];
                var copia = new List<Entidad>(celda);
                foreach (var e in copia)
                {
                    if (!e.gameObject.activeSelf) continue;

                    bool hay_quema_misma_capa = false;
                    for (int j = 0; j < celda.Count; j++)
                    {
                        var h = celda[j];
                        if (!h.gameObject.activeSelf) continue;
                        if (!misma_capa(e, h)) continue;
                        if (tiene(h, Propiedad.QUEMA)) { hay_quema_misma_capa = true; break; }
                    }

                    if (hay_quema_misma_capa && tiene(e, Propiedad.FUNDE))
                    {
                        var cE = e.celda;
                        tablero.rejilla[cE.x, cE.y].Remove(e);
                        e.gameObject.SetActive(false);
                        e.celda = new Vector2Int(-999, -999);
                        generar_productos_guarda(e, cE);
                        eliminadas++;
                    }
                }
            }

        // vence (misma capa), todas los objetos con TU mueren si comparten celda con VENCE
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                var celda = tablero.rejilla[x, y];
                bool hay_vence = false;

                //detectar si hay vence y tu en la misma capa
                for (int i = 0; i < celda.Count && !hay_vence; i++)
                {
                    var d = celda[i];
                    if (!d.gameObject.activeSelf) continue;
                    if (!tiene(d, Propiedad.VENCE)) continue;

                    for (int j = 0; j < celda.Count; j++)
                    {
                        var u = celda[j];
                        if (!u.gameObject.activeSelf) continue;
                        if (!misma_capa(d, u)) continue;
                        if (tiene(u, Propiedad.TU)) { hay_vence = true; break; }
                    }
                }

                if (!hay_vence) continue;

                var copia = new List<Entidad>(celda);
                foreach (var e in copia)
                {
                    if (!e.gameObject.activeSelf) continue;
                    if (!tiene(e, Propiedad.TU)) continue;

                    bool matar_aqui = false;
                    for (int j = 0; j < celda.Count; j++)
                    {
                        var d = celda[j];
                        if (!d.gameObject.activeSelf) continue;
                        if (!misma_capa(d, e)) continue;
                        if (tiene(d, Propiedad.VENCE)) { matar_aqui = true; break; }
                    }
                    if (matar_aqui)
                    {
                        var cE = e.celda;
                        tablero.rejilla[cE.x, cE.y].Remove(e);
                        e.gameObject.SetActive(false);
                        e.celda = new Vector2Int(-999, -999);
                        generar_productos_guarda(e, cE);
                        eliminadas++;
                    }
                }
            }

        return eliminadas; // devuelvo el total de entidades desactivadas
    }

    // devuelvo true si existe al menos una celda con TU y VENCE en la misma capa
    public bool comprobar_derrota()
    {
        int W = tablero.ancho, H = tablero.alto;
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                var celda = tablero.rejilla[x, y];
                for (int i = 0; i < celda.Count; i++)
                {
                    var a = celda[i]; // candidato TU
                    if (!a.gameObject.activeSelf) continue;
                    if (!tiene(a, Propiedad.TU)) continue;
                    for (int j = 0; j < celda.Count; j++)
                    {
                        var b = celda[j]; // candidato VENCE
                        if (!b.gameObject.activeSelf) continue;
                        if (!misma_capa(a, b)) continue; // deben compartir capa
                        if (tiene(b, Propiedad.VENCE)) return true; //hay derrota en esta celda
                    }
                }
            }
        return false; //no se encuentra derrota
    }

    // devuelve true si existe al menos una celda con TU y GANA en la misma capa
    public bool comprobar_victoria()
    {
        int W = tablero.ancho, H = tablero.alto;
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                var celda = tablero.rejilla[x, y];
                for (int i = 0; i < celda.Count; i++)
                {
                    var a = celda[i]; // candidato TU
                    if (!a.gameObject.activeSelf) continue;
                    if (!tiene(a, Propiedad.TU)) continue;
                    for (int j = 0; j < celda.Count; j++)
                    {
                        var b = celda[j]; //candidato GANA
                        if (!b.gameObject.activeSelf) continue;
                        if (!misma_capa(a, b)) continue; //deben compartir capa
                        if (tiene(b, Propiedad.META)) return true; // hay victoria en esta celda
                    }
                }
            }
        return false; //no se encuentra victoria
    }

    //devuelve true si queda al menos una entidad con TU activa en el tablero
    public bool queda_alguno_tu()
    {
        int W = tablero.ancho, H = tablero.alto;
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                var celda = tablero.rejilla[x, y];
                for (int i = 0; i < celda.Count; i++)
                {
                    var e = celda[i];
                    if (!e.gameObject.activeSelf) continue;
                    if (tiene(e, Propiedad.TU)) return true; // encontrado un TU activo
                }
            }
        return false; // no queda TU
    }

    // guarda, instanciacion (spawn) de productos al destruir
    // devuelve el prefab asociado a un objeto concreto
    private Entidad obtener_prefab(Objeto k)
    {
        // objetos
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
        }

        // texto
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
        }

        return null; // si no hay mapeo del prefab, en principio esto no va a pasar
    }

    // instancia los productos definidos por regla GUARDA cuando hay muerte
    private void generar_productos_guarda(Entidad muerta, Vector2Int en_celda)
    {
        if (reglas == null || reglas.guarda == null) return;
        if (!reglas.guarda.TryGetValue(muerta.objeto, out var lista)) return;

        for (int i = 0; i < lista.Count; i++)
        {
            var prod = lista[i]; // objeto a spawnear
            var prefab = obtener_prefab(prod); // y su Prefab asociado
            if (!prefab)
            {
                Debug.LogWarning($"[guarda] no hay prefab asignado para {prod}"); // doy aviso si falta prefab
                continue;
            }
            var e = Instantiate(prefab, tablero.transform); //instancia el nuevo Entity bajo el Board
            e.objeto = prod;
            e.es_texto = Entidad.es_texto_objeto(prod); // respeta si el spawn es texto
            e.dir_mov_desde_rotacion = false;           // para que no cambie orientacion al nacer
            tablero.agregar(e, en_celda); // se mete en la misma celda donde murió el original
        }
    }
}
