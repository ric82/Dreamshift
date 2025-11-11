using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPausa : MonoBehaviour
{
    [Header("ui")]
    public Canvas canvas_pausa;            // canvas de pausa
    public Button btn_continuar;
    public Button btn_reiniciar;           //se oculta en hub
    public Button btn_salir_hub;           //se oculta en hub
    public Button btn_ir_titulo;           // siempre

    [Header("tecla")]
    public KeyCode tecla_pausa = KeyCode.Escape;

    public static bool esta_pausado { get; private set; }

    void Awake()
    {
        // creo aqui el unico EventSystem persistente
        if (EventSystem.current == null)
        {

            // nuevo input system
            var go = new GameObject("EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            var ui = go.GetComponent<InputSystemUIInputModule>();

            // clave: no perder el foco al clicar el fondo
            ui.deselectOnBackgroundClick = false;

            // opcional: comportamiento del mouse
            // ui.pointerBehavior = InputSystemUIInputModule.PointerBehavior.SingleClick;

            DontDestroyOnLoad(go);

        }

        //lo quito momentaneamente, pruebo nueva version para no perder el foco al pinchar en el fondo
        /*
        if (!EventSystem.current)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }
        */

        if (canvas_pausa) canvas_pausa.enabled = false;

        // botones
        if (btn_continuar) btn_continuar.onClick.AddListener(ContinuarSeguro);
        if (btn_reiniciar) btn_reiniciar.onClick.AddListener(ReiniciarNivel);
        if (btn_salir_hub) btn_salir_hub.onClick.AddListener(SalirAlHub);
        if (btn_ir_titulo) btn_ir_titulo.onClick.AddListener(IrATituloSeguro);
    }

    //meto 2 funciones para cierre diferido cuando entro desde Hub

    // cierro la pausa y evito que el hub vea el mismo Intro
    public void ContinuarSeguro()
    {
        if (!gameObject.activeInHierarchy)
        {
            //
            TogglePausa();
            return;
        }
        StartCoroutine(ContinuarEnSiguienteFrame());
    }

    System.Collections.IEnumerator ContinuarEnSiguienteFrame()
    {
        // espero al final del frame actual
        yield return null;
        TogglePausa();
    }





    public void IrATituloSeguro()
    {
        if (!gameObject.activeInHierarchy)
        {
            //
            IrATitulo();
            return;
        }
        StartCoroutine(IrATituloEnSiguienteFrame());
    }

    System.Collections.IEnumerator IrATituloEnSiguienteFrame()
    {
        //espero al final del frame actual
        yield return null;

        // suelta el foco por limpieza ya que el EventSystem es persistente
        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(null);

        IrATitulo(); //
    }





    void Update()
    {
        if (Input.GetKeyDown(tecla_pausa))
            TogglePausa();
    }

    public void TogglePausa()
    {
        esta_pausado = !esta_pausado;

        if (canvas_pausa) canvas_pausa.enabled = esta_pausado;
        Time.timeScale = esta_pausado ? 0f : 1f;

        if (esta_pausado)
        {
            ActualizarBotonesSegunContexto();
            if (btn_continuar) EventSystem.current.SetSelectedGameObject(btn_continuar.gameObject);
        }
        //sol incidencia entra en nivel desde el hub
        else
        {
            // al cerrar pausa suelto el foco para que el mundo vuelva a recibir Intro
            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(null);
        }


    }

    void ActualizarBotonesSegunContexto()
    {
        //
        bool es_hub = FindFirstObjectByType<HubDeNiveles>() != null;

        if (btn_reiniciar) btn_reiniciar.gameObject.SetActive(!es_hub);
        if (btn_salir_hub) btn_salir_hub.gameObject.SetActive(!es_hub);
        if (btn_ir_titulo) btn_ir_titulo.gameObject.SetActive(true);
    }

    //acciones
    void ReiniciarNivel()
    {
        var scn = SceneManager.GetActiveScene().name;
        Time.timeScale = 1f;
        esta_pausado = false;
        SceneManager.LoadScene(scn);
    }

    void SalirAlHub()
    {
        // 
        var hub = EstadoRetornoHub.escena_hub;
        if (string.IsNullOrEmpty(hub)) hub = "Hub";

        Time.timeScale = 1f;
        esta_pausado = false;
        SceneManager.LoadScene(hub);
    }

    void IrATitulo()
    {
        Time.timeScale = 1f;
        esta_pausado = false;
        SceneManager.LoadScene("Titulo");
    }
}
