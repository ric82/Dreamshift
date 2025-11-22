/*
    controljuego.cs
    
    coordina el bucle de juego por paso: entrada, movimiento TU, parseo de reglas, transformaciones,
    automovimientos, tuneles, colisiones y comprobaciones de victoria/derrota

    gestiona autorepeat del movimiento, espera (wait) y deshacer (undo con repeticion)

    mantiene la lista de entidades con propiedad TU para filtrar input cuando corresponde

    para claridad se detalla flujo de un paso, si hay modificaciones cambiar aqui también:

    1-empujar el estado a la pila de UNDO
    2-mover todas las entidades con TU en la dirección solicitada (aplicando empuje)
    3-recalcular reglas del tablero
    4-aplicar transformaciones
    5-reconstruir la lista de TU (puede cambiar tras transformaciones)
    6-ejecutar MOVE en stacks y con rebote
    7-aplicar TUNEL
    8-resolver hunde, quema/funde, abre/cierra y vence, disparar GUARDA en destrucciones
    9-si no queda TU
      -si no hay MUEVE activos, entonces derrota
      -si hay MUEVE, permitir que el mundo avance con WAIT (para resolver ciertos niveles)
    10-comprobar victoria

    flujo de WAIT:
      - igual que de un paso pero sin mover TU al inicio, sirve para avanzar solo los MOVE
        y resolver(posibilidad) el mundo cuando no hay TU

*/

using UnityEngine.SceneManagement;

using System.Collections.Generic;
using UnityEngine;


public class ControlJuego : MonoBehaviour
{
    //referencias
    public Tablero tablero;
    public Reglas reglas;
    public MotorMovimiento motor_movimiento;
    public VictoriaDerrota victoria_derrota;
    public PilaDeshacer pila_deshacer;

    [Header("apariencias (para objeto->objeto)")]
    public BaseApariencias apariencias;   // asignar en el inspector

    // objetos con propiedad TU (incluye textos si la tienen, puede haber cuando texto=TU)
    private List<Entidad> entidades_tu = new List<Entidad>();

    
    [Header("movimiento continuo")]
    [SerializeField] private float retardo_inicial_repeticion = 0.25f; // tiempo desde la primera pulsación hasta empezar autorepeat
    [SerializeField] private float intervalo_repeticion = 0.08f; //intervalo entre repeticiones mientras se mantiene la tecla

    // UNDO-REPEAT
    [Header("deshacer continuo (z mantenida)")]
    [SerializeField] private bool deshacer_en_bucle_habilitado = true; // activa/desactiva autorepeat del UNDO
    [SerializeField] private float deshacer_retardo_inicial = 0.25f; // retardo inicial antes de repetir UNDO
    [SerializeField] private float deshacer_intervalo = 0.08f; // intervalo entre UNDO consecutivos

    // WAIT (esperar un turno sin mover TU)
    [Header("wait (esperar un turno)")]
    public KeyCode tecla_esperar_primaria = KeyCode.X; // tecla 1 para WAIT
    public KeyCode tecla_esperar_alt = KeyCode.Space; // tecla 2 para WAIT
    [SerializeField] private bool esperar_en_bucle_habilitado = true; // autorepeat para WAIT
    [SerializeField] private float esperar_retardo_inicial = 0.25f; // tetardo inicial de WAIT
    [SerializeField] private float esperar_intervalo = 0.08f; // Intervalo de WAIT repetido

    //configuraciones

    // estado de autorepeat de movimiento
    private Vector2Int dir_mantenida = Vector2Int.zero;
    private float tiempo_mantenida = 0f;
    private float tiempo_repeticion = 0f;

    // estado de autorepeat de deshacer
    private bool deshacer_mantenido = false;
    private float deshacer_tiempo_mantenida = 0f;
    private float deshacer_tiempo_repeticion = 0f;

    //estado de autorepeat de WAIT(esperar)
    private bool esperar_mantenido = false;
    private float esperar_tiempo_mantenida = 0f;
    private float esperar_tiempo_repeticion = 0f;

    // estados de partida
    private bool estado_derrota = false; // será true si se detecta derrota
    private bool estado_victoria = false; //será true si se detecta victoria

    // espera a que el tablero este listo
    private System.Collections.IEnumerator Start()
    {
        while (tablero == null || tablero.rejilla == null) yield return null;

        reglas.recalcular_propiedades(); // recalcula reglas iniciales según el estado del tablero
        aplicar_transformaciones_una_vez(); // transformaciones iniciales (si hay reglas que lo requieran)
        reconstruir_tu(); //lista de entidades TU
    }

    // pausa
    bool _pausa_anterior = false;

