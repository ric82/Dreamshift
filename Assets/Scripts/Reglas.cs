/*
    reglas.cs
    
    parsea frases en el tablero y recalcula propiedades, transformaciones, reglas guarda y pilas de mueve

    el parser recorre el tablero en dos direcciones: horizontal derecha (1,0) y vertical abajo (0,-1)

    aplica tintes de color (rojo/azul) y el parpadeo cuando ambos existen

    ofrece utilidades para consultar propiedades y aplicar transformaciones

    el parser reconocerá sujetos (objeto o texto), operadores (=, guarda) y valores (propiedad, objeto, texto)

    realizadas depuraciones de "y" y "no", que rompian frases en determinadas posiciones
*/


using System.Collections.Generic;
using UnityEngine;


public class Reglas : MonoBehaviour
{

    //incidencia sprite movimiento

    [System.Serializable]
    public struct SpritesDireccion
    {
        public Sprite abajo;
        public Sprite derecha;
        public Sprite izquierda;
        public Sprite arriba;
    }

    [Header("sprites por dirección (objetos animables)")]
    public SpritesDireccion sprites_lu;
    public SpritesDireccion sprites_gato;
    public SpritesDireccion sprites_cohete;
    public SpritesDireccion sprites_pez;
    public SpritesDireccion sprites_bicho;
    public SpritesDireccion sprites_aru;

    //

    // colores y parpadeo
    public Color color_rojo = new Color(1f, 0.25f, 0.25f, 1f);
    public Color color_azul = new Color(0.25f, 0.5f, 1f, 1f);
    public bool tintar_textos = false;     // tomo la decision de no tintar los bloques de texto
    public float periodo_parpadeo = 0.6f;  // segundos para el parpadeo rojo/azul

    //pilas de "mueve" por tipo de objeto
    public Dictionary<Objeto, int> pilas_mueve = new Dictionary<Objeto, int>();

    public Tablero tablero;

    // propiedades activas por objeto
    public Dictionary<Objeto, Propiedad> propiedades = new Dictionary<Objeto, Propiedad>();

    // transformaciones objeto -> objeto (nunca texto -> objeto ni texto -> texto)
    public List<(Objeto desde, Objeto hasta)> transformaciones = new List<(Objeto, Objeto)>();

    // reglas "guarda" (que productos nacen al destruirse un objeto)
    public Dictionary<Objeto, List<Objeto>> guarda = new Dictionary<Objeto, List<Objeto>>();

    // AÑADIR cuando se cree un objeto
    // mapas del parser
    // texto -> objeto
    static readonly Dictionary<Objeto, Objeto> palabra_a_objeto = new Dictionary<Objeto, Objeto> {
        {Objeto.TEXTO_LU,     Objeto.LU},
        {Objeto.TEXTO_TRONCO, Objeto.TRONCO},
        {Objeto.TEXTO_COHETE, Objeto.COHETE},
        {Objeto.TEXTO_VAMPI,  Objeto.VAMPI},
        {Objeto.TEXTO_CAJA,   Objeto.CAJA},
        {Objeto.TEXTO_BICHO,  Objeto.BICHO},
        {Objeto.TEXTO_OVNI, Objeto.OVNI},
        {Objeto.TEXTO_LODO,   Objeto.LODO},
        {Objeto.TEXTO_ESPEJO, Objeto.ESPEJO},
        {Objeto.TEXTO_ASTRO,  Objeto.ASTRO},
        {Objeto.TEXTO_AGUA,  Objeto.AGUA},
        {Objeto.TEXTO_ARU,   Objeto.ARU},
        {Objeto.TEXTO_PEZ,  Objeto.PEZ},
        {Objeto.TEXTO_GATO,  Objeto.GATO},
        // nota: TEXTO_TEXTO no es un objeto, es el comodin para transformar objetos a sus bloques de texto
    };

