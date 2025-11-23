using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TituloBasico : MonoBehaviour
{
    [Header("a donde ir al empezar")]
    public string escena_hub = "Hub";

    [Header("botones opcionales")]
    public Button boton_jugar;
    public Button boton_salir;


    public Sprite spriteVictoria; // así lo arrastras en el Inspector
    

    void Awake()
    {

        //AnimaVictoria.establecer_sprite_por_defecto(spriteVictoria);

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

        // le doy el foco inicial al boton jugar
        if (EventSystem.current && boton_jugar)
        {
            EventSystem.current.SetSelectedGameObject(null);
            boton_jugar.Select();                           //evito el parpadeo al arrancar
            EventSystem.current.firstSelectedGameObject = boton_jugar.gameObject;
        }
    }

    /*
    void Awake()
    {
        // creo un event system si no existe (navegacion teclado/raton)
        if (!EventSystem.current)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }

        if (EventSystem.current && boton_jugar)
        {
            EventSystem.current.firstSelectedGameObject = boton_jugar.gameObject;
            EventSystem.current.SetSelectedGameObject(boton_jugar.gameObject);
        }

    }
    */

    void Start()
    {
        if (boton_jugar) boton_jugar.onClick.AddListener(ir_al_hub);
        if (boton_salir) boton_salir.onClick.AddListener(salir_del_juego);

        //lo quito de aqui porque se ve el parpadeo al arrancar
        /*
        // foco inicial para teclado/mandos
        if (EventSystem.current && boton_jugar)
        {
            EventSystem.current.firstSelectedGameObject = boton_jugar.gameObject;
            EventSystem.current.SetSelectedGameObject(boton_jugar.gameObject);
        }
        */

    }

    void Update()
    {
        // empezar con intro (enter o keypad enter)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            ir_al_hub();

        // salir con escape
        if (Input.GetKeyDown(KeyCode.Escape))
            salir_del_juego();
    }

    public void ir_al_hub()
    {
        if (!Application.CanStreamedLevelBeLoaded(escena_hub))
        {
            Debug.LogWarning("[titulo] la escena de hub no esta en build o el nombre no coincide");
            return;
        }
        Sonidos.instancia.reproducir_boton_cancelar(); //sonido selec
        SceneManager.LoadScene(escena_hub);
    }

    public void salir_del_juego()
    {
        Sonidos.instancia.reproducir_boton_cancelar(); //sonido selec
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;  // si estoy dentro del editor detengo el Play, sino cierro la aplicacion
#else
        Application.Quit();
#endif
    }
}