    void ResetearEstadosInput()
    {
        // movimiento (autorepeat)
        dir_mantenida = Vector2Int.zero;
        tiempo_mantenida = 0f;
        tiempo_repeticion = 0f;

        // deshacer (autorepeat)
        deshacer_mantenido = false;
        deshacer_tiempo_mantenida = 0f;
        deshacer_tiempo_repeticion = 0f;

        // esperar (autorepeat)
        esperar_mantenido = false;
        esperar_tiempo_mantenida = 0f;
        esperar_tiempo_repeticion = 0f;
    }

    void Update()
    {

        // pausa bloquea input y limpia estados al entrar/salir
        if (MenuPausa.esta_pausado)
        {
            if (!_pausa_anterior) ResetearEstadosInput(); //ojo,primer frame en pausa
            _pausa_anterior = true;
            return;
        }
        else if (_pausa_anterior)
        {
            //primer frame tras reanudar evita saltos por teclas mantenidas, soluc a incidencia
            ResetearEstadosInput();
            _pausa_anterior = false;
        }



        // si el tablero no está listo, nada que hacer
        if (tablero == null || tablero.rejilla == null) return;

        //deshacer siempre disponible (Z)
        if (Input.GetKey(KeyCode.Z))
        {
            if (!deshacer_mantenido)
            {
                deshacer_mantenido = true;
                deshacer_tiempo_mantenida = 0f;
                deshacer_tiempo_repeticion = 0f;
                hacer_deshacer();
            }
            else
            {
                deshacer_tiempo_mantenida += Time.deltaTime;
                if (deshacer_en_bucle_habilitado && deshacer_tiempo_mantenida >= deshacer_retardo_inicial)
                {
                    deshacer_tiempo_repeticion += Time.deltaTime;
                    if (deshacer_tiempo_repeticion >= deshacer_intervalo)
                    {
                        deshacer_tiempo_repeticion = 0f;
                        hacer_deshacer();
                    }
                }
            }
            return;
        }
        else if (deshacer_mantenido)
        {
            deshacer_mantenido = false;
            deshacer_tiempo_mantenida = 0f;
            deshacer_tiempo_repeticion = 0f;
        }
        //

        //wait (x o espacio)
        bool tecla_wait = Input.GetKey(tecla_esperar_primaria) || Input.GetKey(tecla_esperar_alt);
        if (tecla_wait)
        {
            if (!esperar_mantenido)
            {
                esperar_mantenido = true;
                esperar_tiempo_mantenida = 0f;
                esperar_tiempo_repeticion = 0f;
                paso_espera();
            }
            else
            {
                esperar_tiempo_mantenida += Time.deltaTime;
                if (esperar_en_bucle_habilitado && esperar_tiempo_mantenida >= esperar_retardo_inicial)
                {
                    esperar_tiempo_repeticion += Time.deltaTime;
                    if (esperar_tiempo_repeticion >= esperar_intervalo)
                    {
                        esperar_tiempo_repeticion = 0f;
                        paso_espera();
                    }
                }
            }
            return;
        }
        else if (esperar_mantenido)
        {
            esperar_mantenido = false;
            esperar_tiempo_mantenida = 0f;
            esperar_tiempo_repeticion = 0f;
        }
        //

        // ojo, si ya hay victoria o derrota, no aceptar más input de movimiento
        if (estado_victoria || estado_derrota)
            return;

        // si no hay tu, ignora dir movimiento (solo wait/undo pueden actuar)
        if (entidades_tu.Count == 0)
            return;

        //tecla recien pulsada -> paso inmediato
        Vector2Int pulsada = leer_pulsada();
        if (pulsada != Vector2Int.zero)
        {
            dir_mantenida = pulsada;
            tiempo_mantenida = 0f;
            tiempo_repeticion = 0f;
            paso(dir_mantenida);
            return;
        }

        //si no hay tecla mantenida, resetea
        Vector2Int mantenida = leer_mantenida();
        if (mantenida == Vector2Int.zero)
        {
            dir_mantenida = Vector2Int.zero;
            tiempo_mantenida = 0f;
            tiempo_repeticion = 0f;
            return;
        }

        // si cambio de direccion mientras se mantiene -> paso inmediato
        if (mantenida != dir_mantenida)
        {
            dir_mantenida = mantenida;
            tiempo_mantenida = 0f;
            tiempo_repeticion = 0f;
            paso(dir_mantenida);
            return;
        }

        // misma direccion mantenida ->autorepeat
        tiempo_mantenida += Time.deltaTime;
        if (tiempo_mantenida >= retardo_inicial_repeticion)
        {
            tiempo_repeticion += Time.deltaTime;
            if (tiempo_repeticion >= intervalo_repeticion)
            {
                tiempo_repeticion = 0f;
                paso(dir_mantenida);
            }
        }
    }

