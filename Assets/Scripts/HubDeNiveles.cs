using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
public class HubDeNiveles : MonoBehaviour
{
    [Header("referencias")]
    public Tablero tablero;

    [Header("tecla de entrada")]
    public KeyCode tecla_entrar = KeyCode.Return;         // enter
    public KeyCode tecla_entrar_alt = KeyCode.KeypadEnter; //enter del numpad

    void Awake()
    {
        if (!tablero) tablero = FindFirstObjectByType<Tablero>();
    }

    void Start()
    {
        StartCoroutine(restaurar_si_hay_estado());
    }

    System.Collections.IEnumerator restaurar_si_hay_estado()
    {
        if (!EstadoRetornoHub.hay_estado) yield break;

        // espera a que el tablero y su rejilla esten listos, sol incidencia
        while (tablero == null || tablero.rejilla == null) { yield return null; }

        var lu = buscar_lu();
        if (lu == null) yield break;

        mover_entidad_en_tablero(lu, EstadoRetornoHub.celda);
        lu.dir_mov = EstadoRetornoHub.dir;

        //sol incidencia pintado por debajo de nivel cuando mira a la derecha
        var sr = lu.GetComponent<SpriteRenderer>() ?? lu.GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.sortingOrder = 30000; // empujo arriba del todo


        EstadoRetornoHub.limpiar();
    }

    void LateUpdate()
    {




        if (MenuPausa.esta_pausado) return;


        //incidencia con enter encima de nivel, desactivo entrada del mundo
        //if (EventSystem.current && EventSystem.current.currentSelectedGameObject != null)
        //   return;


        if (!Input.GetKeyDown(tecla_entrar) && !Input.GetKeyDown(tecla_entrar_alt))
            return;

        if (!tablero || tablero.rejilla == null) return;

        var lu = buscar_lu();
        if (lu == null) return;

        var entrada = entrada_en_celda(lu.celda);
        if (entrada == null || string.IsNullOrEmpty(entrada.escena_objetivo)) return;

        var escena_hub = SceneManager.GetActiveScene().name;
        EstadoRetornoHub.guardar(escena_hub, lu.celda, lu.dir_mov);

        //correccion de incidencia 
        if (!Application.CanStreamedLevelBeLoaded(entrada.escena_objetivo))
        {
            Debug.LogWarning($"[hub] la escena '{entrada.escena_objetivo}' no esta en build");
            return;
        }
        SceneManager.LoadScene(entrada.escena_objetivo);
    }

    //funcs

    Entidad buscar_lu()
    {
        for (int x = 0; x < tablero.ancho; x++)
            for (int y = 0; y < tablero.alto; y++)
            {
                var celda = tablero.rejilla[x, y];
                for (int i = 0; i < celda.Count; i++)
                {
                    var e = celda[i];
                    if (!e || !e.gameObject.activeSelf) continue;
                    if (e.objeto == Objeto.LU) return e;
                }
            }
        return null;
    }

    EntradaNivel entrada_en_celda(Vector2Int c)
    {
        var lista = tablero.rejilla[c.x, c.y];

        // prioridad entradas que son Entidad en esa celda
        for (int i = 0; i < lista.Count; i++)
        {
            var en = lista[i].GetComponent<EntradaNivel>();
            if (en != null) return en;
        }

        //  entradas sin Entidad alineadas a esa celda
        var todas = FindObjectsByType<EntradaNivel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < todas.Length; i++)
        {
            var en = todas[i];
            if (en.entidad) continue;
            var p = en.transform.position;
            var celda_en = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
            if (celda_en == c) return en;
        }
        return null;
    }

    void mover_entidad_en_tablero(Entidad e, Vector2Int destino)
    {
        if (e.celda == destino) { actualizar_pos_visual(e); return; }

        // quito de origen
        var lista_origen = tablero.rejilla[e.celda.x, e.celda.y];
        lista_origen.Remove(e);

        // actualizo datos
        e.celda = destino;

        // añado en destino
        var lista_destino = tablero.rejilla[destino.x, destino.y];
        lista_destino.Add(e);

        actualizar_pos_visual(e);
    }

    void actualizar_pos_visual(Entidad e)
    {
        e.transform.localPosition = new Vector3(e.celda.x, e.celda.y, 0f);
    }
}
