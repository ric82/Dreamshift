/*
    fondotablero.cs
    
    dibuja un rectangulo del tamaño del tablero (ancho x alto) centrado en el tablero
    así consigo distinguir que está dentro y que está fuera (color de la cámara)

    se pone el color de fondo de la camara para el exterior

    se crea y mantiene un hijo con un sprite 1x1 escalado (hasta cubrir todo el tablero), 
    evita duplicados

    sortingOrder bajo para quedar detrás de todo
*/


using UnityEngine;


public class FondoTablero : MonoBehaviour
{
    public Tablero tablero; // referencia al Tablero para leer width/height
    public Camera camara; //camara a la que se le pondrá el color fuera

    public Color color_interior = new Color(0.13f, 0.21f, 0.35f, 1f);
    public Color color_exterior = Color.black;

    public int orden_dibujo = -1000; //orden de render bajo para que quede al fondo

    SpriteRenderer sr; //SpriteRenderer que dibuja el fondo
    static Sprite _pixel; //sprite de 1x1

    void Awake() { auto_conectar(); asegurar_renderer(); }
    void OnEnable() { auto_conectar(); asegurar_renderer(); Refrescar(); }
    void OnValidate() { if (sr) aplicar_colores(); }

    // intenta rellenar referencias si faltan
    void auto_conectar()
    {
        if (!tablero)
            tablero = Object.FindFirstObjectByType<Tablero>();
        if (!camara) camara = Camera.main;

    }

    //quiero asegurar que existe un hijo con spriterenderer unico para el fondo
    void asegurar_renderer()
    {
        sr = null;

        // busca hijos llamados "FondoSprite" y elimina duplicados
        for (int i = transform.childCount - 1; i >= 0; --i)
        {
            var t = transform.GetChild(i);
            if (t.name == "FondoSprite") //solo me interesan estos
            {
                if (sr == null) sr = t.GetComponent<SpriteRenderer>();
                else
                {
                    if (Application.isEditor) DestroyImmediate(t.gameObject);
                    else Destroy(t.gameObject);
                }
            }
        }

        //si no existe crea uno nuevo con sprite 1x1
        if (!sr)
        {
            var go = new GameObject("FondoSprite");
            go.transform.SetParent(transform, false);
            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pixel(); //asigna el sprite de 1x1
        }

        sr.sortingOrder = orden_dibujo; // aseguro que se dibuje detras de todo
        aplicar_colores();              // aplico los colores que tenga
    }

    //crea (una vez) un sprite 1x1 blanco con filtro "point"
    static Sprite pixel()
    {
        if (_pixel) return _pixel;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        _pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        _pixel.name = "Pixel_1x1";
        return _pixel;
    }

    // aplica los colores al sprite interior y al fondo de la camara
    void aplicar_colores()
    {
        sr.color = color_interior;
        if (camara) camara.backgroundColor = color_exterior;
    }

    // ajusta tamaño y posicion del rectangulo al tamaño actual del tablero
    public void Refrescar()
    {
        if (!tablero) return; //sin tablero no se podrá refrescar
        asegurar_renderer();
        aplicar_colores();

        float cx = (tablero.ancho - 1) * 0.5f;
        float cy = (tablero.alto - 1) * 0.5f;

        //posiciona el sprite en el centro del tablero y escala para cubrir width x height
        sr.transform.position = new Vector3(cx, cy, 0f);
        sr.transform.localScale = new Vector3(tablero.ancho, tablero.alto, 1f);
    }
}