    //paso normal (con movimiento de TU)
    private void paso(Vector2Int dir)
    {
        if (estado_derrota || estado_victoria) return;

        pila_deshacer.Push();

        var movidos_este_paso = new HashSet<Entidad>();
        foreach (var e in entidades_tu.ToArray())
        {
            if (movidos_este_paso.Contains(e)) continue;
            motor_movimiento.intentar_mover(e, dir, movidos_este_paso);
        }

        // 1-reglas del texto actual
        reglas.recalcular_propiedades();

        // 2-transformaciones
        aplicar_transformaciones_una_vez();

        // 3-reconstruye tu (puede cambiar tras transformaciones)
        reconstruir_tu();

        // 4-mueve (stacks + rebote)
        hacer_movimientos_auto_una_vez_por_turno();

        // 5-tuneles tras mueve
        victoria_derrota.aplicar_tuneles();
        reglas.recalcular_propiedades();
        reconstruir_tu();

        // 6-muertes por hunde/quema-funde/vence/abre-cierra
        int muertas = victoria_derrota.aplicar_derrota();
        if (muertas > 0)
        {
            reglas.recalcular_propiedades();
            reconstruir_tu();
        }

        // 7-derrota solo si no hay tu y tampoco hay mueve
        if (!victoria_derrota.queda_alguno_tu())
        {
            if (!existe_alguno_mueve())
            {
                estado_derrota = true;
                dir_mantenida = Vector2Int.zero;
                tiempo_mantenida = tiempo_repeticion = 0f;
                Debug.Log("derrota (no quedan TU). pulsa Z para deshacer."); //lanzo mensaje

                Sonidos.instancia.reproducir_morir(); //sonido de muerte

                return;
            }
            else
            {
                // sin tu, pero hay mueve, el jugador puede pulsar wait para que el mundo avance
                return;
            }
        }

        // 8-victoria
        if (victoria_derrota.comprobar_victoria())
        {
            estado_victoria = true;
            dir_mantenida = Vector2Int.zero;
            tiempo_mantenida = tiempo_repeticion = 0f;
            Debug.Log("has superado el nivel"); //lanzo mensaje

            Sonidos.instancia.reproducir_ganar(); //sonido de victoria

            //cargo el hub
            var escenaHub = EstadoRetornoHub.escena_hub;
            if (string.IsNullOrEmpty(escenaHub)) escenaHub = "Hub";

            SceneManager.LoadScene(escenaHub);
            return; // evito seguir procesando tras lanzar el cambio de escena

        }
    }

    // paso de wait (sin mover TU) 
    private void paso_espera()
    {
        if (estado_derrota || estado_victoria) return;

        pila_deshacer.Push(); // guardo snapshot para poder deshacer este WAIT

        // no autorepeat de flechas al hacer WAIT
        dir_mantenida = Vector2Int.zero;
        tiempo_mantenida = tiempo_repeticion = 0f;

        //1-reglas
        reglas.recalcular_propiedades();

        // 2-transformaciones
        aplicar_transformaciones_una_vez();

        // 3-reconstruye tu
        reconstruir_tu();

        // 4-mueve
        hacer_movimientos_auto_una_vez_por_turno();

        // 5-tuneles
        victoria_derrota.aplicar_tuneles();
        reglas.recalcular_propiedades();
        reconstruir_tu();

        // 6-muertes
        int muertas = victoria_derrota.aplicar_derrota();
        if (muertas > 0)
        {
            reglas.recalcular_propiedades();
            reconstruir_tu();
        }

        // 7) si sigue sin tu y ya no hay mueve -> derrota, si hay mueve, seguimos en el limbo
        if (!victoria_derrota.queda_alguno_tu() && !existe_alguno_mueve())
        {
            estado_derrota = true;
            Debug.Log("derrota (no quedan tu ni objetos mueve). pulsa Z para deshacer.");
            return;
        }

        // 8) victoria (ha reaparecido al menos un TU y toca meta)
        if (victoria_derrota.queda_alguno_tu() && victoria_derrota.comprobar_victoria())
        {
            estado_victoria = true;
            Debug.Log("has superado el nivel");

        }
    }

    //transformaciones + recalculo
    private void aplicar_transformaciones_una_vez()
    {
        if (apariencias == null) return;
        bool cambio = reglas.aplicar_transformaciones_pase_unico(tablero, apariencias);
        if (cambio)
            reglas.recalcular_propiedades(); // si hubo cambios, recalcular props
    }