    // AÑADIR cuando se cree un objeto
    // objeto -> texto (para reglas tipo "lu=texto")
    static readonly Dictionary<Objeto, Objeto> objeto_a_palabra = new Dictionary<Objeto, Objeto> {
        {Objeto.LU,     Objeto.TEXTO_LU},
        {Objeto.TRONCO, Objeto.TEXTO_TRONCO},
        {Objeto.COHETE, Objeto.TEXTO_COHETE},
        {Objeto.VAMPI,  Objeto.TEXTO_VAMPI},
        {Objeto.CAJA,   Objeto.TEXTO_CAJA},
        {Objeto.BICHO,  Objeto.TEXTO_BICHO},
        {Objeto.OVNI, Objeto.TEXTO_OVNI},
        {Objeto.LODO,   Objeto.TEXTO_LODO},
        {Objeto.ESPEJO, Objeto.TEXTO_ESPEJO},
        {Objeto.ASTRO,  Objeto.TEXTO_ASTRO},
        {Objeto.AGUA,  Objeto.TEXTO_AGUA},
        {Objeto.ARU,   Objeto.TEXTO_ARU},
        {Objeto.PEZ,  Objeto.TEXTO_PEZ},
        {Objeto.GATO,  Objeto.TEXTO_GATO},
    };

    // AÑADIR cuando se cree un objeto
    // texto -> propiedad
    static readonly Dictionary<Objeto, Propiedad> palabra_a_propiedad = new Dictionary<Objeto, Propiedad> {
        {Objeto.TEXTO_TU,     Propiedad.TU},
        {Objeto.TEXTO_EMPUJA, Propiedad.EMPUJA},
        {Objeto.TEXTO_META,   Propiedad.META},
        {Objeto.TEXTO_VENCE,  Propiedad.VENCE},
        {Objeto.TEXTO_PARA,   Propiedad.PARA},
        {Objeto.TEXTO_VUELA,  Propiedad.VUELA},
        {Objeto.TEXTO_HUNDE,  Propiedad.HUNDE},
        {Objeto.TEXTO_QUEMA,  Propiedad.QUEMA},
        {Objeto.TEXTO_FUNDE,  Propiedad.FUNDE},
        {Objeto.TEXTO_ROJO,   Propiedad.ROJO},
        {Objeto.TEXTO_AZUL,   Propiedad.AZUL},
        {Objeto.TEXTO_MUEVE,  Propiedad.MUEVE},
        {Objeto.TEXTO_ABRE,   Propiedad.ABRE},
        {Objeto.TEXTO_CIERRA, Propiedad.CIERRA},
        {Objeto.TEXTO_TUNEL,  Propiedad.TUNEL},
    };

    // devuelve la palabra de texto en la celda si hay algun bloque de texto, si no -1
    Objeto palabra_en(Vector2Int c)
    {
        foreach (var e in tablero.rejilla[c.x, c.y])
            if (e.es_texto) return e.objeto;
        return (Objeto)(-1);
    }