    //deshacer
    private void hacer_deshacer()
    {
        pila_deshacer.Pop(); // restaura el snapshot anterior
        reglas.recalcular_propiedades(); // recalcula reglas para el estado restaurado
        reconstruir_tu();

        // derrota tambien en undo, solo si no hay tu y no hay mueve
        estado_derrota = !victoria_derrota.queda_alguno_tu() && !existe_alguno_mueve();

        if (!estado_derrota)
        {
            dir_mantenida = Vector2Int.zero;
            tiempo_mantenida = tiempo_repeticion = 0f;
        }
        if (estado_victoria && !victoria_derrota.comprobar_victoria()) estado_victoria = false;

        Sonidos.instancia.reproducir_undo(); //sonido de victoria

    }

    // mueve (auto)
    private void hacer_movimientos_auto_una_vez_por_turno()
    {
        if (!existe_alguno_mueve()) return; // si no hay MOVE en el tablero, nada que hacer

        int max = 1; //número de subpasos a ejecutar este turno (1 por defecto, a priori se quedará asi)
        if (reglas.pilas_mueve != null && reglas.pilas_mueve.Count > 0)
        {
            max = 0;
            foreach (var kv in reglas.pilas_mueve)
                if (kv.Value > max) max = kv.Value;
            if (max <= 0) max = 1;
        }

        for (int s = 1; s <= max; s++)
        {
            var movidos_subpaso = new HashSet<Entidad>();

            var que_se_mueven = new List<Entidad>();
            for (int x = 0; x < tablero.ancho; x++)
                for (int y = 0; y < tablero.alto; y++)
                {
                    var celda = tablero.rejilla[x, y];
                    for (int i = 0; i < celda.Count; i++)
                    {
                        var e = celda[i];
                        if (!e.gameObject.activeSelf || e.es_texto) continue;
                        if (!reglas.tiene_propiedad(e.objeto, Propiedad.MUEVE)) continue;

                        int necesidad = 1;
                        if (reglas.pilas_mueve != null && reglas.pilas_mueve.TryGetValue(e.objeto, out var v))
                            necesidad = v;

                        if (necesidad >= s) que_se_mueven.Add(e);
                    }
                }

            for (int i = 0; i < que_se_mueven.Count; i++)
            {
                var m = que_se_mueven[i];
                if (movidos_subpaso.Contains(m)) continue;

                var dir = m.dir_mov;
                if (dir == Vector2Int.zero) dir = Vector2Int.down;

                bool ok = motor_movimiento.intentar_mover(m, dir, movidos_subpaso);
                if (ok) { m.dir_mov = dir; }
                else
                {
                    var inv = -dir;
                    bool ok2 = motor_movimiento.intentar_mover(m, inv, movidos_subpaso);
                    m.dir_mov = inv;
                }
            }

            reglas.recalcular_propiedades();
        }
    }

    // devuelve true si existe al menos una entidad activa no texto con MUEVE
    private bool existe_alguno_mueve()
    {
        for (int x = 0; x < tablero.ancho; x++)
            for (int y = 0; y < tablero.alto; y++)
            {
                var celda = tablero.rejilla[x, y];
                for (int i = 0; i < celda.Count; i++)
                {
                    var e = celda[i];
                    if (!e.gameObject.activeSelf || e.es_texto) continue;
                    if (reglas.tiene_propiedad(e.objeto, Propiedad.MUEVE)) return true; // encontrado un MUEVE activo
                }
            }
        return false; // NO encontrado un MUEVE activo
    }

    //lectura de teclas recién pulsadas (GetKeyDown)
    private Vector2Int leer_pulsada()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return Vector2Int.right;
        return Vector2Int.zero;
    }

    //lectura de teclas mantenidas (GetKey)
    private Vector2Int leer_mantenida()
    {
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) return Vector2Int.up;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) return Vector2Int.down;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) return Vector2Int.left;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) return Vector2Int.right;
        return Vector2Int.zero;
    }

    // reconstruye la lista de entidades con propiedad tu (incluye textos si los hay)
    private void reconstruir_tu()
    {
        entidades_tu.Clear();
        for (int x = 0; x < tablero.ancho; x++)
            for (int y = 0; y < tablero.alto; y++)
                foreach (var e in tablero.rejilla[x, y])
                    if (e.gameObject.activeSelf && reglas.tiene_propiedad(e.objeto, Propiedad.TU))
                        entidades_tu.Add(e);
    }

    // interfaz para ia u otras entradas
    //esta es una funcion que la dejo preparada para permitir simular un Step en la dirección dada
    //la idea es enganchar ia mediante ollama para ver como se desenvuelven los modelos con el juego (es una expansion futura, sale del ambito del actual proyecto)
    public void paso_ia(Vector2Int dir)
    {
        paso(dir); // reutiliza la misma lógica de Step que usa el jugador
    }
}