    // parsea el tablero y actualiza: propiedades, transformaciones, guarda y pilas_mueve
    public void recalcular_propiedades()
    {
        propiedades.Clear();
        transformaciones.Clear();
        guarda.Clear();

        pilas_mueve.Clear();
        var mueve_suma = new Dictionary<Objeto, int>();
        var mueve_resta = new Dictionary<Objeto, int>();

        // AÑADIR cuando se cree un objeto
        // por defecto, todo bloque de texto es empuja
        // importante: incluir todos los TEXTO_* que existan en el enum
        propiedades[Objeto.TEXTO_LU] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_TRONCO] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_COHETE] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_VAMPI] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_CAJA] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_BICHO] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_OVNI] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_LODO] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_ESPEJO] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_ASTRO] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_AGUA] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_ARU] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_PEZ] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_GATO] = Propiedad.EMPUJA;

        propiedades[Objeto.TEXTO_TEXTO] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_IGUAL] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_PARA] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_Y] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_NO] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_VUELA] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_HUNDE] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_QUEMA] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_FUNDE] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_ROJO] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_AZUL] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_MUEVE] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_GUARDA] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_ABRE] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_CIERRA] = Propiedad.EMPUJA;
        propiedades[Objeto.TEXTO_TUNEL] = Propiedad.EMPUJA;

        // universo de objetos no-texto para reglas con "no"
        var universo_objetos = new HashSet<Objeto>(palabra_a_objeto.Values);

        // altas y bajas de propiedades (las bajas prevalecen al final)
        var altas = new Dictionary<Objeto, Propiedad>();
        var bajas = new Dictionary<Objeto, Propiedad>();

        // direcciones de escaneo: horizontal (1,0) y vertical (0,-1)
        Vector2Int[] direcciones = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(0, -1) };

        //acumulador de "guarda"
        var guarda_altas = new Dictionary<Objeto, List<Objeto>>();

        // lista de todos los tokens de texto, para aplicar reglas sobre "texto"
        List<Objeto> todos_los_textos()
        {
            var lista = new List<Objeto>();
            foreach (Objeto o in System.Enum.GetValues(typeof(Objeto)))
                if (Entidad.es_texto_objeto(o)) lista.Add(o);
            return lista;
        }
        var textos = todos_los_textos();

        // func para acumular propiedades y "guarda"
        void acum_prop(Objeto objetivo, Propiedad p, bool alta)
        {
            if (alta)
                altas[objetivo] = altas.ContainsKey(objetivo) ? (altas[objetivo] | p) : p;
            else
                bajas[objetivo] = bajas.ContainsKey(objetivo) ? (bajas[objetivo] | p) : p;
        }
        void anadir_guarda(Objeto sujeto, Objeto producto)
        {
            if (!guarda_altas.TryGetValue(sujeto, out var lista)) { lista = new List<Objeto>(); guarda_altas[sujeto] = lista; }
            if (!lista.Contains(producto)) lista.Add(producto);
        }

        bool inicio_valor(Objeto w)
        {
            return w == Objeto.TEXTO_NO || w == Objeto.TEXTO_TEXTO || palabra_a_propiedad.ContainsKey(w) || palabra_a_objeto.ContainsKey(w);
        }
        bool inicio_sujeto(Objeto w)
        {
            return w == Objeto.TEXTO_NO || w == Objeto.TEXTO_TEXTO || palabra_a_objeto.ContainsKey(w);
        }

        // func antiduplicado
        bool sujeto_atomico(Objeto w) => (w == Objeto.TEXTO_TEXTO) || palabra_a_objeto.ContainsKey(w);
        bool saltar_inicio(Vector2Int s, Vector2Int dir)
        {
            var prev = s - dir;
            if (!tablero.en_rango(prev)) return false;

            var wp = palabra_en(prev);
            if (wp == Objeto.TEXTO_NO) return true;  // estoy en mitad del bloque [no]*
            if (wp == Objeto.TEXTO_Y) return true;  // inicio duplicado tras "y"

            if (sujeto_atomico(wp))
            {
                var prev2 = prev - dir;
                if (tablero.en_rango(prev2) && palabra_en(prev2) == Objeto.TEXTO_Y)
                    return true;
            }
            return false;
        }

        for (int x = 0; x < tablero.ancho; x++)
            for (int y = 0; y < tablero.alto; y++)
            {
                foreach (var dir in direcciones)
                {
                    var inicio = new Vector2Int(x, y);
                    var w_inicio = palabra_en(inicio);

                    // recortar "y" iniciales
                    bool recorto_y = false;
                    while (w_inicio == Objeto.TEXTO_Y)
                    {
                        inicio += dir;
                        if (!tablero.en_rango(inicio)) { recorto_y = false; break; }
                        w_inicio = palabra_en(inicio);
                        recorto_y = true;
                    }

                    if (!inicio_sujeto(w_inicio)) continue;

                    if (!recorto_y && saltar_inicio(inicio, dir))
                        continue;

                    // sujetos: ([no]* (objeto|texto)) (y [no]* (objeto|texto))*
                    var sujetos = new List<(HashSet<Objeto> conjunto, bool afecta_texto)>();
                    var c = inicio;
                    bool parseo_sujeto = false;

                    while (true)
                    {
                        int no_izq = 0;
                        while (tablero.en_rango(c) && palabra_en(c) == Objeto.TEXTO_NO) { no_izq++; c += dir; }
                        if (!tablero.en_rango(c)) { sujetos.Clear(); break; }

                        var w_subj = palabra_en(c);
                        if (w_subj == (Objeto)(-1)) { sujetos.Clear(); break; }

                        bool neg_subj = (no_izq & 1) == 1;
                        var conjunto = new HashSet<Objeto>();
                        bool afecta_texto = false;

                        if (w_subj == Objeto.TEXTO_TEXTO)
                        {
                            if (!neg_subj)
                                afecta_texto = true;
                            else
                                foreach (var n in universo_objetos) conjunto.Add(n); // "no texto" -> todos los objetos
                            c += dir;
                            parseo_sujeto = true;
                        }
                        else if (palabra_a_objeto.ContainsKey(w_subj))
                        {
                            var subj = palabra_a_objeto[w_subj];
                            if (!neg_subj)
                                conjunto.Add(subj);
                            else
                                foreach (var n in universo_objetos) if (n != subj) conjunto.Add(n);
                            c += dir;
                            parseo_sujeto = true;
                        }
                        else
                        {
                            sujetos.Clear();
                            break;
                        }

                        sujetos.Add((conjunto, afecta_texto));

                        if (tablero.en_rango(c) && palabra_en(c) == Objeto.TEXTO_Y)
                        {
                            c += dir;
                            if (!tablero.en_rango(c) || !inicio_sujeto(palabra_en(c))) { sujetos.Clear(); break; }
                            continue;
                        }
                        break;
                    }

                    if (!parseo_sujeto || sujetos.Count == 0) continue;

                    // a partir de aqui debe haber "=" o "guarda"
                    if (!tablero.en_rango(c)) continue;
                    var enlace = palabra_en(c);
                    if (enlace != Objeto.TEXTO_IGUAL && enlace != Objeto.TEXTO_GUARDA) continue;

                    // valores: ([no]* (propiedad/objeto/texto)) (y...)*
                    var cur = c + dir;

                    while (tablero.en_rango(cur))
                    {
                        int no_der = 0;
                        while (tablero.en_rango(cur) && palabra_en(cur) == Objeto.TEXTO_NO) { no_der++; cur += dir; }
                        if (!tablero.en_rango(cur)) break;

                        var w_val = palabra_en(cur);
                        if (w_val == (Objeto)(-1)) break;

                        bool negar_valor = (no_der & 1) == 1;

                        if (enlace == Objeto.TEXTO_IGUAL)
                        {
                            // propiedad
                            if (palabra_a_propiedad.TryGetValue(w_val, out var p))
                            {
                                foreach (var st in sujetos)
                                {
                                    foreach (var n in st.conjunto)
                                    {
                                        if (p == Propiedad.MUEVE)
                                        {
                                            if (!negar_valor) { if (!mueve_suma.ContainsKey(n)) mueve_suma[n] = 0; mueve_suma[n] += 1; }
                                            else { if (!mueve_resta.ContainsKey(n)) mueve_resta[n] = 0; mueve_resta[n] += 1; }
                                        }
                                        acum_prop(n, p, !negar_valor);
                                    }
                                    if (st.afecta_texto)
                                    {
                                        foreach (var t in textos)
                                            acum_prop(t, p, !negar_valor);
                                    }
                                }
                                cur += dir;
                            }
                            // transformacion a objeto
                            else if (palabra_a_objeto.ContainsKey(w_val))
                            {
                                if (!negar_valor)
                                {
                                    var destino = palabra_a_objeto[w_val];
                                    foreach (var st in sujetos)
                                    {
                                        foreach (var n in st.conjunto)
                                            transformaciones.Add((n, destino));
                                        // "texto = objeto" no se usa
                                    }
                                }
                                cur += dir;
                            }
                            // transformacion a texto
                            else if (w_val == Objeto.TEXTO_TEXTO)
                            {
                                if (!negar_valor)
                                {
                                    foreach (var st in sujetos)
                                    {
                                        foreach (var n in st.conjunto)
                                        {
                                            if (objeto_a_palabra.TryGetValue(n, out var palabra))
                                                transformaciones.Add((n, palabra));
                                        }
                                        // "texto = texto" sin efecto
                                    }
                                }
                                cur += dir;
                            }
                            else break;
                        }
                        else // enlace == guarda
                        {
                            // guarda <objeto> (sin "no")
                            if (palabra_a_objeto.ContainsKey(w_val) && !negar_valor)
                            {
                                var prod = palabra_a_objeto[w_val];
                                foreach (var st in sujetos)
                                {
                                    foreach (var n in st.conjunto) anadir_guarda(n, prod);
                                    if (st.afecta_texto)
                                        foreach (var t in textos) anadir_guarda(t, prod);
                                }
                                cur += dir;
                            }
                            //guarda texto ->cada sujeto objeto produce su bloque de texto
                            else if (w_val == Objeto.TEXTO_TEXTO && !negar_valor)
                            {
                                foreach (var st in sujetos)
                                {
                                    foreach (var n in st.conjunto)
                                    {
                                        if (objeto_a_palabra.TryGetValue(n, out var palabra))
                                            anadir_guarda(n, palabra);
                                    }
                                    if (st.afecta_texto)
                                    {
                                        foreach (var t in textos)
                                            anadir_guarda(t, t);
                                    }
                                }
                                cur += dir;
                            }
                            else break;
                        }

                        //encadenar con "y"
                        if (tablero.en_rango(cur) && palabra_en(cur) == Objeto.TEXTO_Y)
                        {
                            cur += dir;
                            if (!tablero.en_rango(cur) || !inicio_valor(palabra_en(cur))) break;
                            continue;
                        }
                        else break;
                    }
                }
            }

        // ojo si identidad (x = x) cancela otras transformaciones de x
        if (transformaciones.Count > 0)
        {
            var por_origen = new Dictionary<Objeto, HashSet<Objeto>>();
            for (int i = 0; i < transformaciones.Count; i++)
            {
                var tr = transformaciones[i];
                if (!por_origen.TryGetValue(tr.desde, out var set))
                {
                    set = new HashSet<Objeto>();
                    por_origen[tr.desde] = set;
                }
                set.Add(tr.hasta);
            }
            var filtradas = new List<(Objeto desde, Objeto hasta)>();
            foreach (var kv in por_origen)
            {
                var subj = kv.Key;
                var destinos = kv.Value;
                if (destinos.Contains(subj))
                    filtradas.Add((subj, subj));
                else
                    foreach (var to in destinos) filtradas.Add((subj, to));
            }
            transformaciones = filtradas;
        }

        // aplicar altas y bajas de propiedades (la baja prevalece)
        foreach (var kv in altas)
        {
            var o = kv.Key; var p = kv.Value;
            propiedades[o] = propiedades.ContainsKey(o) ? (propiedades[o] | p) : p;
        }
        foreach (var kv in bajas)
        {
            var o = kv.Key; var p = kv.Value;
            if (!propiedades.ContainsKey(o)) propiedades[o] = Propiedad.NINGUNA;
            propiedades[o] &= ~p;
        }

        // resolver pilas de "mueve" y reflejar el bit en propiedades
        foreach (var n in palabra_a_objeto.Values)
        {
            mueve_suma.TryGetValue(n, out var a);
            mueve_resta.TryGetValue(n, out var r);
            int cuenta = a - r;
            if (cuenta < 0) cuenta = 0;

            if (cuenta > 0)
            {
                pilas_mueve[n] = cuenta;
                propiedades[n] = propiedades.ContainsKey(n) ? (propiedades[n] | Propiedad.MUEVE) : Propiedad.MUEVE;
            }
            else
            {
                if (propiedades.ContainsKey(n)) propiedades[n] &= ~Propiedad.MUEVE;
            }
        }

        // fijar reglas "guarda"
        foreach (var kv in guarda_altas)
            guarda[kv.Key] = kv.Value;

        // aplicar tintado
        aplicar_tintes_color();
    }

    //consulta si un objeto tiene una propiedad concreta
    public bool tiene_propiedad(Objeto o, Propiedad p)
    {
        if (propiedades.TryGetValue(o, out var got))
            return (got & p) != 0;
        return false;
    }

    // aplica transformaciones en pase unico (simultaneo)
    public bool aplicar_transformaciones_pase_unico(Tablero t, BaseApariencias apariencias)
    {
        if (transformaciones == null || transformaciones.Count == 0) return false;

        // construir mapa desde-> hasta(el ultimo gana si hay duplicados)
        var mapa = new Dictionary<Objeto, Objeto>();
        for (int i = 0; i < transformaciones.Count; i++)
        {
            var tr = transformaciones[i];
            mapa[tr.desde] = tr.hasta;
        }

        // snapshot de entidades al inicio del turno
        var snapshot = new List<(Entidad e, Objeto obj0)>();
        for (int x = 0; x < t.ancho; x++)
            for (int y = 0; y < t.alto; y++)
            {
                var celda = t.rejilla[x, y];
                for (int i = 0; i < celda.Count; i++)
                {
                    var e = celda[i];
                    if (!e.gameObject.activeSelf) continue;
                    snapshot.Add((e, e.objeto));
                }
            }

        bool cambio = false;

        for (int i = 0; i < snapshot.Count; i++)
        {
            var (e, k0) = snapshot[i];
            if (!e.gameObject.activeSelf) continue;

            if (mapa.TryGetValue(k0, out var to))
            {
                Sprite sprite = (apariencias != null) ? apariencias.obtener(to) : null;
                if (sprite == null)
                {
                    var sr_en_tablero = buscar_sprite_en_tablero(to);
                    if (sr_en_tablero != null) sprite = sr_en_tablero;
                }
                e.establecer_objeto(to, sprite);
                cambio = true;


                //incidencia sprite movimiento

                // animación direccional tras transformacion
                bool debe_animar = !e.es_texto && es_animable(e.objeto);
                var dir_spr = e.GetComponent<SpritePorDireccion>();
                var sr_destino = e.GetComponent<SpriteRenderer>() ?? e.GetComponentInChildren<SpriteRenderer>();

                if (debe_animar)
                {
                    // asegurar componente
                    if (!dir_spr) dir_spr = e.gameObject.AddComponent<SpritePorDireccion>();
                    dir_spr.sr = sr_destino;

                    //asignar sprites desde el pack serializado
                    var pack = obtener_pack(e.objeto);
                    dir_spr.sprite_abajo = pack.abajo;
                    dir_spr.sprite_derecha = pack.derecha;
                    dir_spr.sprite_izquierda = pack.izquierda;
                    dir_spr.sprite_arriba = pack.arriba;

                    // si el pack está vacío por error, no mantengo el componente
                    bool pack_vacio = (pack.abajo == null && pack.derecha == null && pack.izquierda == null && pack.arriba == null);
                    if (pack_vacio)
                    {
                        if (dir_spr) Destroy(dir_spr);
                    }
                    else
                    {
                        // refresco inmediato para que el sprite coincida con la dirección actual
                        dir_spr.ForzarRefrescoInmediato();
                    }
                }
                else
                {
                    // no debe animar: elimina el componente si existe para que no pisotee sprites
                    if (dir_spr) Destroy(dir_spr);
                }


                //


            }
        }

        return cambio;
    }

    // version anterior (ya no la uso, mantengo por si la necesito mas adelante)
    public bool aplicar_transformaciones(Tablero t, BaseApariencias apariencias)
    {
        if (transformaciones.Count == 0) return false;
        bool cambio = false;

        for (int x = 0; x < t.ancho; x++)
            for (int y = 0; y < t.alto; y++)
            {
                var celda = t.rejilla[x, y];
                for (int i = 0; i < celda.Count; i++)
                {
                    var e = celda[i];

                    for (int ti = 0; ti < transformaciones.Count; ti++)
                    {
                        var tr = transformaciones[ti]; // desde -> hasta
                        if (e.objeto == tr.desde)
                        {
                            var sprite = apariencias ? apariencias.obtener(tr.hasta) : null;
                            e.establecer_objeto(tr.hasta, sprite);
                            cambio = true;
                        }
                    }
                }
            }
        return cambio;
    }

    // intenta encontrar un sprite ya presente en el tablero para el objeto dado
    private Sprite buscar_sprite_en_tablero(Objeto o)
    {
        for (int x = 0; x < tablero.ancho; x++)
            for (int y = 0; y < tablero.alto; y++)
            {
                var celda = tablero.rejilla[x, y];
                for (int i = 0; i < celda.Count; i++)
                {
                    var e = celda[i];
                    if (e.objeto != o) continue;
                    var sr = e.GetComponent<SpriteRenderer>() ?? e.GetComponentInChildren<SpriteRenderer>();
                    if (sr) return sr.sprite;
                }
            }
        return null;
    }

    //pinturas y parpadeo rojo/azul
    void aplicar_tintes_color()
    {
        for (int x = 0; x < tablero.ancho; x++)
            for (int y = 0; y < tablero.alto; y++)
            {
                var celda = tablero.rejilla[x, y];
                for (int i = 0; i < celda.Count; i++)
                {
                    var e = celda[i];
                    if (!e.gameObject.activeSelf) continue;

                    var sr = e.GetComponent<SpriteRenderer>() ?? e.GetComponentInChildren<SpriteRenderer>();
                    if (!sr) continue;

                    if (e.es_texto && !tintar_textos)
                    {
                        sr.color = Color.white;
                        quitar_parpadeo_si_existe(e);
                        continue;
                    }

                    bool es_rojo = tiene_propiedad(e.objeto, Propiedad.ROJO);
                    bool es_azul = tiene_propiedad(e.objeto, Propiedad.AZUL);

                    if (es_rojo && es_azul)
                    {
                        var bl = e.GetComponent<ParpadeoColor>();
                        if (!bl) bl = e.gameObject.AddComponent<ParpadeoColor>();
                        bl.sr = sr;
                        bl.colorA = color_rojo;
                        bl.colorB = color_azul;
                        bl.period = Mathf.Max(0.06f, periodo_parpadeo);
                    }
                    else
                    {
                        quitar_parpadeo_si_existe(e);
                        if (es_azul) sr.color = color_azul;
                        else if (es_rojo) sr.color = color_rojo;
                        else sr.color = Color.white;
                    }
                }
            }
    }

    void quitar_parpadeo_si_existe(Entidad e)
    {
        var bl = e.GetComponent<ParpadeoColor>();
        if (bl) Destroy(bl);
    }


    //incidencia sprite movimiento
    bool es_animable(Objeto o)
    {
        return o == Objeto.LU
            || o == Objeto.GATO
            || o == Objeto.COHETE
            || o == Objeto.PEZ
            || o == Objeto.BICHO
            || o == Objeto.ARU;
    }

    SpritesDireccion obtener_pack(Objeto o)
    {
        switch (o)
        {
            case Objeto.LU: return sprites_lu;
            case Objeto.GATO: return sprites_gato;
            case Objeto.COHETE: return sprites_cohete;
            case Objeto.PEZ: return sprites_pez;
            case Objeto.BICHO: return sprites_bicho;
            case Objeto.ARU: return sprites_aru;
            default: return new SpritesDireccion(); // todo null
        }
    }
    //

}
